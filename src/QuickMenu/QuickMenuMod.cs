using System;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using HarmonyLib;
using QuickMenu.Core.Input;
using QuickMenu.Diagnostics;
using QuickMenu.Menu;
using QuickMenu.Pinning;
using QuickMenu.Settings;
using QuickMenu.Trigger;
using RimWorld;
using Verse;

namespace QuickMenu;

/// <summary>Mod entry point: applies the Harmony patches. A failure is logged once and never stops loading.</summary>
public sealed class QuickMenuMod : Mod
{
    internal const string HarmonyId = "sixdd.QuickMenu";

    private static QuickMenuMod? _instance;
    private static QuickMenuSettings _settings = new();

    /// <summary>The loaded settings (defaults until the mod is constructed or when loading failed).</summary>
    internal static QuickMenuSettings Settings => _settings;

    /// <summary>Whether Dubs Mint Menus was active when the game started; the pin modifier then defaults to Alt (FR-8).</summary>
    internal static bool MintActive => _mintActive;

    private static bool _mintActive;

    /// <summary>Opens this mod's page in the game's mod settings window.</summary>
    internal static void OpenSettings()
    {
        if (_instance != null)
        {
            Find.WindowStack.Add(new Dialog_ModSettings(_instance));
        }
    }

    /// <summary>
    /// The pin modifier in effect, cached at startup and by <see cref="RefreshEffectiveModifier"/>. The pin prefix reads
    /// only this field, never <c>ModsConfig</c> or the settings.
    /// </summary>
    internal static PinModifier EffectiveModifier { get; private set; } = PinModifier.Ctrl;

    /// <summary>Recomputes <see cref="EffectiveModifier"/> from the settings. Call after the settings load and whenever the pin modifier setting changes.</summary>
    internal static void RefreshEffectiveModifier() =>
        EffectiveModifier = ModifierPolicy.Effective(_settings.PinModifierUserSet, _settings.PinModifier, _mintActive);

    /// <summary>How many times the settings were written this session (the self-test shows it: one per edit session, not per drag step).</summary>
    internal static int SaveCount { get; private set; }

    /// <summary>Writes the settings file. Call once per pin, at the end of an edit and when the menu closes, never per frame or per drag step.</summary>
    internal static void SaveSettings()
    {
        SaveCount++;
        try
        {
            _instance?.WriteSettings();
        }
        catch (Exception ex)
        {
            Diagnostics.Log.LogOnce("Settings.Write", $"Writing the settings failed: {ex}");
        }
    }

    public QuickMenuMod(ModContentPack content)
        : base(content)
    {
        long started = Stopwatch.GetTimestamp();
        TimeSpan jitStart = JitInfo.GetCompilationTime(currentThread: true);
        long jitMethodsStart = JitInfo.GetCompiledMethodCount(currentThread: true);
        _instance = this;

        // The game's own work (settings file read, Scribe, our ExposeData) is timed apart from ours, so P-16 can be attributed.
        long getStart = Stopwatch.GetTimestamp();
        // LoadedModManager.ReadModSettings catches load failures itself and returns fresh settings, so no catch is needed here.
        _settings = GetSettings<QuickMenuSettings>();
        long getTicks = Stopwatch.GetTimestamp() - getStart;
        TimeSpan jitAfterGet = JitInfo.GetCompilationTime(currentThread: true);
        long jitMethodsAfterGet = JitInfo.GetCompiledMethodCount(currentThread: true);

        long ownStart = Stopwatch.GetTimestamp();
        _settings.ApplyCapacity();
        Compat.ModDetection.Detect();
        _mintActive = Compat.ModDetection.Mint;
        RefreshEffectiveModifier();
        long ownTicks = Stopwatch.GetTimestamp() - ownStart;
        long settingsTicks = getTicks + ownTicks;
        PerfStartup.NoteSettings(
            getTicks,
            QuickMenuSettings.ScribeTicks,
            QuickMenuSettings.PostLoadTicks,
            ownTicks,
            jitAfterGet - jitStart,
            jitMethodsAfterGet - jitMethodsStart);
        long patchStart = Stopwatch.GetTimestamp();
        try
        {
            ApplyPatches();
        }
        catch (Exception ex)
        {
            Diagnostics.Log.LogOnce("Harmony.Bootstrap", $"Harmony is unavailable, Quick Menu patches were not applied: {ex}");
        }

        long end = Stopwatch.GetTimestamp();
        PerfStartup.Note(end - started, end - patchStart, settingsTicks);
    }

    public override string SettingsCategory() => "QuickMenu.Settings.Title".Translate();

    public override void DoSettingsWindowContents(UnityEngine.Rect inRect) => SettingsPage.Draw(inRect);

    // Isolated and not inlined so a missing 0Harmony assembly fails here, inside the caller's try block.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ApplyPatches()
    {
        TriggerPoller.OpenRequested = MenuController.Open;
        var harmony = new Harmony(HarmonyId);
        ApplyPostfix(harmony, TriggerPoller.Site, typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootUpdate), typeof(TriggerPoller), nameof(TriggerPoller.Postfix));
        PatchGuard.Run(PinClickPatch.Site, () => PinClickPatch.Apply(harmony));
        DropdownEntryPatch.Apply(harmony);
    }

    private static void ApplyPostfix(Harmony harmony, Diagnostics.PatchSite site, Type targetType, string targetMethod, Type patchType, string postfix)
    {
        try
        {
            var original = AccessTools.Method(targetType, targetMethod);
            if (original == null)
            {
                site.Fail(new MissingMethodException(targetType.FullName, targetMethod));
                return;
            }

            harmony.Patch(original, postfix: new HarmonyMethod(patchType, postfix));
        }
        catch (Exception ex)
        {
            site.Fail(ex);
        }
    }
}
