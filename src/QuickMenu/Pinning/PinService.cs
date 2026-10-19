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

namespace QuickMenu.Pinning;

/// <summary>
/// Pin and unpin through the settings board (FR-5, FR-15). Stored pins are resolved against the catalog once, after the
/// catalog is built; opening the menu reads that cache and does no catalog work.
/// </summary>
internal static class PinService
{
    internal static readonly PatchSite Site = new("Pinning.Service");

    private static readonly Dictionary<PinKey, CatalogEntry> _resolved = new();
    private static bool _initialized;

    /// <summary>Resolves every stored pin against the catalog key map. Call after the catalog build, on the main thread.</summary>
    internal static void Initialize()
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

            _initialized = true;
        }
        catch (Exception ex)
        {
            Site.Fail(ex);
        }
    }

    /// <summary>Answers from the cache built by <see cref="Initialize"/>; never touches the catalog.</summary>
    internal static IPinResolver Resolver { get; } = new CacheResolver();

    /// <summary>The catalog entry of a stored pin, or null when it is unresolved (mod removed, FR-14).</summary>
    internal static CatalogEntry? Entry(PinKey key) => _resolved.TryGetValue(key, out CatalogEntry? e) ? e : null;

    internal static PinBoard Board => QuickMenuMod.Settings.Board;

    /// <summary>Toggles the pin of <paramref name="entry"/>, shows the S9 message with its sound and writes the settings once.</summary>
    internal static PinResult Toggle(CatalogEntry entry)
    {
        PinResult result = Board.Toggle(entry.Key, entry.Label, entry.PackageId);
        if (result == PinResult.Unpinned)
        {
            _resolved.Remove(entry.Key);
        }
        else if (result == PinResult.Pinned)
        {
            _resolved[entry.Key] = entry;
        }

        PinFeedback feedback = PinFeedback.For(result);
        Announce(feedback, entry.Label);
        if (feedback.Persist)
        {
            QuickMenuMod.SaveSettings();
        }

        return result;
    }

    private static void Announce(PinFeedback feedback, string label)
    {
        try
        {
            TaggedString text = feedback.UsesLabel ? feedback.MessageKey.Translate(label) : feedback.MessageKey.Translate();
            Messages.Message(text, feedback.IsReject ? MessageTypeDefOf.RejectInput : MessageTypeDefOf.SilentInput, historical: false);
            SoundDef sound = feedback.Sound switch
            {
                PinSound.TickHigh => SoundDefOf.Tick_High,
                PinSound.TickLow => SoundDefOf.Tick_Low,
                _ => SoundDefOf.ClickReject,
            };
            sound.PlayOneShotOnCamera();
        }
        catch (Exception ex)
        {
            Site.Fail(ex);
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
        public bool IsResolved(PinKey key) => _initialized && _resolved.ContainsKey(key);
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
