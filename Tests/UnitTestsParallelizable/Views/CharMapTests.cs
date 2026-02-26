using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="CharMap"/> command handling and IValue implementation.
/// </summary>
public class CharMapTests
{
    /// <summary>
    ///     Verifies that <see cref="CharMap.ValueChangedUntyped"/> is raised when <see cref="CharMap.SelectedCodePoint"/> changes.
    /// </summary>
    /// <remarks>
    ///     This test is expected to fail until CharMap is updated to invoke ValueChangedUntyped in the SelectedCodePoint setter,
    ///     following the pattern used in ListView.Selection.cs line 397.
    /// </remarks>
    [Fact]
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public void ValueChangedUntyped_Is_Raised_When_SelectedCodePoint_Changes ()
    {
        CharMap charMap = new () { SelectedCodePoint = 0x41 }; // 'A'
        var valueChangedUntypedCount = 0;
        object? oldValue = null;
        object? newValue = null;

        charMap.ValueChangedUntyped += (_, e) =>
                                       {
                                           valueChangedUntypedCount++;
                                           oldValue = e.OldValue;
                                           newValue = e.NewValue;
                                       };

        // Change the selected code point
        charMap.SelectedCodePoint = 0x42; // 'B'

        Assert.Equal (1, valueChangedUntypedCount);
        Assert.Equal (new Rune (0x41), oldValue);
        Assert.Equal (new Rune (0x42), newValue);

        charMap.Dispose ();
    }

    /// <summary>
    ///     Verifies that <see cref="CharMap.ValueChanged"/> and <see cref="CharMap.ValueChangedUntyped"/> are raised together.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public void ValueChanged_And_ValueChangedUntyped_Are_Both_Raised ()
    {
        CharMap charMap = new () { SelectedCodePoint = 0x30 }; // '0'
        var valueChangedCount = 0;
        var valueChangedUntypedCount = 0;

        charMap.ValueChanged += (_, _) => valueChangedCount++;
        charMap.ValueChangedUntyped += (_, _) => valueChangedUntypedCount++;

        // Change the selected code point
        charMap.SelectedCodePoint = 0x31; // '1'

        Assert.Equal (1, valueChangedCount);
        Assert.Equal (1, valueChangedUntypedCount);

        charMap.Dispose ();
    }

    /// <summary>
    ///     Verifies that <see cref="CharMap.ValueChangedUntyped"/> is not raised when value doesn't actually change.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public void ValueChangedUntyped_Is_Not_Raised_When_Value_Same ()
    {
        CharMap charMap = new () { SelectedCodePoint = 0x41 }; // 'A'
        var valueChangedUntypedCount = 0;

        charMap.ValueChangedUntyped += (_, _) => valueChangedUntypedCount++;

        // Set to same value
        charMap.SelectedCodePoint = 0x41; // 'A' again

        Assert.Equal (0, valueChangedUntypedCount);

        charMap.Dispose ();
    }

    /// <summary>
    ///     Verifies that multiple changes to SelectedCodePoint raise ValueChangedUntyped each time.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public void ValueChangedUntyped_Is_Raised_For_Each_Change ()
    {
        CharMap charMap = new () { SelectedCodePoint = 0x41 }; // 'A'
        var valueChangedUntypedCount = 0;

        charMap.ValueChangedUntyped += (_, _) => valueChangedUntypedCount++;

        charMap.SelectedCodePoint = 0x42; // 'B'
        charMap.SelectedCodePoint = 0x43; // 'C'
        charMap.SelectedCodePoint = 0x44; // 'D'

        Assert.Equal (3, valueChangedUntypedCount);

        charMap.Dispose ();
    }

    /// <summary>
    ///     Verifies that setting the <see cref="CharMap.Value"/> property (IValue&lt;Rune&gt;) also raises ValueChangedUntyped.
    /// </summary>
    [Fact]
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public void Value_Property_Change_Raises_ValueChangedUntyped ()
    {
        CharMap charMap = new () { Value = new Rune (0x41) }; // 'A'
        var valueChangedUntypedCount = 0;

        charMap.ValueChangedUntyped += (_, _) => valueChangedUntypedCount++;

        // Change via Value property
        charMap.Value = new Rune (0x42); // 'B'

        Assert.Equal (1, valueChangedUntypedCount);

        charMap.Dispose ();
    }
}
