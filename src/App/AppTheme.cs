using System;
using Godot;

namespace App;

/// <summary>
/// The app's UI theme — carved-stone, now with procedurally-generated **textured** panels and buttons
/// (Perlin-noise stone fill + a carved dark border + a bevel), framed as nine-patch `StyleBoxTexture`.
/// No art assets — the texture is drawn into an `Image` at startup. This is the single source of visual
/// truth (ui-design-system, BLUEPRINT §8); authored parchment/marble PNGs + a display font can drop in
/// on the same seams later.
/// </summary>
public static class AppTheme
{
    public static Color Backdrop => new("#1a1a1e");

    public static Color Gold => new("#c9a24b");

    public static Theme Build()
    {
        var theme = new Theme { DefaultFontSize = 16 };

        StyleBoxTexture panel = StoneBox(112, new Color("#2b2b31"), border: 12, raised: true);
        SetContentMargin(panel, 14);
        theme.SetStylebox("panel", "PanelContainer", panel);
        theme.SetStylebox("panel", "Panel", panel);

        StyleBoxTexture normal = StoneBox(72, new Color("#3d3d44"), border: 9, raised: true);
        StyleBoxTexture hover = StoneBox(72, new Color("#4a4a52"), border: 9, raised: true);
        StyleBoxTexture pressed = StoneBox(72, new Color("#28282d"), border: 9, raised: false);
        foreach (StyleBoxTexture b in new[] { normal, hover, pressed })
        {
            b.ContentMarginLeft = 16;
            b.ContentMarginRight = 16;
            b.ContentMarginTop = 9;
            b.ContentMarginBottom = 9;
        }

        theme.SetStylebox("normal", "Button", normal);
        theme.SetStylebox("hover", "Button", hover);
        theme.SetStylebox("pressed", "Button", pressed);
        theme.SetColor("font_color", "Button", new Color("#e6e0d2"));
        theme.SetColor("font_hover_color", "Button", new Color("#f6f1e2"));
        theme.SetColor("font_pressed_color", "Button", new Color("#cfcabc"));

        theme.SetColor("font_color", "Label", new Color("#cdc8bb"));

        theme.SetStylebox("background", "ProgressBar", Flat("#141018", "#000000", 1, 3));
        theme.SetStylebox("fill", "ProgressBar", Flat("#6f8a3c", "#6f8a3c", 0, 3));
        theme.SetColor("font_color", "ProgressBar", new Color(0, 0, 0, 0));

        theme.SetColor("default_color", "RichTextLabel", new Color("#c6c1b3"));

        theme.SetStylebox("slider", "HSlider", Flat("#141018", "#33333a", 1, 3));
        StyleBoxFlat grabber = Flat("#8a8a90", "#a6a6ac", 0, 3);
        theme.SetStylebox("grabber_area", "HSlider", grabber);
        theme.SetStylebox("grabber_area_highlight", "HSlider", grabber);

        return theme;
    }

    /// <summary>A full-screen stone backdrop, textured with tiled Perlin noise.</summary>
    public static Control CreateBackdrop()
    {
        var noise = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
            Frequency = 0.015f,
            FractalOctaves = 4,
        };

        var ramp = new Gradient();
        ramp.SetColor(0, new Color("#101013"));
        ramp.SetColor(1, new Color("#28282f"));

        var texture = new NoiseTexture2D { Noise = noise, Width = 512, Height = 512, Seamless = true, ColorRamp = ramp };
        var rect = new TextureRect { Texture = texture, StretchMode = TextureRect.StretchModeEnum.Tile };
        rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        rect.MouseFilter = Control.MouseFilterEnum.Ignore;
        return rect;
    }

    // Draw a stone tile: noise fill + a carved dark border + a diagonal bevel (raised or inset).
    private static StyleBoxTexture StoneBox(int size, Color mid, int border, bool raised)
    {
        var noise = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
            Frequency = 0.11f,
            FractalOctaves = 3,
        };

        Image img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float shade = 1f + (noise.GetNoise2D(x, y) * 0.16f);
                var c = new Color(mid.R * shade, mid.G * shade, mid.B * shade, 1f);

                int edge = Math.Min(Math.Min(x, y), Math.Min(size - 1 - x, size - 1 - y));
                if (edge < border)
                {
                    c = c.Darkened(0.55f * (1f - (edge / (float)border)));
                }

                float diag = (((size - x) + (size - y)) / (float)(2 * size)) - 0.5f; // + top-left, - bottom-right
                float b = diag * (raised ? 0.4f : -0.3f);
                c = b >= 0f ? c.Lightened(b) : c.Darkened(-b);

                img.SetPixel(x, y, c);
            }
        }

        var box = new StyleBoxTexture { Texture = ImageTexture.CreateFromImage(img) };
        box.TextureMarginLeft = border;
        box.TextureMarginRight = border;
        box.TextureMarginTop = border;
        box.TextureMarginBottom = border;
        return box;
    }

    private static void SetContentMargin(StyleBoxTexture box, int margin)
    {
        box.ContentMarginLeft = margin;
        box.ContentMarginRight = margin;
        box.ContentMarginTop = margin;
        box.ContentMarginBottom = margin;
    }

    private static StyleBoxFlat Flat(string bg, string border, int borderWidth, int radius)
    {
        var sb = new StyleBoxFlat { BgColor = new Color(bg), BorderColor = new Color(border) };
        sb.SetBorderWidthAll(borderWidth);
        sb.SetCornerRadiusAll(radius);
        return sb;
    }
}
