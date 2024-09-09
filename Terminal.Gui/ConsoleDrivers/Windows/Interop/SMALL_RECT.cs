namespace Terminal.Gui.ConsoleDrivers.Windows.Interop;

using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.UnmanagedType;

[StructLayout (LayoutKind.Explicit, Size = 8)]
public struct SmallRect
{
    public SmallRect (short cols, short rows) : this (0, 0, cols, rows) { }

    public SmallRect (short left, short top, short right, short bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    [FieldOffset (0)]
    [MarshalAs (I2)]
    public short Left;

    [FieldOffset (2)]
    [MarshalAs (I2)]
    public short Top;

    [FieldOffset (4)]
    [MarshalAs (I2)]
    public short Right;

    [FieldOffset (6)]
    [MarshalAs (I2)]
    public short Bottom;

    public static void MakeEmpty (ref SmallRect rect) { rect.Left = -1; }

    public readonly override string ToString () => $"Left={Left},Top={Top},Right={Right},Bottom={Bottom}";

    public static void Update (ref SmallRect rect, short col, short row)
    {
        if (rect.Left == -1)
        {
            rect.Left = rect.Right = col;
            rect.Bottom = rect.Top = row;

            return;
        }

        if (col >= rect.Left && col <= rect.Right && row >= rect.Top && row <= rect.Bottom)
        {
            return;
        }

        if (col < rect.Left)
        {
            rect.Left = col;
        }

        if (col > rect.Right)
        {
            rect.Right = col;
        }

        if (row < rect.Top)
        {
            rect.Top = row;
        }

        if (row > rect.Bottom)
        {
            rect.Bottom = row;
        }
    }
}
