using Xunit.Abstractions;

namespace ViewsTests;

/// <summary>
///     Unit tests for <see cref="Dialog"/> that modify static properties and cannot run in parallel.
/// </summary>
public class DialogTests (ITestOutputHelper output)
{
    [Fact]
    public void DefaultBorderStyle_Get_Set ()
    {
        LineStyle original = Dialog.DefaultBorderStyle;

        try
        {
            Dialog.DefaultBorderStyle = LineStyle.Single;
            Assert.Equal (LineStyle.Single, Dialog.DefaultBorderStyle);

            Dialog dialog = new ();
            Assert.Equal (LineStyle.Single, dialog.BorderStyle);
            dialog.Dispose ();

            Dialog.DefaultBorderStyle = LineStyle.Double;
            Assert.Equal (LineStyle.Double, Dialog.DefaultBorderStyle);

            dialog = new ();
            Assert.Equal (LineStyle.Double, dialog.BorderStyle);
            dialog.Dispose ();
        }
        finally
        {
            Dialog.DefaultBorderStyle = original;
        }
    }

    [Fact]
    public void DefaultButtonAlignment_Get_Set ()
    {
        Alignment original = Dialog.DefaultButtonAlignment;

        try
        {
            Dialog.DefaultButtonAlignment = Alignment.Start;
            Assert.Equal (Alignment.Start, Dialog.DefaultButtonAlignment);

            Dialog dialog = new ();
            Assert.Equal (Alignment.Start, dialog.ButtonAlignment);
            dialog.Dispose ();

            Dialog.DefaultButtonAlignment = Alignment.Center;
            Assert.Equal (Alignment.Center, Dialog.DefaultButtonAlignment);

            dialog = new ();
            Assert.Equal (Alignment.Center, dialog.ButtonAlignment);
            dialog.Dispose ();
        }
        finally
        {
            Dialog.DefaultButtonAlignment = original;
        }
    }

    [Fact]
    public void DefaultButtonAlignmentModes_Get_Set ()
    {
        AlignmentModes original = Dialog.DefaultButtonAlignmentModes;

        try
        {
            Dialog.DefaultButtonAlignmentModes = AlignmentModes.StartToEnd;
            Assert.Equal (AlignmentModes.StartToEnd, Dialog.DefaultButtonAlignmentModes);

            Dialog dialog = new ();
            Assert.Equal (AlignmentModes.StartToEnd, dialog.ButtonAlignmentModes);
            dialog.Dispose ();

            Dialog.DefaultButtonAlignmentModes = AlignmentModes.IgnoreFirstOrLast;
            Assert.Equal (AlignmentModes.IgnoreFirstOrLast, Dialog.DefaultButtonAlignmentModes);

            dialog = new ();
            Assert.Equal (AlignmentModes.IgnoreFirstOrLast, dialog.ButtonAlignmentModes);
            dialog.Dispose ();
        }
        finally
        {
            Dialog.DefaultButtonAlignmentModes = original;
        }
    }

    [Fact]
    public void DefaultShadow_Get_Set ()
    {
        ShadowStyle original = Dialog.DefaultShadow;

        try
        {
            Dialog.DefaultShadow = ShadowStyle.None;
            Assert.Equal (ShadowStyle.None, Dialog.DefaultShadow);

            Dialog dialog = new ();
            Assert.Equal (ShadowStyle.None, dialog.ShadowStyle);
            dialog.Dispose ();

            Dialog.DefaultShadow = ShadowStyle.Opaque;
            Assert.Equal (ShadowStyle.Opaque, Dialog.DefaultShadow);

            dialog = new ();
            Assert.Equal (ShadowStyle.Opaque, dialog.ShadowStyle);
            dialog.Dispose ();
        }
        finally
        {
            Dialog.DefaultShadow = original;
        }
    }
}
