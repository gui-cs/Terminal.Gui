﻿#nullable enable

using ColorHelper;
using ColorConverter = ColorHelper.ColorConverter;

namespace Terminal.Gui;

internal class HueBar : ColorBar
{
    /// <inheritdoc/>
    protected override int MaxValue => 360;

    /// <inheritdoc/>
    protected override Color GetColor (double fraction)
    {
        var hsl = new HSL ((int)(MaxValue * fraction), 100, 50);
        RGB rgb = ColorConverter.HslToRgb (hsl);

        return new (rgb.R, rgb.G, rgb.B);
    }
}
