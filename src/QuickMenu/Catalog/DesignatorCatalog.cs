using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using QuickMenu.Core.Pins;
using QuickMenu.Diagnostics;
using RimWorld;
using Verse;
using Log = QuickMenu.Diagnostics.Log;

namespace QuickMenu.Catalog;

/// <summary>
/// Every architect designator, independent of the architect UI (ADR 0006). The static part is built once at
/// startup, after defs are resolved; Ideology designators are added later (T-40). Built from
/// <see cref="DesignationCategoryDef.AllResolvedDesignators"/>, dropdown elements flattened after their dropdown.
/// </summary>
[StaticConstructorOnStartup]
internal static class DesignatorCatalog
{
    internal static readonly PatchSite BuildSite = new("Catalog.Build");

    private static readonly List<CatalogEntry> _entries = new();
    private static readonly Dictionary<PinKey, CatalogEntry> _byKey = new();
    private static readonly Dictionary<Designator, CatalogEntry> _byDesignator = new();

    static DesignatorCatalog()
    {
        try
        {
            Rebuild();
        }
        catch (Exception ex)
        {
            BuildSite.Fail(ex);
        }
    }

    /// <summary>All entries in pre-order; empty when the build failed.</summary>
    internal static IReadOnlyList<CatalogEntry> Entries => _entries;

    /// <summary>Wall-clock time of the last build, for the self-test (budget P-11c: at most 200 ms).</summary>
    internal static TimeSpan BuildTime { get; private set; }

    /// <summary>Entries whose designator or mod information could not be read and were left out.</summary>
    internal static int SkippedCount { get; private set; }

    /// <summary>Finds the entry for a key: exact match, then (for builds) the architect's own lookup. Never throws.</summary>
    /// <returns>The entry, or null when the key is unresolved.</returns>
    internal static CatalogEntry? Resolve(PinKey? key)
    {
        if (key == null)
        {
            return null;
        }

        if (_byKey.TryGetValue(key, out CatalogEntry? exact))
        {
            return exact;
        }

        if (key.Kind is not (PinKeyKind.BuildThing or PinKeyKind.BuildTerrain))
        {
            return null;
        }

        try
        {
            BuildableDef? def = key.Kind == PinKeyKind.BuildThing
                ? DefDatabase<ThingDef>.GetNamedSilentFail(key.Name)
                : DefDatabase<TerrainDef>.GetNamedSilentFail(key.Name);
            Designator? found = def == null ? null : BuildCopyCommandUtility.FindAllowedDesignator(def);
            return found != null && _byDesignator.TryGetValue(found, out CatalogEntry? entry) ? entry : null;
        }
        catch (Exception ex)
        {
            Log.LogOnce("Catalog.Resolve", $"Resolving {key} failed: {ex}");
            return null;
        }
    }

    /// <summary>The catalog entry of a live designator, or null (for example an Ideology-only designator).</summary>
    internal static CatalogEntry? Find(Designator designator) =>
        designator != null && _byDesignator.TryGetValue(designator, out CatalogEntry? e) ? e : null;

    internal static void Rebuild()
    {
        var stopwatch = Stopwatch.StartNew();
        _entries.Clear();
        _byKey.Clear();
        _byDesignator.Clear();
        SkippedCount = 0;

        var nodes = new List<Node>();
        var descriptors = new List<DesignatorDescriptor>();
        var categories = new List<DesignationCategoryDef>(DefDatabase<DesignationCategoryDef>.AllDefsListForReading);
        categories.Sort(static (a, b) => a.order != b.order ? a.order.CompareTo(b.order) : string.CompareOrdinal(a.defName, b.defName));
        var mods = new ModLookup();
        foreach (DesignationCategoryDef category in categories)
        {
            foreach (Designator designator in category.AllResolvedDesignators)
            {
                Node? node = MakeNode(designator, category, mods, depth: 0);
                if (node != null)
                {
                    nodes.Add(node);
                    descriptors.Add(node.Descriptor);
                }
            }
        }

        IReadOnlyList<PinKey> keys = PinKeyFactory.Assign(descriptors);
        int index = 0;
        foreach (Node node in nodes)
        {
            AddEntries(node, -1, keys, ref index);
        }

        stopwatch.Stop();
        BuildTime = stopwatch.Elapsed;
    }

