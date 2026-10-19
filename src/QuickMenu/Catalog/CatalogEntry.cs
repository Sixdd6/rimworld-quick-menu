using QuickMenu.Core.Pins;
using Verse;

namespace QuickMenu.Catalog;

/// <summary>One architect designator (including dropdown elements) as the catalog saw it at startup.</summary>
internal sealed class CatalogEntry
{
    internal CatalogEntry(
        int id,
        int parentId,
        Designator designator,
        PinKey key,
        string label,
        string categoryLabel,
        string modName,
        string packageId,
        bool isDropdown,
        bool forcedUnavailable = false)
    {
        Id = id;
        ParentId = parentId;
        Designator = designator;
        Key = key;
        Label = label;
        CategoryLabel = categoryLabel;
        ModName = modName;
        PackageId = packageId;
        IsDropdown = isDropdown;
        ForcedUnavailable = forcedUnavailable;
    }

    /// <summary>Index in <see cref="DesignatorCatalog.Entries"/> (pre-order: a dropdown precedes its elements).</summary>
    internal int Id { get; }

    /// <summary>Id of the owning dropdown entry, or -1.</summary>
    internal int ParentId { get; }

    internal Designator Designator { get; }

    internal PinKey Key { get; }

    internal string Label { get; }

    internal string CategoryLabel { get; }

    internal string ModName { get; }

    /// <summary>Package id of the owning mod, lower-cased by the game; empty when unknown.</summary>
    internal string PackageId { get; }

    internal bool IsDropdown { get; }

    /// <summary>
    /// True for an entry made only to show a pin whose def exists but whose designator the game does not offer right now
    /// (FR-14): it is always shown greyed. Such entries are not in <see cref="DesignatorCatalog.Entries"/> (Id is -1).
    /// </summary>
    internal bool ForcedUnavailable { get; }
}
