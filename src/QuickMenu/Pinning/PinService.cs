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
    private static long _stamp = -1;
    private static long _failedStamp = -1;
    private static Game? _lastGame;
    private static int _gameGeneration;

    /// <summary>
    /// True when pins have been resolved against a built, non-empty catalog. When false, "unresolved" means "not known yet",
    /// and the missing-pin count and "Forget missing pins" button must be disabled (T-51). Being ready is not enough to
    /// forget a pin: at the main menu resolution is incomplete (no game, so no architect lookup), so only
    /// <see cref="IsMissing"/> decides which pins are gone, never plain "unresolved".
    /// </summary>
    internal static bool IsReady => _initialized && DesignatorCatalog.Entries.Count > 0;

    /// <summary>
    /// Resolves every stored pin against the catalog. At startup (no game loaded) only exact keys resolve; the architect
    /// lookup is used once a game is loaded. Call after the catalog build, on the main thread.
    /// </summary>
    internal static void Initialize() => Refresh();

    /// <summary>Resolves every stored pin again. Runs by itself (via the stamp) after the Ideology part of the catalog changes or the catalog is rebuilt.</summary>
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
            _failedStamp = -1;
            _initialized = true;
        }
        catch (Exception ex)
        {
            _initialized = false;
            _stamp = -1;
            _failedStamp = Stamp();
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

    /// <summary>
    /// True when a stored pin is really gone: unresolved, and (for a build pin) its def is not in the DefDatabase, or its
    /// stored packageId is not an active mod. An unresolved pin that fails both tests is only unavailable right now
    /// (no game loaded, hidden by research or a mode) and is never forgotten. Opaque pins are never missing.
    /// </summary>
    internal static bool IsMissing(PinEntry entry)
    {
        EnsureCurrent();
        PinKey key = entry.Key;
        if (!IsReady || key.Kind == PinKeyKind.Opaque || _resolved.ContainsKey(key))
        {
            return false;
        }

        try
        {
            if (key.Kind == PinKeyKind.BuildThing)
            {
                if (DefDatabase<ThingDef>.GetNamedSilentFail(key.Name) == null)
                {
                    return true;
                }
            }
            else if (key.Kind == PinKeyKind.BuildTerrain && DefDatabase<TerrainDef>.GetNamedSilentFail(key.Name) == null)
            {
                return true;
            }

            return !string.IsNullOrEmpty(entry.PackageId) && !ModsConfig.IsActive(entry.PackageId);
        }
        catch (Exception ex)
        {
            Log.LogOnce("Pinning.IsMissing", $"Checking whether a pin is missing failed, it is kept: {ex.GetType().Name}");
            return false;
        }
    }

    /// <summary>How many stored pins are missing (see <see cref="IsMissing"/>); 0 while not ready.</summary>
    internal static int CountMissing()
    {
        int n = 0;
        foreach (PinEntry? entry in Board.Entries)
        {
            if (entry != null && IsMissing(entry))
            {
                n++;
            }
        }

        return n;
    }

    /// <summary>Removes the missing pins only (see <see cref="IsMissing"/>) and writes the settings once; returns how many.</summary>
    internal static int ForgetMissing()
    {
        int n = Board.ForgetUnresolved(new MissingResolver());
        if (n > 0)
        {
            QuickMenuMod.SaveSettings();
        }

        return n;
    }

    /// <summary>Removes every pin and the resolved cache with it, so no stale entry outlives its pin.</summary>
    internal static void ClearAll()
    {
        QuickMenuMod.Settings.ClearAllPins();
        _resolved.Clear();
    }

    internal static PinBoard Board => QuickMenuMod.Settings.Board;

    /// <summary>
    /// Toggles the pin of <paramref name="entry"/>, shows the S9 message with its sound and writes the settings once.
    /// A mod dropdown whose stored pin was matched fuzzily (<see cref="ModDropdownMatcher"/>, after the mod added or removed a
    /// child) has a different key than the entry; with no exact pin on the board, the stored pin that resolves to this entry
    /// is toggled instead, and its stored key is never rewritten. If several stored pins resolve to the same entry, each
    /// toggle removes one of them, in board order, until none is left; only then does a toggle pin the entry's own key.
    /// </summary>
    internal static PinResult Toggle(CatalogEntry entry) => Toggle(StoredKeyFor(entry) ?? entry.Key, entry.Label, entry.PackageId, entry, persist: true);

    /// <summary>
    /// As <see cref="Toggle(CatalogEntry)"/> for a stored pin identified by its own key, optionally without writing the
    /// settings. The caller (edit mode) then owns the one write at the end of the edit session.
    /// </summary>
    internal static PinResult Toggle(PinKey key, string label, string? packageId, bool persist) => Toggle(key, label, packageId, null, persist);

    /// <summary>
    /// Toggles a pin for a designator that is not in the catalog (T-26). The resolved cache gains an entry only when the
    /// catalog knows the key.
    /// </summary>
    internal static PinResult Toggle(PinKey key, string label, string? packageId) => Toggle(key, label, packageId, null, persist: true);

    private static PinKey? StoredKeyFor(CatalogEntry entry)
    {
        if (entry.Key.Kind != PinKeyKind.ModDropdown)
        {
            return null;
        }

        EnsureCurrent();
        PinKey? fuzzy = null;
        foreach (PinEntry? stored in Board.Entries)
        {
            if (stored == null)
            {
                continue;
            }

            if (stored.Key.Equals(entry.Key))
            {
                return null; // an exact pin exists: the normal toggle finds it
            }

            if (fuzzy == null && stored.Key.Kind == PinKeyKind.ModDropdown && _resolved.TryGetValue(stored.Key, out CatalogEntry? resolved) && ReferenceEquals(resolved, entry))
            {
                fuzzy = stored.Key;
            }
        }

        return fuzzy;
    }

    private static PinResult Toggle(PinKey key, string label, string? packageId, CatalogEntry? known, bool persist)
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
        if (persist && feedback.Persist)
        {
            QuickMenuMod.SaveSettings();
        }

        return result;
    }

    private static long Stamp()
    {
        Game? game = Current.Game;
        if (!ReferenceEquals(game, _lastGame))
        {
            _lastGame = game;
            _gameGeneration++; // a different game object (a second save loaded) needs its pins resolved again
        }

        return ((long)_gameGeneration << 32) + ((long)DesignatorCatalog.Version * 4) + (game != null ? 1 : 0) + (Current.ProgramState == ProgramState.Playing ? 2 : 0);
    }

    private static void EnsureCurrent()
    {
        DesignatorCatalog.RefreshIdeology(); // O(1) when nothing changed; a change bumps the catalog version, so the stamp below differs
        long stamp = Stamp();
        if (_stamp != stamp && _failedStamp != stamp)
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

    /// <summary>Reports every pin that is not missing as resolved, so ForgetUnresolved removes exactly the missing ones.</summary>
    private sealed class MissingResolver : IPinResolver
    {
        private readonly Dictionary<PinKey, bool> _missing = new();

        public MissingResolver()
        {
            foreach (PinEntry? entry in Board.Entries)
            {
                if (entry != null)
                {
                    _missing[entry.Key] = IsMissing(entry);
                }
            }
        }

        public bool IsResolved(PinKey key) => !_missing.TryGetValue(key, out bool missing) || !missing;
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
