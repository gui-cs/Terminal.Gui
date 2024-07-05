namespace Terminal.Gui.TextEffects;
using System;

public delegate float EasingFunction (float progressRatio);

public static class Easing
{
    public static float Linear (float progressRatio)
    {
        return progressRatio;
    }

    public static float InSine (float progressRatio)
    {
        return 1 - (float)Math.Cos ((progressRatio * Math.PI) / 2);
    }

    public static float OutSine (float progressRatio)
    {
        return (float)Math.Sin ((progressRatio * Math.PI) / 2);
    }

    public static float InOutSine (float progressRatio)
    {
        return -(float)(Math.Cos (Math.PI * progressRatio) - 1) / 2;
    }

    public static float InQuad (float progressRatio)
    {
        return progressRatio * progressRatio;
    }

    public static float OutQuad (float progressRatio)
    {
        return 1 - (1 - progressRatio) * (1 - progressRatio);
    }

    public static float InOutQuad (float progressRatio)
    {
        if (progressRatio < 0.5)
        {
            return 2 * progressRatio * progressRatio;
        }
        else
        {
            return 1 - (float)Math.Pow (-2 * progressRatio + 2, 2) / 2;
        }
    }

    public static float InCubic (float progressRatio)
    {
        return progressRatio * progressRatio * progressRatio;
    }

    public static float OutCubic (float progressRatio)
    {
        return 1 - (float)Math.Pow (1 - progressRatio, 3);
    }

    public static float InOutCubic (float progressRatio)
    {
        if (progressRatio < 0.5)
        {
            return 4 * progressRatio * progressRatio * progressRatio;
        }
        else
        {
            return 1 - (float)Math.Pow (-2 * progressRatio + 2, 3) / 2;
        }
    }

    public static float InQuart (float progressRatio)
    {
        return progressRatio * progressRatio * progressRatio * progressRatio;
    }

    public static float OutQuart (float progressRatio)
    {
        return 1 - (float)Math.Pow (1 - progressRatio, 4);
    }

    public static float InOutQuart (float progressRatio)
    {
        if (progressRatio < 0.5)
        {
            return 8 * progressRatio * progressRatio * progressRatio * progressRatio;
        }
        else
        {
            return 1 - (float)Math.Pow (-2 * progressRatio + 2, 4) / 2;
        }
    }

    public static float InQuint (float progressRatio)
    {
        return progressRatio * progressRatio * progressRatio * progressRatio * progressRatio;
    }

    public static float OutQuint (float progressRatio)
    {
        return 1 - (float)Math.Pow (1 - progressRatio, 5);
    }

    public static float InOutQuint (float progressRatio)
    {
        if (progressRatio < 0.5)
        {
            return 16 * progressRatio * progressRatio * progressRatio * progressRatio * progressRatio;
        }
        else
        {
            return 1 - (float)Math.Pow (-2 * progressRatio + 2, 5) / 2;
        }
    }

    public static float InExpo (float progressRatio)
    {
        if (progressRatio == 0)
        {
            return 0;
        }
        else
        {
            return (float)Math.Pow (2, 10 * progressRatio - 10);
        }
    }

    public static float OutExpo (float progressRatio)
    {
        if (progressRatio == 1)
        {
            return 1;
        }
        else
        {
            return 1 - (float)Math.Pow (2, -10 * progressRatio);
        }
    }

    public static float InOutExpo (float progressRatio)
    {
        if (progressRatio == 0)
        {
            return 0;
        }
        else if (progressRatio == 1)
        {
            return 1;
        }
        else if (progressRatio < 0.5)
        {
            return (float)Math.Pow (2, 20 * progressRatio - 10) / 2;
        }
        else
        {
            return (2 - (float)Math.Pow (2, -20 * progressRatio + 10)) / 2;
        }
    }

    public static float InCirc (float progressRatio)
    {
        return 1 - (float)Math.Sqrt (1 - progressRatio * progressRatio);
    }

