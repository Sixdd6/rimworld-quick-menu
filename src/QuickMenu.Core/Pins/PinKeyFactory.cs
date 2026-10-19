using System;
using System.Collections.Generic;
using System.Linq;

namespace QuickMenu.Core.Pins;

/// <summary>Assigns <see cref="PinKey"/>s to designator descriptors by the rules of ADR 0004.</summary>
public static class PinKeyFactory
{
    /// <summary>
    /// Returns one key per descriptor in depth-first pre-order: a dropdown's key comes first, then the keys of its
    /// children (recursively). A dropdown without elements gets an <c>order:</c> key. Nested mod dropdowns are
    /// left out of the parent's child keys. <c>order:</c> keys get <c>#n</c> (1-based, in that same order) only when the type
    /// occurs more than once in the whole list.
    /// </summary>
    public static IReadOnlyList<PinKey> Assign(IReadOnlyList<DesignatorDescriptor> roots)
    {
        ArgumentNullException.ThrowIfNull(roots);

        var flat = new List<DesignatorDescriptor>();
        foreach (DesignatorDescriptor root in roots)
        {
            Flatten(root, flat, 0);
        }

        var totals = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (DesignatorDescriptor d in flat)
        {
            if (IsOrder(d))
            {
                totals[d.TypeFullName] = totals.GetValueOrDefault(d.TypeFullName) + 1;
            }
        }

        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        var keys = new List<PinKey>(flat.Count);
        foreach (DesignatorDescriptor d in roots)
        {
            Append(d, keys, totals, seen);
        }

        return keys;
    }

    private const int MaxDepth = 8;

    private static void Flatten(DesignatorDescriptor d, List<DesignatorDescriptor> into, int depth)
    {
        ArgumentNullException.ThrowIfNull(d);
        if (depth > MaxDepth)
        {
            throw new ArgumentException("Dropdowns are nested too deeply.");
        }

        into.Add(d);
        foreach (DesignatorDescriptor child in d.Children)
        {
            Flatten(child, into, depth + 1);
        }
    }

    private static PinKey Append(DesignatorDescriptor d, List<PinKey> keys, Dictionary<string, int> totals, Dictionary<string, int> seen)
    {
        // Reserve the parent's slot so the output stays pre-order.
        int index = keys.Count;
        keys.Add(null!);
        var childKeys = new List<PinKey>(d.Children.Count);
        foreach (DesignatorDescriptor child in d.Children)
        {
            childKeys.Add(Append(child, keys, totals, seen));
        }

        PinKey key = d.Kind switch
        {
            DescriptorKind.BuildThing => PinKey.Build(PinKeyKind.BuildThing, Require(d.DefName, d)),
            DescriptorKind.BuildTerrain => PinKey.Build(PinKeyKind.BuildTerrain, Require(d.DefName, d)),
            _ when IsOrder(d) => OrderKey(d, totals, seen),
            _ => DropdownKey(d, childKeys),
        };
        keys[index] = key;
        return key;
    }

    private static PinKey DropdownKey(DesignatorDescriptor d, List<PinKey> childKeys)
    {
        string? group = SharedGroup(d);
        return group != null
            ? PinKey.Dropdown(group)
            : PinKey.ModDropdown(d.TypeFullName, d.Category, childKeys.Where(static k => k.Kind != PinKeyKind.ModDropdown));
    }

    /// <summary>Plain designators, and dropdowns without elements, are identified by type.</summary>
    private static bool IsOrder(DesignatorDescriptor d) =>
        d.Kind == DescriptorKind.Other || (d.Kind == DescriptorKind.Dropdown && d.Children.Count == 0);

    /// <summary>The group def name when every element is a build in the same group; otherwise null.</summary>
    private static string? SharedGroup(DesignatorDescriptor d)
    {
        string? group = null;
        foreach (DesignatorDescriptor child in d.Children)
        {
            bool build = child.Kind is DescriptorKind.BuildThing or DescriptorKind.BuildTerrain;
            if (!build || string.IsNullOrEmpty(child.DropdownGroup) || (group != null && group != child.DropdownGroup))
            {
                return null;
            }

            group = child.DropdownGroup;
        }

        return group;
    }

    private static PinKey OrderKey(DesignatorDescriptor d, Dictionary<string, int> totals, Dictionary<string, int> seen)
    {
        if (totals[d.TypeFullName] < 2)
        {
            return PinKey.Order(d.TypeFullName);
        }

        int n = seen.GetValueOrDefault(d.TypeFullName) + 1;
        seen[d.TypeFullName] = n;
        return PinKey.Order(d.TypeFullName, n);
    }

    private static string Require(string? defName, DesignatorDescriptor d) =>
        string.IsNullOrEmpty(defName)
            ? throw new ArgumentException($"A build descriptor of type {d.TypeFullName} needs a def name.")
            : defName;
}
