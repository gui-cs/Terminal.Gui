using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Terminal.Gui.TextEffects;

using Color = Terminal.Gui.TextEffects.Color;

public static class PositiveInt
{
    public static int Parse (string arg)
    {
        if (int.TryParse (arg, out int value) && value > 0)
        {
            return value;
        }
        else
        {
            throw new ArgumentException ($"invalid value: '{arg}' is not > 0.");
        }
    }
}

public static class NonNegativeInt
{
    public static int Parse (string arg)
    {
        if (int.TryParse (arg, out int value) && value >= 0)
        {
            return value;
        }
        else
        {
            throw new ArgumentException ($"invalid value: '{arg}' Argument must be int >= 0.");
        }
    }
}

public static class IntRange
{
    public static (int, int) Parse (string arg)
    {
        var parts = arg.Split ('-');
        if (parts.Length == 2 && int.TryParse (parts [0], out int start) && int.TryParse (parts [1], out int end) && start > 0 && start <= end)
        {
            return (start, end);
        }
        else
        {
            throw new ArgumentException ($"invalid range: '{arg}' is not a valid range. Must be start-end. Ex: 1-10");
        }
    }
}

public static class PositiveFloat
{
    public static float Parse (string arg)
    {
        if (float.TryParse (arg, out float value) && value > 0)
        {
            return value;
        }
        else
        {
            throw new ArgumentException ($"invalid value: '{arg}' is not a valid value. Argument must be a float > 0.");
        }
    }
}

public static class NonNegativeFloat
{
    public static float Parse (string arg)
    {
        if (float.TryParse (arg, out float value) && value >= 0)
        {
            return value;
        }
        else
        {
            throw new ArgumentException ($"invalid argument value: '{arg}' is out of range. Must be float >= 0.");
        }
    }
}

public static class PositiveFloatRange
{
    public static (float, float) Parse (string arg)
    {
        var parts = arg.Split ('-');
        if (parts.Length == 2 && float.TryParse (parts [0], out float start) && float.TryParse (parts [1], out float end) && start > 0 && start <= end)
        {
            return (start, end);
        }
        else
        {
            throw new ArgumentException ($"invalid range: '{arg}' is not a valid range. Must be start-end. Ex: 0.1-1.0");
        }
    }
}

public static class Ratio
{
    public static float Parse (string arg)
    {
        if (float.TryParse (arg, out float value) && value >= 0 && value <= 1)
        {
            return value;
        }
        else
        {
            throw new ArgumentException ($"invalid value: '{arg}' is not a float >= 0 and <= 1. Example: 0.5");
        }
    }
}

public enum GradientDirection
{
    Horizontal,
    Vertical,
    Diagonal,
    Radial
}

public static class GradientDirectionParser
{
    public static GradientDirection Parse (string arg)
    {
        return arg.ToLower () switch
        {
            "horizontal" => GradientDirection.Horizontal,
            "vertical" => GradientDirection.Vertical,
            "diagonal" => GradientDirection.Diagonal,
            "radial" => GradientDirection.Radial,
            _ => throw new ArgumentException ($"invalid gradient direction: '{arg}' is not a valid gradient direction. Choices are diagonal, horizontal, vertical, or radial."),
        };
    }
}

public static class ColorArg
{
    public static Color Parse (string arg)
    {
        if (int.TryParse (arg, out int xtermValue) && xtermValue >= 0 && xtermValue <= 255)
        {
            return new Color (xtermValue);
        }
        else if (arg.Length == 6 && int.TryParse (arg, NumberStyles.HexNumber, null, out int _))
        {
            return new Color (arg);
        }
        else
        {
            throw new ArgumentException ($"invalid color value: '{arg}' is not a valid XTerm or RGB color. Must be in range 0-255 or 000000-FFFFFF.");
        }
    }
}

public static class Symbol
{
    public static string Parse (string arg)
    {
        if (arg.Length == 1 && IsAsciiOrUtf8 (arg))
        {
            return arg;
        }
        else
        {
            throw new ArgumentException ($"invalid symbol: '{arg}' is not a valid symbol. Must be a single ASCII/UTF-8 character.");
        }
    }

    private static bool IsAsciiOrUtf8 (string s)
    {
        try
        {
            Encoding.ASCII.GetBytes (s);
        }
        catch (EncoderFallbackException)
        {
            try
            {
                Encoding.UTF8.GetBytes (s);
            }
            catch (EncoderFallbackException)
            {
                return false;
            }
        }
        return true;
    }
}

public static class CanvasDimension
{
    public static int Parse (string arg)
    {
        if (int.TryParse (arg, out int value) && value >= -1)
        {
            return value;
        }
        else
        {
            throw new ArgumentException ($"invalid value: '{arg}' is not >= -1.");
        }
    }
}

public static class TerminalDimensions
{
    public static (int, int) Parse (string arg)
    {
        var parts = arg.Split (' ');
        if (parts.Length == 2 && int.TryParse (parts [0], out int width) && int.TryParse (parts [1], out int height) && width >= 0 && height >= 0)
        {
            return (width, height);
        }
        else
        {
            throw new ArgumentException ($"invalid terminal dimensions: '{arg}' is not a valid terminal dimension. Must be >= 0.");
        }
    }
}

public static class Ease
{
    private static readonly Dictionary<string, EasingFunction> easingFuncMap = new ()
    {
        {"linear", Easing.Linear},
        {"in_sine", Easing.InSine},
        {"out_sine", Easing.OutSine},
        {"in_out_sine", Easing.InOutSine},
        {"in_quad", Easing.InQuad},
        {"out_quad", Easing.OutQuad},
        {"in_out_quad", Easing.InOutQuad},
        {"in_cubic", Easing.InCubic},
        {"out_cubic", Easing.OutCubic},
        {"in_out_cubic", Easing.InOutCubic},
        {"in_quart", Easing.InQuart},
        {"out_quart", Easing.OutQuart},
        {"in_out_quart", Easing.InOutQuart},
        {"in_quint", Easing.InQuint},
        {"out_quint", Easing.OutQuint},
        {"in_out_quint", Easing.InOutQuint},
        {"in_expo", Easing.InExpo},
        {"out_expo", Easing.OutExpo},
        {"in_out_expo", Easing.InOutExpo},
        {"in_circ", Easing.InCirc},
        {"out_circ", Easing.OutCirc},
        {"in_out_circ", Easing.InOutCirc},
        {"in_back", Easing.InBack},
        {"out_back", Easing.OutBack},
        {"in_out_back", Easing.InOutBack},
        {"in_elastic", Easing.InElastic},
        {"out_elastic", Easing.OutElastic},
        {"in_out_elastic", Easing.InOutElastic},
        {"in_bounce", Easing.InBounce},
        {"out_bounce", Easing.OutBounce},
        {"in_out_bounce", Easing.InOutBounce},
    };

    public static EasingFunction Parse (string arg)
    {
        if (easingFuncMap.TryGetValue (arg.ToLower (), out var easingFunc))
        {
            return easingFunc;
        }
        else
        {
            throw new ArgumentException ($"invalid ease value: '{arg}' is not a valid ease.");
        }
    }
}
