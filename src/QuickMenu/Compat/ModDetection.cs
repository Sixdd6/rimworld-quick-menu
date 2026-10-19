using System;
using System.Collections.Generic;
using QuickMenu.Core.Input;
using Verse;

namespace QuickMenu.Compat;

/// <summary>Detects Dubs Mint Menus, Better Architect Menu and Multiplayer once at startup. Only Mint changes behaviour.</summary>
internal static class ModDetection
{
    internal static bool Mint { get; private set; }

    internal static bool BetterArchitect { get; private set; }

    internal static bool Multiplayer { get; private set; }

    internal static void Detect()
    {
        try
        {
            List<ModContentPack> running = LoadedModManager.RunningModsListForReading;
            for (int i = 0; i < running.Count; i++)
            {
                string id = running[i].PackageId;
                Mint |= ModIds.Matches(id, ModIds.Mint);
                BetterArchitect |= ModIds.Matches(id, ModIds.BetterArchitect);
                Multiplayer |= ModIds.Matches(id, ModIds.Multiplayer);
            }
        }
        catch (Exception ex)
        {
            Diagnostics.Log.LogOnce("Compat.Detect", $"Detecting other mods failed, assuming none: {ex}");
        }

        Diagnostics.Log.Info($"Mods detected: Mint={Mint}, BetterArchitect={BetterArchitect}, Multiplayer={Multiplayer}");
    }

    internal static string Summary() => $"mint={Mint} bam={BetterArchitect} multiplayer={Multiplayer}";
}
