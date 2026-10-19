using System;
using System.Diagnostics;
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

    /// <summary>Package id of Dubs Mint Menus; with it active the pin modifier defaults to Alt (FR-8).</summary>
    private const string MintPackageId = "Dubwise.DubsMintMenus";

    private static bool _mintActive;

    /// <summary>
    /// The pin modifier in effect, cached at startup and by <see cref="RefreshEffectiveModifier"/>. The pin prefix reads
    /// only this field, never <c>ModsConfig</c> or the settings.
    /// </summary>
    internal static PinModifier EffectiveModifier { get; private set; } = PinModifier.Ctrl;

    /// <summary>Recomputes <see cref="EffectiveModifier"/> from the settings. Call after the settings load and whenever the pin modifier setting changes.</summary>
    internal static void RefreshEffectiveModifier() =>
        EffectiveModifier = ModifierPolicy.Effective(_settings.PinModifierUserSet, _settings.PinModifier, _mintActive);

    /// <summary>Writes the settings file. Call once per pin, at the end of an edit and when the menu closes, never per frame or per drag step.</summary>
    internal static void SaveSettings()
    {
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
        _instance = this;
        long settingsStart = Stopwatch.GetTimestamp();
        // LoadedModManager.ReadModSettings catches load failures itself and returns fresh settings, so no catch is needed here.
        _settings = GetSettings<QuickMenuSettings>();

        _settings.ApplyCapacity();
        try
        {
            _mintActive = ModsConfig.IsActive(MintPackageId);
        }
        catch (Exception ex)
        {
            Diagnostics.Log.LogOnce("Pinning.MintDetect", $"Detecting Dubs Mint Menus failed, assuming it is not active: {ex}");
        }

        RefreshEffectiveModifier();
        long settingsTicks = Stopwatch.GetTimestamp() - settingsStart;
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