    private static void AddEntries(Node node, int parentId, IReadOnlyList<PinKey> keys, ref int index)
    {
        int id = _entries.Count;
        PinKey key = keys[index++];
        var entry = new CatalogEntry(id, parentId, node.Designator, key, node.Label, node.CategoryLabel, node.ModName, node.PackageId, node.Descriptor.Kind == DescriptorKind.Dropdown);
        _entries.Add(entry);
        _byKey.TryAdd(key, entry);
        _byDesignator.TryAdd(node.Designator, entry);
        foreach (Node child in node.Children)
        {
            AddEntries(child, id, keys, ref index);
        }
    }

    // One bad third-party designator must not take the whole catalog down (NFR-6).
    private static Node? MakeNode(Designator designator, DesignationCategoryDef category, ModLookup mods, int depth)
    {
        try
        {
            if (designator == null)
            {
                return null;
            }

            var children = new List<Node>();
            var childDescriptors = new List<DesignatorDescriptor>();
            if (designator is Designator_Dropdown dropdown && depth < 4)
            {
                foreach (Designator element in dropdown.Elements)
                {
                    Node? child = MakeNode(element, category, mods, depth + 1);
                    if (child != null)
                    {
                        children.Add(child);
                        childDescriptors.Add(child.Descriptor);
                    }
                }
            }

            BuildableDef? placing = (designator as Designator_Build)?.PlacingDef;
            DescriptorKind kind = designator switch
            {
                Designator_Dropdown => DescriptorKind.Dropdown,
                Designator_Build when placing is TerrainDef => DescriptorKind.BuildTerrain,
                Designator_Build when placing is ThingDef => DescriptorKind.BuildThing,
                _ => DescriptorKind.Other,
            };
            string? group = placing switch
            {
                ThingDef t => t.designatorDropdown?.defName,
                TerrainDef t => t.designatorDropdown?.defName,
                _ => null,
            };
            string? defName = kind is DescriptorKind.BuildThing or DescriptorKind.BuildTerrain ? placing?.defName : null;
            if (kind is DescriptorKind.BuildThing or DescriptorKind.BuildTerrain && string.IsNullOrEmpty(defName))
            {
                kind = DescriptorKind.Other;
            }

            Type type = designator.GetType();
            var descriptor = new DesignatorDescriptor(kind, type.FullName ?? type.Name, defName, category.defName, group, childDescriptors);
            ModContentPack? pack = placing?.modContentPack ?? mods.Of(type);
            return new Node(
                designator,
                descriptor,
                designator.Label ?? string.Empty,
                category.LabelCap.ToString(),
                pack?.Name ?? string.Empty,
                pack?.PackageId ?? string.Empty,
                children);
        }
        catch (Exception ex)
        {
            SkippedCount++;
            Log.LogOnce("Catalog.Designator." + designator?.GetType().FullName, $"A designator was left out of the catalog: {ex}");
            return null;
        }
    }

    private sealed class Node
    {
        internal Node(Designator designator, DesignatorDescriptor descriptor, string label, string categoryLabel, string modName, string packageId, List<Node> children)
        {
            Designator = designator;
            Descriptor = descriptor;
            Label = label;
            CategoryLabel = categoryLabel;
            ModName = modName;
            PackageId = packageId;
            Children = children;
        }

        internal Designator Designator { get; }

        internal DesignatorDescriptor Descriptor { get; }

        internal string Label { get; }

        internal string CategoryLabel { get; }

        internal string ModName { get; }

        internal string PackageId { get; }

        internal List<Node> Children { get; }
    }

    /// <summary>Maps a type to the mod whose assembly defines it; types of the base game map to the core mod.</summary>
    private sealed class ModLookup
    {
        private readonly Dictionary<Assembly, ModContentPack> _byAssembly = new();
        private readonly ModContentPack? _core;

        internal ModLookup()
        {
            foreach (ModContentPack pack in LoadedModManager.RunningModsListForReading)
            {
                if (pack.IsCoreMod)
                {
                    _core = pack;
                }

                foreach (Assembly assembly in pack.assemblies.loadedAssemblies)
                {
                    _byAssembly.TryAdd(assembly, pack);
                }
            }
        }

        internal ModContentPack? Of(Type type) => _byAssembly.TryGetValue(type.Assembly, out ModContentPack? pack) ? pack : _core;
    }
}
