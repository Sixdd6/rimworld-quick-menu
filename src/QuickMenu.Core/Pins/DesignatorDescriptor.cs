using System;
using System.Collections.Generic;

namespace QuickMenu.Core.Pins;

/// <summary>What a designator is, as far as pin keys are concerned (ADR 0004).</summary>
public enum DescriptorKind
{
    /// <summary>A <c>Designator_Build</c> for a <c>ThingDef</c>.</summary>
    BuildThing,

    /// <summary>A <c>Designator_Build</c> for a <c>TerrainDef</c>.</summary>
    BuildTerrain,

    /// <summary>A <c>Designator_Dropdown</c> or subclass.</summary>
    Dropdown,

    /// <summary>Any other designator (orders, zones, areas, mod tools).</summary>
    Other,
}

/// <summary>
/// Game-free description of one designator, as the adapter extracts it. Dropdown children are nested; the
/// descriptor list given to <see cref="PinKeyFactory.Assign"/> is in category order, then definition order.
/// </summary>
public sealed class DesignatorDescriptor
{
    private static readonly IReadOnlyList<DesignatorDescriptor> _noChildren = Array.AsReadOnly(Array.Empty<DesignatorDescriptor>());

    public DesignatorDescriptor(
        DescriptorKind kind,
        string typeFullName,
        string? defName,
        string category,
        string? dropdownGroup = null,
        IReadOnlyList<DesignatorDescriptor>? children = null)
    {
        Kind = kind;
        TypeFullName = typeFullName;
        DefName = defName;
        Category = category;
        DropdownGroup = dropdownGroup;
        Children = children ?? _noChildren;
    }

    public DescriptorKind Kind { get; }

    /// <summary>Full name of the designator's runtime type.</summary>
    public string TypeFullName { get; }

    /// <summary>Def name of the built def (builds only).</summary>
    public string? DefName { get; }

    /// <summary>Def name of the designation category that holds the designator.</summary>
    public string Category { get; }

    /// <summary>For a build: <c>designatorDropdown</c> group def name, or null.</summary>
    public string? DropdownGroup { get; }

    /// <summary>Elements of a dropdown; empty for everything else.</summary>
    public IReadOnlyList<DesignatorDescriptor> Children { get; }
}
