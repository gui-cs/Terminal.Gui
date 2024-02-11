﻿using System.Numerics;
using System.Runtime.CompilerServices;

namespace Terminal.Gui.DrawingTests;

public partial class ColorTests
{
    [Fact]
    [Trait ("Category", "Type Checks")]
    [Trait ("Category", "Change Control")]
    public void ColorName_Has_Exactly_16_Defined_Values () { Assert.Equal (16, Enum.GetValues<ColorName> ().DistinctBy (static cname => (int)cname).Count ()); }

    [Theory]
    [Trait ("Category", "Type Checks")]
    [Trait ("Category", "Change Control")]
    [MemberData (
                    nameof (ColorTestsTheoryDataGenerators.ColorName_HasCorrectOrdinals),
                    MemberType = typeof (ColorTestsTheoryDataGenerators)
                )]
    public void ColorName_HasCorrectOrdinals (ColorName cname, int ordinal) { Assert.Equal ((int)cname, ordinal); }

    [Fact]
    [Trait ("Category", "Type Checks")]
    [Trait ("Category", "Change Control")]
    [SkipLocalsInit]
    public unsafe void Fields_At_Expected_Offsets ()
    {
        // Raw write to the stack and read-back as a Color
        // Byte order is little endian
        Color* c = stackalloc Color [1];
        var rgba = (int*)c;
        *rgba = 0;

        // Pre-conditions. Ensure everything is zero;
        Assert.Equal (0, c->Rgba);
        Assert.Equal (0u, c->Argb);
        Assert.Equal ((byte)0, c->R);
        Assert.Equal ((byte)0, c->G);
        Assert.Equal ((byte)0, c->B);
        Assert.Equal ((byte)0, c->A);

        var bytePointer = (byte*)c;

        // Write value to first byte and read it back in B
        *bytePointer = 239;
        Assert.Equal (239, c->Rgba);
        Assert.Equal (239u, c->Argb);
        Assert.Equal ((byte)239, c->B);
        Assert.Equal ((byte)0, c->G);
        Assert.Equal ((byte)0, c->R);
        Assert.Equal ((byte)0, c->A);

        // Move to offset 1, write the next value, and check everything again.
        bytePointer++;
        *bytePointer = 190;
        Assert.Equal (48879, c->Rgba);
        Assert.Equal (48879u, c->Argb);
        Assert.Equal ((byte)239, c->B);
        Assert.Equal ((byte)190, c->G);
        Assert.Equal ((byte)0, c->R);
        Assert.Equal ((byte)0, c->A);

        // Move to offset 2, write the next value, and check everything again.
        bytePointer++;
        *bytePointer = 173;
        Assert.Equal (11386607, c->Rgba);
        Assert.Equal (11386607u, c->Argb);
        Assert.Equal ((byte)239, c->B);
        Assert.Equal ((byte)190, c->G);
        Assert.Equal ((byte)173, c->R);
        Assert.Equal ((byte)0, c->A);

        // Move to offset 3, write the next value, and check everything again.
        bytePointer++;
        *bytePointer = 222;
        Assert.Equal (-559038737, c->Rgba);
        Assert.Equal (0x_DEAD_BEEF, c->Argb);
        Assert.Equal ((byte)239, c->B);
        Assert.Equal ((byte)190, c->G);
        Assert.Equal ((byte)173, c->R);
        Assert.Equal ((byte)222, c->A);
    }

    [Theory]
    [Trait ("Category", "Type Checks")]
    [Trait ("Category", "Change Control")]
    [CombinatorialData]
    public void Implements_Expected_Interfaces (
        [CombinatorialValues (
                                 typeof (IEquatable<Color>),
                                 typeof (ISpanParsable<Color>),
                                 typeof (IUtf8SpanParsable<Color>),
                                 typeof (ISpanFormattable),
                                 typeof (IUtf8SpanFormattable),
                                 typeof (IMinMaxValue<Color>)
                             )]
        Type expectedInterface
    )
    {
        Assert.Contains (expectedInterface, typeof (Color).GetInterfaces ());
    }

    [Fact]
    [Trait ("Category", "Type Checks")]
    [Trait ("Category", "Change Control")]
    public void Is_Explicit_LayoutKind () { Assert.True (typeof (Color).IsExplicitLayout); }

    [Fact]
    [Trait ("Category", "Type Checks")]
    [Trait ("Category", "Change Control")]
    public void Is_Value_Type ()
    {
        // prove that Color is a value type
        Assert.True (typeof (Color).IsValueType);
    }
}

public static partial class ColorTestsTheoryDataGenerators
{
    public static TheoryData<ColorName, int> ColorName_HasCorrectOrdinals ()
    {
        TheoryData<ColorName, int> data = []
            ;
        data.Add (ColorName.Black, 0);
        data.Add (ColorName.Blue, 1);
        data.Add (ColorName.Green, 2);
        data.Add (ColorName.Cyan, 3);
        data.Add (ColorName.Red, 4);
        data.Add (ColorName.Magenta, 5);
        data.Add (ColorName.Yellow, 6);
        data.Add (ColorName.Gray, 7);
        data.Add (ColorName.DarkGray, 8);
        data.Add (ColorName.BrightBlue, 9);
        data.Add (ColorName.BrightGreen, 10);
        data.Add (ColorName.BrightCyan, 11);
        data.Add (ColorName.BrightRed, 12);
        data.Add (ColorName.BrightMagenta, 13);
        data.Add (ColorName.BrightYellow, 14);
        data.Add (ColorName.White, 15);

        return data;
    }
}
