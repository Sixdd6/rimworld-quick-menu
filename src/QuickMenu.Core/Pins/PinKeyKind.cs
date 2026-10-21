namespace QuickMenu.Core.Pins;

/// <summary>The kind of designator a <see cref="PinKey"/> identifies (ADR 0004).</summary>
public enum PinKeyKind
{
    /// <summary><c>build:thing/&lt;defName&gt;</c></summary>
    BuildThing,

    /// <summary><c>build:terrain/&lt;defName&gt;</c></summary>
    BuildTerrain,

    /// <summary><c>dropdown:&lt;DesignatorDropdownGroupDef.defName&gt;</c></summary>
    Dropdown,

    /// <summary><c>order:&lt;Type.FullName&gt;[#n]</c></summary>
    Order,

    /// <summary><c>moddrop:&lt;Type.FullName&gt;@&lt;category&gt;~&lt;child&gt;,...</c></summary>
    ModDropdown,

    /// <summary>
    /// A stored line this version cannot interpret (unknown or unparseable key). Its <see cref="PinKey.Value"/> is the
    /// raw text after the slot, written back unchanged. Never produced by <see cref="PinKey.TryParse"/>; never resolved.
    /// </summary>
    Opaque,
}
