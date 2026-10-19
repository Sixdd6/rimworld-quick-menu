using QuickMenu.Diagnostics;
using Verse;

namespace QuickMenu.Menu;

/// <summary>Opens and closes the quick menu window (ADR 0007). Created lazily: nothing exists while the menu is closed.</summary>
internal static class MenuController
{
    internal static readonly PatchSite Site = new("MenuController.Open");

    private static QuickMenuWindow? _window;

    internal static bool IsOpen => _window != null;

    /// <summary>Opens the menu in the current frame. Pressing the trigger while it is open closes it.</summary>
    internal static void Open()
    {
        if (Site.Disabled)
        {
            return;
        }

        try
        {
            if (_window != null)
            {
                Close();
                return;
            }

            Find.WindowStack.Add(_window = new QuickMenuWindow(UnityEngine.Time.frameCount));
        }
        catch (System.Exception ex)
        {
            _window = null;
            Site.Fail(ex);
        }
    }

    internal static void Close()
    {
        QuickMenuWindow? window = _window;
        _window = null;
        window?.Close();
    }

    /// <summary>Called by the window when it has been removed from the stack for any reason.</summary>
    internal static void Closed(QuickMenuWindow window)
    {
        if (_window == window)
        {
            _window = null;
        }
    }
}
