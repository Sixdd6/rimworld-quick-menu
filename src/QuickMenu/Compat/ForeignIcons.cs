using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using QuickMenu.Core.Render;
using QuickMenu.Menu;
using UnityEngine;
using Verse;
using Log = QuickMenu.Diagnostics.Log;

namespace QuickMenu.Compat;

/// <summary>
/// Icons of designators that have no <see cref="Command.icon"/> because their mod paints the button art itself (user decision
/// 2026-10-27). More Planning's tools draw theirs in a protected <c>BaseDesignator.DrawToolbarIcon(Rect)</c>, which is called
/// through a delegate built once. Any other icon-less designator that keeps <see cref="Command.DrawIcon"/> gets a neutral
/// placeholder instead of the game's red "bad texture" cross. Lookups after the first per type allocate nothing.
/// </summary>
internal static class ForeignIcons
{
    private const string MorePlanningBase = "MorePlanning.Designators.BaseDesignator";
    private const float PlaceholderFraction = 0.4f;

    private static readonly Dictionary<Type, Kind> _kinds = new();
    private static Action<Designator, Rect>? _morePlanningDraw;
    private static Type? _morePlanningBase;
    private static bool _resolved;

    private enum Kind
    {
        Own,
        MorePlanning,
        Placeholder,
    }

    /// <summary>
    /// Draws the icon of <paramref name="command"/> in <paramref name="rect"/> when the command cannot draw a usable one itself.
    /// Returns false when the caller should use <see cref="Command.DrawIcon"/> as usual.
    /// </summary>
    internal static bool TryDraw(Command command, Rect rect)
    {
        switch (KindOf(command))
        {
            case Kind.MorePlanning when command is Designator designator && _morePlanningDraw != null:
                _morePlanningDraw(designator, rect);
                return true;
            case Kind.Placeholder:
                DrawPlaceholder(rect);
                return true;
            default:
                return false;
        }
    }

    /// <summary>True when the command would draw the game's bad-texture cross: no icon and no <see cref="Command.DrawIcon"/> override.</summary>
    internal static bool LacksIcon(Command command) => command.icon == null && !OverridesDrawIcon(command.GetType());

    private static Kind KindOf(Command command)
    {
        if (command.icon != null)
        {
            return Kind.Own;
        }

        Type type = command.GetType();
        if (!_kinds.TryGetValue(type, out Kind kind))
        {
            kind = Classify(type);
            _kinds[type] = kind;
        }

        return kind;
    }

    private static Kind Classify(Type type)
    {
        ResolveMorePlanning();
        if (_morePlanningBase != null && _morePlanningDraw != null && _morePlanningBase.IsAssignableFrom(type))
        {
            return Kind.MorePlanning;
        }

        return OverridesDrawIcon(type) ? Kind.Own : Kind.Placeholder;
    }

    private static bool OverridesDrawIcon(Type type)
    {
        MethodInfo? method = AccessTools.Method(type, nameof(Command.DrawIcon), new[] { typeof(Rect), typeof(Material), typeof(GizmoRenderParms) });
        return method != null && method.DeclaringType != typeof(Command);
    }

    private static void ResolveMorePlanning()
    {
        if (_resolved)
        {
            return;
        }

        _resolved = true;
        try
        {
            _morePlanningBase = AccessTools.TypeByName(MorePlanningBase);
            MethodInfo? draw = _morePlanningBase == null ? null : AccessTools.Method(_morePlanningBase, "DrawToolbarIcon", new[] { typeof(Rect) });
            if (draw == null || !typeof(Designator).IsAssignableFrom(_morePlanningBase))
            {
                return;
            }

            // A protected virtual method: an open delegate over a dynamic method that casts and calls it virtually.
            var dm = new DynamicMethod("QuickMenu_MorePlanningDrawToolbarIcon", typeof(void), new[] { typeof(Designator), typeof(Rect) }, typeof(ForeignIcons).Module, skipVisibility: true);
            ILGenerator il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, _morePlanningBase);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Callvirt, draw);
            il.Emit(OpCodes.Ret);
            _morePlanningDraw = (Action<Designator, Rect>)dm.CreateDelegate(typeof(Action<Designator, Rect>));
        }
        catch (Exception ex)
        {
            _morePlanningDraw = null;
            Log.LogOnce("Compat.MorePlanningIcons", $"More Planning's icon drawing could not be reached, its icon-less tools get a placeholder: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void DrawPlaceholder(Rect rect)
    {
        Texture2D? disc = Textures.Ready ? Textures.Mask(MaskId.Disc) : null;
        if (disc == null)
        {
            return;
        }

        float side = Mathf.Min(rect.width, rect.height) * PlaceholderFraction;
        Color old = GUI.color;
        GUI.color = QMStyle.TextMuted;
        GUI.DrawTexture(new Rect(rect.center.x - (side / 2f), rect.center.y - (side / 2f), side, side), disc);
        GUI.color = old;
    }
}
