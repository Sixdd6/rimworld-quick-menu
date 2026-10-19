using System;
using System.Collections.Generic;
using QuickMenu.Catalog;
using QuickMenu.Core.Pins;
using QuickMenu.Core.Settings;
using QuickMenu.Diagnostics;
using QuickMenu.Settings;
using RimWorld;
using Verse;
using Verse.Sound;
using Log = QuickMenu.Diagnostics.Log;

namespace QuickMenu.Pinning;

/// <summary>
/// Pin and unpin through the settings board (FR-5, FR-15). Stored pins are resolved against the catalog and the result is
/// cached; opening the menu reads that cache and does no catalog work. The cache is stamped with the catalog version and
/// whether a game is loaded, so it resolves again (an O(1) check on read) after the catalog is rebuilt or gains its
/// Ideology part, and when a game loads (the architect-lookup fallback needs one).
/// </summary>
internal static class PinService
{
    private static readonly Dictionary<PinKey, CatalogEntry> _resolved = new();
    private static bool _initialized;
    private static int _stamp = -1;

    /// <summary>
    /// True when pins have been resolved against a built, non-empty catalog. When false, "unresolved" means "not known yet",
    /// not "mod removed": ForgetUnresolved and the missing-pin count and button must be disabled (T-51), so a transient
    /// fault can never wipe valid pins.
    /// </summary>
    internal static bool IsReady => _initialized && DesignatorCatalog.Entries.Count > 0;

    /// <summary>
    /// Resolves every stored pin against the catalog. At startup (no game loaded) only exact keys resolve; the architect
    /// lookup is used once a game is loaded. Call after the catalog build, on the main thread.
    /// </summary>
    internal static void Initialize() => Refresh();

    /// <summary>Resolves every stored pin again. T-40 calls this after the Ideology part of the catalog is added.</summary>
    internal static void Refresh()
    {
        try
        {
            _resolved.Clear();
            foreach (PinEntry? entry in QuickMenuMod.Settings.Board.Entries)
            {
                if (entry != null)
                {
                    Cache(entry.Key);
                }
            }

            _stamp = Stamp();
            _initialized = true;
        }
        catch (Exception ex)
        {
            _initialized = false;
            Log.LogOnce("Pinning.Resolve", $"Resolving the pins failed, they are treated as not yet known: {ex}");
        }
    }

    /// <summary>Answers from the cache; resolves again first when the catalog or game state changed since.</summary>
    internal static IPinResolver Resolver { get; } = new CacheResolver();

    /// <summary>The catalog entry of a stored pin, or null when it is unresolved (mod removed, FR-14).</summary>
    internal static CatalogEntry? Entry(PinKey key)
    {
        EnsureCurrent();
        return _resolved.TryGetValue(key, out CatalogEntry? e) ? e : null;
    }

    internal static PinBoard Board => QuickMenuMod.Settings.Board;

    /// <summary>Toggles the pin of <paramref name="entry"/>, shows the S9 message with its sound and writes the settings once.</summary>
    internal static PinResult Toggle(CatalogEntry entry) => Toggle(entry.Key, entry.Label, entry.PackageId, entry);

    /// <summary>
    /// Toggles a pin for a designator that is not in the catalog (T-26). The resolved cache gains an entry only when the
    /// catalog knows the key.
    /// </summary>
    internal static PinResult Toggle(PinKey key, string label, string? packageId) => Toggle(key, label, packageId, null);

    private static PinResult Toggle(PinKey key, string label, string? packageId, CatalogEntry? known)
    {
        EnsureCurrent();
        PinResult result = Board.Toggle(key, label, packageId);
        if (result == PinResult.Unpinned)
        {
            _resolved.Remove(key);
        }
        else if (result == PinResult.Pinned && (known ?? (key.Kind == PinKeyKind.Opaque ? null : DesignatorCatalog.Resolve(key))) is { } entry)
        {
            _resolved[key] = entry;
        }

        PinFeedback feedback = PinFeedback.For(result);
        Announce(feedback, label);
        if (feedback.Persist)
        {
            QuickMenuMod.SaveSettings();
        }

        return result;
    }

    private static int Stamp() => (DesignatorCatalog.Version * 2) + (Current.Game != null ? 1 : 0);

    private static void EnsureCurrent()
    {
        if (_initialized && _stamp != Stamp())
        {
            Refresh();
        }
    }

    private static void Announce(PinFeedback feedback, string label)
    {
        try
        {
            TaggedString text = feedback.UsesLabel ? feedback.MessageKey.Translate(label) : feedback.MessageKey.Translate();

            // MessageTypeDefOf.RejectInput already carries the ClickReject sound, which Messages.Message plays, so a
            // reject result adds no sound of its own (it would play twice).
            Messages.Message(text, feedback.IsReject ? MessageTypeDefOf.RejectInput : MessageTypeDefOf.SilentInput, historical: false);
            SoundDef? sound = feedback.Sound switch
            {
                PinSound.TickHigh => SoundDefOf.Tick_High,
                PinSound.TickLow => SoundDefOf.Tick_Low,
                _ => null,
            };
            sound?.PlayOneShotOnCamera();
        }
        catch (Exception ex)
        {
            Log.LogOnce("Pinning.Announce", $"Showing the pin message failed: {ex}");
        }
    }

    private static void Cache(PinKey key)
    {
        if (key.Kind != PinKeyKind.Opaque && DesignatorCatalog.Resolve(key) is { } found)
        {
            _resolved[key] = found;
        }
    }

    private sealed class CacheResolver : IPinResolver
    {
        public bool IsResolved(PinKey key)
        {
            EnsureCurrent();
            return _initialized && _resolved.ContainsKey(key);
        }
    }
}

/// <summary>Triggers the one-time resolve after the catalog build (class initialisers run in an undefined order, so the catalog is touched first).</summary>
[StaticConstructorOnStartup]
internal static class PinServiceStartup
{
    static PinServiceStartup()
    {
        _ = DesignatorCatalog.Entries;
        PinService.Initialize();
    }
}
