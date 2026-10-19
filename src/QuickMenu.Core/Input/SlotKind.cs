namespace QuickMenu.Core.Input;

/// <summary>What a slot is when it is released on (design.md S4). Decides which command the state machine returns.</summary>
public enum SlotKind
{
    /// <summary>Empty or reserved-missing slot in use mode: no hover, no click, the menu stays open.</summary>
    Empty = 0,

    /// <summary>A pin that activates.</summary>
    Normal,

    /// <summary>A pin that opens a satellite.</summary>
    Submenu,

    /// <summary>A pin that cannot be used now: reject sound and reason, the menu stays open (FR-14).</summary>
    Disabled,
}
