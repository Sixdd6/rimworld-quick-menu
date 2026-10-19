using System;
using System.Collections.Generic;

namespace QuickMenu.Core.Pins;

/// <summary>Assigns <see cref="PinKey"/>s to designator descriptors by the rules of ADR 0004.</summary>
public static class PinKeyFactory
{
    /// <summary>
    /// Deepest nesting level that gets keys (roots are level 0). Descriptors below it are treated as absent. The
    /// adapter must not build deeper levels either, so that its entries and the returned keys line up.
    /// </summary>
    public const int MaxDepth = 8;

    private const string UnknownName = "(unknown)";

    /// <summary>
    /// Returns one key per descriptor in depth-first pre-order: a dropdown's key comes first, then the keys of its
    /// children (recursively). A dropdown without elements, or with no element that can be keyed, gets an
    /// <c>order:</c> key, as does a build without a def name. Nested mod dropdowns are left out of the parent's
    /// child keys. <c>order:</c> keys get <c>#n</c> (1-based, in that same order) only when the type occurs more than
    /// once in the whole list. Descriptors nested deeper than <see cref="MaxDepth"/> are ignored. Never throws for
    /// non-null input.
    /// </summary>
    /// <exception cref="ArgumentNullException">A descriptor (or the list) is null.</exception>
    public static IReadOnlyList<PinKey> Assign(IReadOnlyList<DesignatorDescriptor> roots)
    {
        ArgumentNullException.ThrowIfNull(roots);

        var items = new List<Item>(roots.Count);
        var totals = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (DesignatorDescriptor root in roots)
        {
            items.Add(Resolve(root, 0, totals));
        }

        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        var keys = new List<PinKey>(items.Count);
        foreach (Item item in items)
        {
            Append(item, keys, totals, seen);
        }

        return keys;
    }

    private static Item Resolve(DesignatorDescriptor d, int depth, Dictionary<string, int> totals)
    {
        ArgumentNullException.ThrowIfNull(d);

        // Pre-order position among orders is fixed later; here only the totals are counted.
        var children = new List<Item>();
        if (depth < MaxDepth)
        {
            foreach (DesignatorDescriptor child in d.Children)
            {
                children.Add(Resolve(child, depth + 1, totals));
            }
        }

        ItemKind kind = KindOf(d, children);
        var item = new Item(d, kind, children);
        if (kind == ItemKind.Order)
        {
            string type = TypeOf(d);
            totals[type] = totals.GetValueOrDefault(type) + 1;
        }

        return item;
    }

    private static ItemKind KindOf(DesignatorDescriptor d, List<Item> children)
    {
        switch (d.Kind)
        {
            case DescriptorKind.BuildThing:
                return string.IsNullOrEmpty(d.DefName) ? ItemKind.Order : ItemKind.BuildThing;
            case DescriptorKind.BuildTerrain:
                return string.IsNullOrEmpty(d.DefName) ? ItemKind.Order : ItemKind.BuildTerrain;
            case DescriptorKind.Dropdown:
                if (SharedGroup(children) != null)
                {
                    return ItemKind.Dropdown;
                }

                foreach (Item child in children)
                {
                    if (child.Kind != ItemKind.ModDropdown)
                    {
                        return ItemKind.ModDropdown;
                    }
                }

                return ItemKind.Order;
            default:
                return ItemKind.Order;
        }
    }

    private static PinKey Append(Item item, List<PinKey> keys, Dictionary<string, int> totals, Dictionary<string, int> seen)
    {
        DesignatorDescriptor d = item.Descriptor;
        if (item.Kind == ItemKind.Order)
        {
            // Ordinals follow pre-order, so take the parent's before its children's.
            PinKey order = OrderKey(d, totals, seen);
            keys.Add(order);
            foreach (Item child in item.Children)
            {
                Append(child, keys, totals, seen);
            }

            return order;
        }

        // Reserve the parent's slot so the output stays pre-order.
        int index = keys.Count;
        keys.Add(null!);
        var childKeys = new List<PinKey>(item.Children.Count);
        foreach (Item child in item.Children)
        {
            childKeys.Add(Append(child, keys, totals, seen));
        }

        PinKey key = item.Kind switch
        {
            ItemKind.BuildThing => PinKey.Build(PinKeyKind.BuildThing, d.DefName!),
            ItemKind.BuildTerrain => PinKey.Build(PinKeyKind.BuildTerrain, d.DefName!),
            _ => DropdownKey(item, childKeys),
        };
        keys[index] = key;
        return key;
    }

    private static PinKey DropdownKey(Item item, List<PinKey> childKeys)
    {
        string? group = SharedGroup(item.Children);
        if (group != null)
        {
            return PinKey.Dropdown(group);
        }

        var keyable = new List<PinKey>(childKeys.Count);
        foreach (PinKey k in childKeys)
        {
            if (k.Kind != PinKeyKind.ModDropdown)
            {
                keyable.Add(k);
            }
        }

        string category = string.IsNullOrEmpty(item.Descriptor.Category) ? UnknownName : item.Descriptor.Category;
        return PinKey.ModDropdown(TypeOf(item.Descriptor), category, keyable);
    }

    /// <summary>The group def name when every element is a build in the same group; otherwise null.</summary>
    private static string? SharedGroup(List<Item> children)
    {
        string? group = null;
        foreach (Item child in children)
        {
            bool build = child.Kind is ItemKind.BuildThing or ItemKind.BuildTerrain;
            string? childGroup = child.Descriptor.DropdownGroup;
            if (!build || string.IsNullOrEmpty(childGroup) || (group != null && group != childGroup))
            {
                return null;
            }

            group = childGroup;
        }

        return group;
    }

    private static PinKey OrderKey(DesignatorDescriptor d, Dictionary<string, int> totals, Dictionary<string, int> seen)
    {
        string type = TypeOf(d);
        if (totals[type] < 2)
        {
            return PinKey.Order(type);
        }

        int n = seen.GetValueOrDefault(type) + 1;
        seen[type] = n;
        return PinKey.Order(type, n);
    }

    private static string TypeOf(DesignatorDescriptor d) => string.IsNullOrEmpty(d.TypeFullName) ? UnknownName : d.TypeFullName;

    private enum ItemKind
    {
        BuildThing,
        BuildTerrain,
        Dropdown,
        ModDropdown,
        Order,
    }

    private sealed class Item
    {
        internal Item(DesignatorDescriptor descriptor, ItemKind kind, List<Item> children)
        {
            Descriptor = descriptor;
            Kind = kind;
            Children = children;
        }

        internal DesignatorDescriptor Descriptor { get; }

        internal ItemKind Kind { get; }

        internal List<Item> Children { get; }
    }
}
