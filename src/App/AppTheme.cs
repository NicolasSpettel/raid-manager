using Godot;

namespace App;

/// <summary>
/// The app's UI theme — "carved stone" baseline (M1 step 8). Procedural StyleBoxes for now: dark stone
/// panels, beveled stone-slab buttons, engraved bars. This is the single source of visual truth
/// (ui-design-system, BLUEPRINT §8); a later pass swaps these flat styleboxes for texture-based
/// StyleBoxTexture (parchment/iron/marble) + a display font — the reason ADR-0008 chose Godot.
/// </summary>
public static class AppTheme
{
    public static Color Backdrop => new("#1a1a1e");

    public static Color Gold => new("#c9a24b");

    /// <summary>
    /// A full-screen stone backdrop, textured procedurally with tiled Perlin noise (no art assets) —
    /// a first move away from the flat fill toward real texture. A later pass replaces this with
    /// authored parchment/marble/iron textures.
    /// </summary>
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

        var texture = new NoiseTexture2D
        {
            Noise = noise,
            Width = 512,
            Height = 512,
            Seamless = true,
            ColorRamp = ramp,
        };

        var rect = new TextureRect { Texture = texture, StretchMode = TextureRect.StretchModeEnum.Tile };
        rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        rect.MouseFilter = Control.MouseFilterEnum.Ignore;
        return rect;
    }

    public static Theme Build()
    {
        var theme = new Theme { DefaultFontSize = 16 };

        StyleBoxFlat panel = Box("#26262b", "#454549", 2, 4);
        panel.SetContentMarginAll(12);
        theme.SetStylebox("panel", "PanelContainer", panel);
        theme.SetStylebox("panel", "Panel", panel);

        theme.SetStylebox("normal", "Button", Pad(Box("#3a3a3f", "#57575c", 1, 3), 16, 9));
        theme.SetStylebox("hover", "Button", Pad(Box("#46464b", "#66666c", 1, 3), 16, 9));
        theme.SetStylebox("pressed", "Button", Pad(Box("#2c2c30", "#3a3a3f", 1, 3), 16, 9));
        theme.SetColor("font_color", "Button", new Color("#e2ded4"));
        theme.SetColor("font_hover_color", "Button", new Color("#f4f0e4"));
        theme.SetColor("font_pressed_color", "Button", new Color("#cfcbc0"));

        theme.SetColor("font_color", "Label", new Color("#cbc7bb"));

        theme.SetStylebox("background", "ProgressBar", Box("#141419", "#33333a", 1, 3));
        theme.SetStylebox("fill", "ProgressBar", Box("#6f8a3c", "#6f8a3c", 0, 3));
        theme.SetColor("font_color", "ProgressBar", new Color(0, 0, 0, 0));

        theme.SetColor("default_color", "RichTextLabel", new Color("#c4c0b4"));

        theme.SetStylebox("slider", "HSlider", Box("#141419", "#33333a", 1, 3));
        StyleBoxFlat grabber = Box("#7a7a80", "#9a9aa0", 0, 3);
        theme.SetStylebox("grabber_area", "HSlider", grabber);
        theme.SetStylebox("grabber_area_highlight", "HSlider", grabber);

        return theme;
    }

    private static StyleBoxFlat Box(string bg, string border, int borderWidth, int radius)
    {
        var sb = new StyleBoxFlat { BgColor = new Color(bg), BorderColor = new Color(border) };
        sb.SetBorderWidthAll(borderWidth);
        sb.SetCornerRadiusAll(radius);
        return sb;
    }

    private static StyleBoxFlat Pad(StyleBoxFlat sb, int horizontal, int vertical)
    {
        sb.ContentMarginLeft = horizontal;
        sb.ContentMarginRight = horizontal;
        sb.ContentMarginTop = vertical;
        sb.ContentMarginBottom = vertical;
        return sb;
    }
}