    public static float OutCirc (float progressRatio)
    {
        return (float)Math.Sqrt (1 - (progressRatio - 1) * (progressRatio - 1));
    }

    public static float InOutCirc (float progressRatio)
    {
        if (progressRatio < 0.5)
        {
            return (1 - (float)Math.Sqrt (1 - (2 * progressRatio) * (2 * progressRatio))) / 2;
        }
        else
        {
            return ((float)Math.Sqrt (1 - (-2 * progressRatio + 2) * (-2 * progressRatio + 2)) + 1) / 2;
        }
    }

    public static float InBack (float progressRatio)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1;
        return c3 * progressRatio * progressRatio * progressRatio - c1 * progressRatio * progressRatio;
    }

    public static float OutBack (float progressRatio)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1;
        return 1 + c3 * (progressRatio - 1) * (progressRatio - 1) * (progressRatio - 1) + c1 * (progressRatio - 1) * (progressRatio - 1);
    }

    public static float InOutBack (float progressRatio)
    {
        const float c1 = 1.70158f;
        const float c2 = c1 * 1.525f;
        if (progressRatio < 0.5)
        {
            return ((2 * progressRatio) * (2 * progressRatio) * ((c2 + 1) * 2 * progressRatio - c2)) / 2;
        }
        else
        {
            return ((2 * progressRatio - 2) * (2 * progressRatio - 2) * ((c2 + 1) * (progressRatio * 2 - 2) + c2) + 2) / 2;
        }
    }

    public static float InElastic (float progressRatio)
    {
        const float c4 = (2 * (float)Math.PI) / 3;
        if (progressRatio == 0)
        {
            return 0;
        }
        else if (progressRatio == 1)
        {
            return 1;
        }
        else
        {
            return -(float)Math.Pow (2, 10 * progressRatio - 10) * (float)Math.Sin ((progressRatio * 10 - 10.75) * c4);
        }
    }

    public static float OutElastic (float progressRatio)
    {
        const float c4 = (2 * (float)Math.PI) / 3;
        if (progressRatio == 0)
        {
            return 0;
        }
        else if (progressRatio == 1)
        {
            return 1;
        }
        else
        {
            return (float)Math.Pow (2, -10 * progressRatio) * (float)Math.Sin ((progressRatio * 10 - 0.75) * c4) + 1;
        }
    }

    public static float InOutElastic (float progressRatio)
    {
        const float c5 = (2 * (float)Math.PI) / 4.5f;
        if (progressRatio == 0)
        {
            return 0;
        }
        else if (progressRatio == 1)
        {
            return 1;
        }
        else if (progressRatio < 0.5)
        {
            return -(float)Math.Pow (2, 20 * progressRatio - 10) * (float)Math.Sin ((20 * progressRatio - 11.125) * c5) / 2;
        }
        else
        {
            return ((float)Math.Pow (2, -20 * progressRatio + 10) * (float)Math.Sin ((20 * progressRatio - 11.125) * c5)) / 2 + 1;
        }
    }

    public static float InBounce (float progressRatio)
    {
        return 1 - OutBounce (1 - progressRatio);
    }

    public static float OutBounce (float progressRatio)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;
        if (progressRatio < 1 / d1)
        {
            return n1 * progressRatio * progressRatio;
        }
        else if (progressRatio < 2 / d1)
        {
            return n1 * (progressRatio - 1.5f / d1) * (progressRatio - 1.5f / d1) + 0.75f;
        }
        else if (progressRatio < 2.5 / d1)
        {
            return n1 * (progressRatio - 2.25f / d1) * (progressRatio - 2.25f / d1) + 0.9375f;
        }
        else
        {
            return n1 * (progressRatio - 2.625f / d1) * (progressRatio - 2.625f / d1) + 0.984375f;
        }
    }

    public static float InOutBounce (float progressRatio)
    {
        if (progressRatio < 0.5)
        {
            return (1 - OutBounce (1 - 2 * progressRatio)) / 2;
        }
        else
        {
            return (1 + OutBounce (2 * progressRatio - 1)) / 2;
        }
    }
}
