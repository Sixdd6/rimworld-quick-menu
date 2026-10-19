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
    private static readonly List<PinKey> _duplicateKeys = new();

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

    /// <summary>How many times <see cref="Rebuild"/> has run this session (budget P-12: expected 1).</summary>
    internal static int BuildCount { get; private set; }

    /// <summary>Number of entries dropped from the key lookup because their key was already taken.</summary>
    internal static int DuplicateKeyCount => _duplicateKeys.Count;

    /// <summary>The keys that were produced more than once (one item per dropped entry).</summary>
    internal static IReadOnlyList<PinKey> DuplicateKeys => _duplicateKeys;

    /// <summary>Count of <see cref="DesignationCategoryDef.AllResolvedDesignators"/> over all categories at build time.</summary>
    internal static int ResolvedDesignatorCount { get; private set; }

    /// <summary>Number of top-level entries (entries without a dropdown parent) in the catalog.</summary>
    internal static int TopLevelCount { get; private set; }

    /// <summary>True when every resolved designator became a top-level entry (nothing was skipped or dropped).</summary>
    internal static bool IsComplete => ResolvedDesignatorCount == TopLevelCount;

    /// <summary>Entries whose designator or mod information could not be read and were left out.</summary>
    internal static int SkippedCount { get; private set; }

    /// <summary>
    /// Changes whenever the entries change (a rebuild, or the Ideology part being added). Pin resolution compares it to
    /// the value it resolved against, an O(1) check, and resolves again when it differs.
    /// </summary>
    internal static int Version { get; private set; }

    /// <summary>Call after the entries were changed outside <see cref="Rebuild"/> (the Ideology refresh), so pins resolve again.</summary>
    internal static void NotifyChanged() => Version++;

    /// <summary>
    /// Finds the entry for a key: exact match, then (for builds, and only while a game is loaded) the architect's own
    /// lookup, which evaluates designator visibility and so needs game state. Never throws, never logs the key.
    /// </summary>
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

        if (key.Kind is not (PinKeyKind.BuildThing or PinKeyKind.BuildTerrain) || Current.Game == null)
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
            // The key holds a def name that can identify a mod, so only its kind and the exception type are logged.
            Log.LogOnce("Catalog.Resolve", $"Resolving a {key.Kind} pin through the architect lookup failed ({ex.GetType().Name}); it stays unresolved.");
            return null;
        }
    }

    /// <summary>The catalog entry of a live designator, or null (for example an Ideology-only designator).</summary>
    internal static CatalogEntry? Find(Designator designator) =>
        designator != null && _byDesignator.TryGetValue(designator, out CatalogEntry? e) ? e : null;

    internal static void Rebuild()
    {
        BuildCount++;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _entries.Clear();
            _byKey.Clear();
            _byDesignator.Clear();
            _duplicateKeys.Clear();
            SkippedCount = 0;
            ResolvedDesignatorCount = 0;
            TopLevelCount = 0;

            var nodes = new List<Node>();
            var categories = new List<DesignationCategoryDef>(DefDatabase<DesignationCategoryDef>.AllDefsListForReading);
            categories.Sort(static (a, b) => a.order != b.order ? a.order.CompareTo(b.order) : string.CompareOrdinal(a.defName, b.defName));
            var mods = new ModLookup();
            foreach (DesignationCategoryDef category in categories)
            {
                foreach (Designator designator in category.AllResolvedDesignators)
                {
                    ResolvedDesignatorCount++;
                    Node? node = MakeNode(designator, category, mods, depth: 0);
                    if (node != null)
                    {
                        nodes.Add(node);
                    }
                }
            }

            IReadOnlyList<PinKey> keys = AssignKeys(nodes);
            int index = 0;
            foreach (Node node in nodes)
            {
                AddEntries(node, -1, keys, ref index);
            }

            TopLevelCount = nodes.Count;
        }
        finally
        {
            Version++;
            stopwatch.Stop();
            BuildTime = stopwatch.Elapsed;
        }
    }

    // Key assignment should never throw, but one root must not be able to empty the catalog if it does.
    private static IReadOnlyList<PinKey> AssignKeys(List<Node> nodes)
    {
        try
        {
            return PinKeyFactory.Assign(Descriptors(nodes));
        }
        catch (Exception ex)
        {
            Log.LogOnce("Catalog.Keys", $"Assigning pin keys failed, isolating the bad designators: {ex}");
        }

        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            try
            {
                PinKeyFactory.Assign(new[] { nodes[i].Descriptor });
            }
            catch (Exception ex)
            {
                SkippedCount++;
                Log.LogOnce("Catalog.Keys." + nodes[i].Descriptor.TypeFullName, $"A designator of type {nodes[i].Descriptor.TypeFullName} was left out of the catalog: {ex.Message}");
                nodes.RemoveAt(i);
            }
        }

        return PinKeyFactory.Assign(Descriptors(nodes));
    }

    private static List<DesignatorDescriptor> Descriptors(List<Node> nodes)
    {
        var descriptors = new List<DesignatorDescriptor>(nodes.Count);
        foreach (Node node in nodes)
        {
            descriptors.Add(node.Descriptor);
        }

        return descriptors;
    }

    private static void AddEntries(Node node, int parentId, IReadOnlyList<PinKey> keys, ref int index)
    {
        int id = _entries.Count;
        PinKey key = keys[index++];
        var entry = new CatalogEntry(id, parentId, node.Designator, key, node.Label, node.CategoryLabel, node.ModName, node.PackageId, node.Descriptor.Kind == DescriptorKind.Dropdown);
        _entries.Add(entry);
        if (!_byKey.TryAdd(key, entry))
        {
            _duplicateKeys.Add(key);
        }

        _byDesignator.TryAdd(node.Designator, entry);
        foreach (Node child in node.Children)
        {
            AddEntries(child, id, keys, ref index);
        }
    }

    /// <summary>
    /// The one place a designator becomes a descriptor (kind, empty def name downgraded to Other, dropdown group, children,
    /// category). The catalog and the pin click's uncatalogued fallback both use it, so their keys always agree.
    /// </summary>
    internal static DesignatorDescriptor BuildDescriptor(Designator designator, string categoryDefName, IReadOnlyList<DesignatorDescriptor> children, out BuildableDef? placing)
    {
        placing = (designator as Designator_Build)?.PlacingDef;
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
        return new DesignatorDescriptor(kind, type.FullName ?? type.Name, defName, categoryDefName, group, children);
    }

    /// <summary>Descriptor of a designator that is not in the catalog, with its dropdown elements, built by <see cref="BuildDescriptor"/>.</summary>
    internal static DesignatorDescriptor DescribeTree(Designator designator, string categoryDefName, int depth = 0)
    {
        var children = new List<DesignatorDescriptor>();
        if (designator is Designator_Dropdown dropdown && depth < PinKeyFactory.MaxDepth)
        {
            foreach (Designator element in dropdown.Elements)
            {
                if (element != null)
                {
                    children.Add(DescribeTree(element, categoryDefName, depth + 1));
                }
            }
        }

        return BuildDescriptor(designator, categoryDefName, children, out _);
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
            if (designator is Designator_Dropdown dropdown && depth < PinKeyFactory.MaxDepth)
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

            DesignatorDescriptor descriptor = BuildDescriptor(designator, category.defName, childDescriptors, out BuildableDef? placing);
            DescriptorKind kind = descriptor.Kind;
            Type type = designator.GetType();
            ModContentPack? pack = placing?.modContentPack ?? mods.Of(type);
            string modName = pack?.Name ?? string.Empty;
            string packageId = pack?.PackageId ?? string.Empty;
            if (kind == DescriptorKind.Dropdown && pack is { IsCoreMod: true } && children.Count > 0)
            {
                // Def dropdowns from mods are plain Designator_Dropdown, which the type lookup credits to Core.
                modName = children[0].ModName;
                packageId = children[0].PackageId;
            }

            return new Node(designator, descriptor, designator.Label ?? string.Empty, category.LabelCap.ToString(), modName, packageId, children);
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
