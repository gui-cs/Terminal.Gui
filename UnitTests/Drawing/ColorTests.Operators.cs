using System.Numerics;
using System.Reflection;

namespace Terminal.Gui.DrawingTests;

public partial class ColorTests
{
    [Theory]
    [Trait ("Category", "Operators")]
    [CombinatorialData]
    public void ExplicitOperator_ToVector3_ReturnsCorrectValue (
        [CombinatorialRange (0, 255, 51)] byte r,
        [CombinatorialRange (0, 255, 51)] byte g,
        [CombinatorialRange (0, 255, 51)] byte b,
        [CombinatorialValues (0, 255)] byte a
    )
    {
        Color color = new (r, g, b, a);

        var vector = (Vector3)color;

        Assert.Equal (color.R, vector.X);
        Assert.Equal (color.G, vector.Y);
        Assert.Equal (color.B, vector.Z);
    }

    [Theory]
    [CombinatorialData]
    public void GeneratedEqualityOperators_BehaveAsExpected (
        [CombinatorialValues (0, short.MaxValue, int.MaxValue, uint.MaxValue)]
        uint u1,
        [CombinatorialValues (0, short.MaxValue, int.MaxValue, uint.MaxValue)]
        uint u2
    )
    {
        Color color1 = u1;
        Color color2 = u2;

        if (u1 == u2)
        {
            Assert.True (color1 == color2);
            Assert.False (color1 != color2);
        }
        else
        {
            Assert.True (color1 != color2);
            Assert.False (color1 == color2);
        }
    }

    public static IEnumerable<object[]> ColorValueSetInt32
    {
        get
        {
            for (int colorValue = int.MinValue; colorValue < int.MaxValue; colorValue += 0x0F0F0F0F)
            {
                yield return [colorValue];
            }
        }
    }

    public static IEnumerable<object[]> ColorValueSetUInt32
    {
        get
        {
            for (uint colorValue = uint.MinValue; colorValue < uint.MaxValue; colorValue += 0x0F0F0F0F)
            {
                yield return [colorValue];
            }
        }
    }

    [Theory]

    [MemberData (nameof (ColorValueSetInt32))]
    [Trait ("Category", "Operators")]
    public void GetHashCode_DelegatesTo_Rgba (int rgba)
    {
        Color color = new (rgba);

        Assert.Equal (rgba.GetHashCode (), color.GetHashCode ());
    }

    [Theory]
    [Trait ("Category", "Operators")]
    [MemberData (
                    nameof (ColorTestsTheoryDataGenerators.ExplicitOperator_FromColorName_RoundTripsCorrectly),
                    MemberType = typeof (ColorTestsTheoryDataGenerators)
                )]
    public void ImplicitOperator_FromColorName_ReturnsCorrectColorValue (ColorName cname, Color expectedColor)
    {
        Color color = cname;

        Assert.Equal (expectedColor, color);
    }

    [Theory]
    [MemberData (nameof (ColorValueSetInt32))]
    [Trait ("Category", "Operators")]
    public void ImplicitOperator_FromInt32_ReturnsCorrectColorValue (int expectedValue)
    {
        Color color = expectedValue;

        Assert.Equal (expectedValue, color.Rgba);
    }

    [Theory]
    [MemberData (nameof (ColorValueSetUInt32))]
    [Trait ("Category", "Operators")]
    public void ImplicitOperator_FromUInt32_ReturnsCorrectColorValue (uint expectedValue)
    {
        Color color = expectedValue;

        Assert.Equal (expectedValue, color.Argb);
    }

    [Theory]
    [CombinatorialData]
    [Trait ("Category", "Operators")]
    public void ImplicitOperator_FromVector3_ReturnsCorrectColorValue (
        [CombinatorialRange (0, 255, 51)] byte r,
        [CombinatorialRange (0, 255, 51)] byte g,
        [CombinatorialRange (0, 255, 51)] byte b
    )
    {
        Color color = new Vector3 (r, g, b);

        Assert.Equal (r, color.R);
        Assert.Equal (g, color.G);
        Assert.Equal (b, color.B);
        Assert.Equal (byte.MaxValue, color.A);
    }

