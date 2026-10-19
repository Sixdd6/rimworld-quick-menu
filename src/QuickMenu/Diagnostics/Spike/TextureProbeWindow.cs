using System;
using System.Text;
using LudeonTK;
using QuickMenu.Core.Render;
using UnityEngine;
using Verse;

namespace QuickMenu.Diagnostics.Spike;

/// <summary>
/// Spike S-7 probe: shows side by side (1) a runtime <c>Texture2D</c> from <see cref="MaskRasterizer.Circle(int)"/>,
/// (2) the shipped <c>SpikeCircle.png</c> through <c>ContentFinder</c>, (3) a generated 9-slice rounded rect through
/// <c>Widgets.DrawAtlas</c>. Every step is guarded and its outcome logged. Dev-only; allocation is accepted.
/// </summary>
internal sealed class TextureProbeWindow : Window
{
    private const int MaskSize = 256;

    // Widgets.DrawAtlas (1.7.4982, verified from IL) uses corner = min(atlas.width * 0.25, rect.width / 2, rect.height / 2),
    // so the rounded corner must fit in AtlasSize / 4: radius 14 <= 64 / 4 = 16.
    private const int AtlasSize = 64;
    private const float AtlasRadius = 14f;
    private static readonly float[] _sizes = { 36f, 56f, 96f };
    private static readonly Color _cyanHalf = new(0f, 1f, 1f, 0.5f);

    private readonly StringBuilder _report = new();
    private Texture2D? _mipped;
    private Texture2D? _runtime;
    private Texture2D? _png;
    private Texture2D? _atlas;
    private bool _drawFailed;

    public TextureProbeWindow()
    {
        doCloseX = true;
        doCloseButton = false;
        draggable = true;
        absorbInputAroundWindow = false;
        preventCameraMotion = false;
        closeOnClickedOutside = false;
        closeOnAccept = false;
        closeOnCancel = false;
        _report.AppendLine(Log.Prefix + "BEGIN texture probe");
        _report.AppendLine("UI scale=" + Prefs.UIScale + " screen=" + UI.screenWidth + "x" + UI.screenHeight);
        _runtime = Step("runtime circle", () => FromMask(MaskRasterizer.Circle(MaskSize), MaskSize, MaskSize));
        _png = Step("ContentFinder SpikeCircle.png", () => ContentFinder<Texture2D>.Get("QuickMenu/Spike/SpikeCircle", false));
        if (_png != null)
        {
            _report.AppendLine("png size=" + _png.width + "x" + _png.height + " format=" + _png.format + " mipmapCount=" + _png.mipmapCount);
        }

        if (_runtime != null)
        {
            _report.AppendLine("runtime size=" + _runtime.width + "x" + _runtime.height + " format=" + _runtime.format + " mipmapCount=" + _runtime.mipmapCount);
        }

        _mipped = Step("runtime circle with mipmaps", () => FromMask(MaskRasterizer.Circle(MaskSize), MaskSize, MaskSize, true));
        if (_mipped != null)
        {
            _report.AppendLine("runtime+mips mipmapCount=" + _mipped.mipmapCount);
        }

        _atlas = Step("9-slice rounded rect", () => FromMask(MaskRasterizer.RoundedRect(AtlasSize, AtlasSize, AtlasRadius), AtlasSize, AtlasSize));
        _report.AppendLine("Draw happens every frame; draw failures are logged once below.");
    }

    public override Vector2 InitialSize => new(560f, 560f);

    [DebugAction("Quick Menu", "texture probe", allowedGameStates = AllowedGameStates.Playing)]
    private static void Open()
    {
        var existing = Find.WindowStack.WindowOfType<TextureProbeWindow>();
        if (existing != null)
        {
            existing.LogReport();
            return;
        }

        Find.WindowStack.Add(new TextureProbeWindow());
    }

    public override void DoWindowContents(Rect inRect)
    {
        if (_drawFailed)
        {
            return;
        }

        var saved = GUI.color;
        try
        {
            var y = inRect.y;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 24f), "1 runtime Texture2D (white, cyan a0.5)  |  2 PNG via ContentFinder  |  3 runtime with mipmaps");
            y += 28f;
            DrawRow(_runtime, inRect.x, y);
            y += 100f;
            DrawRow(_png, inRect.x, y);
            y += 100f;
            DrawRow(_mipped, inRect.x, y);
            y += 100f;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 24f), "4 Widgets.DrawAtlas 9-slice at two sizes");
            y += 28f;
            if (_atlas != null)
            {
                GUI.color = Color.white;
                Widgets.DrawAtlas(new Rect(inRect.x, y, 120f, 60f), _atlas);
                GUI.color = _cyanHalf;
                Widgets.DrawAtlas(new Rect(inRect.x + 140f, y, 300f, 80f), _atlas);
            }

            if (Widgets.ButtonText(new Rect(inRect.xMax - 120f, inRect.yMax - 30f, 120f, 30f), "Log report"))
            {
                LogReport();
            }
        }
        catch (Exception ex)
        {
            _drawFailed = true;
            Log.LogOnce("TextureProbe.draw", "texture probe draw failed: " + ex);
        }
        finally
        {
            GUI.color = saved;
        }
    }

    public override void PostClose()
    {
        base.PostClose();
        DestroyTexture(_runtime);
        DestroyTexture(_mipped);
        DestroyTexture(_atlas);
        _runtime = null;
        _mipped = null;
        _atlas = null;
        LogReport();
    }

    internal void LogReport()
    {
        Verse.Log.Message(_report + Log.Prefix + "END texture probe");
    }

    private static void DestroyTexture(Texture2D? texture)
    {
        if (texture != null)
        {
            UnityEngine.Object.Destroy(texture);
        }
    }

    private static void DrawRow(Texture2D? texture, float x, float y)
    {
        if (texture == null)
        {
            return;
        }

        var saved = GUI.color;
        var cx = x;
        for (var tint = 0; tint < 2; tint++)
        {
            GUI.color = tint == 0 ? Color.white : _cyanHalf;
            for (var i = 0; i < _sizes.Length; i++)
            {
                var s = _sizes[i];
                GUI.DrawTexture(new Rect(cx, y, s, s), texture);
                cx += s + 8f;
            }

            cx += 16f;
        }

        GUI.color = saved;
    }

    /// <summary>Rows are top-first in the rasterizer; Unity expects bottom-first, so rows are flipped.</summary>
    private static Texture2D FromMask(byte[] mask, int width, int height, bool mipmaps = false)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, mipmaps)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
        var pixels = new Color32[width * height];
        for (var row = 0; row < height; row++)
        {
            var dst = (height - 1 - row) * width;
            var src = row * width;
            for (var col = 0; col < width; col++)
            {
                pixels[dst + col] = new Color32(255, 255, 255, mask[src + col]);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(mipmaps);
        return texture;
    }

    private Texture2D? Step(string name, Func<Texture2D?> create)
    {
        try
        {
            var texture = create();
            _report.AppendLine(name + ": " + (texture != null ? "OK " + texture.width + "x" + texture.height : "FAILED (null)"));
            return texture;
        }
        catch (Exception ex)
        {
            _report.AppendLine(name + ": EXCEPTION " + ex);
            return null;
        }
    }
}