    [Theory]
    [CombinatorialData]
    [Trait ("Category", "Operators")]
    public void ImplicitOperator_FromVector4_ReturnsCorrectColorValue (
        [CombinatorialRange (0, 255, 51)] byte r,
        [CombinatorialRange (0, 255, 51)] byte g,
        [CombinatorialRange (0, 255, 51)] byte b,
        [CombinatorialValues (0, 255)] byte a
    )
    {
        Color color = new Vector4 (r, g, b, a);

        Assert.Equal (r, color.R);
        Assert.Equal (g, color.G);
        Assert.Equal (b, color.B);
        Assert.Equal (a, color.A);
    }

    [Theory]
    [MemberData (nameof (ColorValueSetInt32))]
    [Trait ("Category", "Operators")]
    public void ImplicitOperator_ToInt32_ReturnsCorrectInt32Value (int expectedValue)
    {
        int colorAsInt32 = new Color (expectedValue);

        Assert.Equal (expectedValue, colorAsInt32);
    }

    [Theory]
    [MemberData (nameof (ColorValueSetUInt32))]
    [Trait ("Category", "Operators")]
    public void ImplicitOperator_ToUInt32_ReturnsCorrectUInt32Value (uint expectedValue)
    {
        uint colorAsInt32 = new Color (expectedValue);

        Assert.Equal (expectedValue, colorAsInt32);
    }

    [Theory]
    [CombinatorialData]
    [Trait ("Category", "Operators")]
    public void ImplicitOperator_ToVector4_ReturnsCorrectVector4Value (
        [CombinatorialRange (0, 255, 51)] byte r,
        [CombinatorialRange (0, 255, 51)] byte g,
        [CombinatorialRange (0, 255, 51)] byte b,
        [CombinatorialValues (0, 255)] byte a
    )
    {
        Color color = new (r, g, b, a);
        Vector4 vector = color;

        Assert.Equal (r, vector.X);
        Assert.Equal (g, vector.Y);
        Assert.Equal (b, vector.Z);
        Assert.Equal (a, vector.W);
    }
}

public static partial class ColorTestsTheoryDataGenerators
{
    public static TheoryData<ColorName, Color> ExplicitOperator_FromColorName_RoundTripsCorrectly ()
    {
        TheoryData<ColorName, Color> data = []
            ;
        data.Add (ColorName.Black, new Color (12, 12, 12));
        data.Add (ColorName.Blue, new Color (0, 55, 218));
        data.Add (ColorName.Green, new Color (19, 161, 14));
        data.Add (ColorName.Cyan, new Color (58, 150, 221));
        data.Add (ColorName.Red, new Color (197, 15, 31));
        data.Add (ColorName.Magenta, new Color (136, 23, 152));
        data.Add (ColorName.Yellow, new Color (128, 64, 32));
        data.Add (ColorName.Gray, new Color (204, 204, 204));
        data.Add (ColorName.DarkGray, new Color (118, 118, 118));
        data.Add (ColorName.BrightBlue, new Color (59, 120, 255));
        data.Add (ColorName.BrightGreen, new Color (22, 198, 12));
        data.Add (ColorName.BrightCyan, new Color (97, 214, 214));
        data.Add (ColorName.BrightRed, new Color (231, 72, 86));
        data.Add (ColorName.BrightMagenta, new Color (180, 0, 158));
        data.Add (ColorName.BrightYellow, new Color (249, 241, 165));
        data.Add (ColorName.White, new Color (242, 242, 242));

        return data;
    }

    public static TheoryData<FieldInfo, int> Fields_At_Expected_Offsets ()
    {
        TheoryData<FieldInfo, int> data = []
            ;

        data.Add (
                  typeof (Color).GetField (
                                           "Argb",
                                           BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding
                                          ),
                  0
                 );

        data.Add (
                  typeof (Color).GetField (
                                           "Rgba",
                                           BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding
                                          ),
                  0
                 );

        data.Add (
                  typeof (Color).GetField (
                                           "B",
                                           BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding
                                          ),
                  0
                 );

        data.Add (
                  typeof (Color).GetField (
                                           "G",
                                           BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding
                                          ),
                  1
                 );

        data.Add (
                  typeof (Color).GetField (
                                           "R",
                                           BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding
                                          ),
                  2
                 );

        data.Add (
                  typeof (Color).GetField (
                                           "A",
                                           BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding
                                          ),
                  3
                 );

        return data;
    }
}
