using JetBrains.Annotations;

namespace Terminal.Gui.ViewsTests;

[TestSubject (typeof (Bar))]
public class BarTests
{
    [Fact]
    public void Constructor_Defaults ()
    {
        var bar = new Bar ();

        Assert.NotNull (bar);
        Assert.True (bar.CanFocus);
        Assert.IsType<DimAuto> (bar.Width);
        Assert.IsType<DimAuto> (bar.Height);

        // TOOD: more
    }

    [Fact]
    public void Constructor_InitializesEmpty_WhenNoShortcutsProvided ()
    {
        var bar = new Bar ();
        Assert.Empty (bar.SubViews);
    }

    [Fact]
    public void Constructor_InitializesWithShortcuts_WhenProvided ()
    {
        var shortcuts = new List<Shortcut>
        {
            new Shortcut(Key.Empty, "Command1", null, null),
            new Shortcut(Key.Empty, "Command2", null, null)
        };

        var bar = new Bar (shortcuts);

        Assert.Equal (shortcuts.Count, bar.SubViews.Count);
        for (int i = 0; i < shortcuts.Count; i++)
        {
            Assert.Same (shortcuts [i], bar.SubViews.ElementAt (i));
        }
    }

    [Fact]
    public void OrientationProperty_SetsCorrectly ()
    {
        var bar = new Bar ();
        Assert.Equal (Orientation.Horizontal, bar.Orientation); // Default value

        bar.Orientation = Orientation.Vertical;
        Assert.Equal (Orientation.Vertical, bar.Orientation);
    }

    [Fact]
    public void AlignmentModesProperty_SetsCorrectly ()
    {
        var bar = new Bar ();
        Assert.Equal (AlignmentModes.StartToEnd, bar.AlignmentModes); // Default value

        bar.AlignmentModes = AlignmentModes.EndToStart;
        Assert.Equal (AlignmentModes.EndToStart, bar.AlignmentModes);
    }

    [Fact]
    public void AddShortcutAt_InsertsShortcutCorrectly ()
    {
        var bar = new Bar ();
        var shortcut = new Shortcut (Key.Empty, "Command", null, null);
        bar.AddShortcutAt (0, shortcut);

        Assert.Contains (shortcut, bar.SubViews);
    }

    [Fact]
    public void RemoveShortcut_RemovesShortcutCorrectly ()
    {
        var shortcut1 = new Shortcut (Key.Empty, "Command1", null, null);
        var shortcut2 = new Shortcut (Key.Empty, "Command2", null, null);
        var bar = new Bar (new List<Shortcut> { shortcut1, shortcut2 });

        var removedShortcut = bar.RemoveShortcut (0);

        Assert.Same (shortcut1, removedShortcut);
        Assert.DoesNotContain (shortcut1, bar.SubViews);
        Assert.Contains (shortcut2, bar.SubViews);
    }

    [Fact]
    public void Layout_ChangesBasedOnOrientation ()
    {
        var shortcut1 = new Shortcut (Key.Empty, "Command1", null, null);
        var shortcut2 = new Shortcut (Key.Empty, "Command2", null, null);
        var bar = new Bar (new List<Shortcut> { shortcut1, shortcut2 });

        bar.Orientation = Orientation.Horizontal;
        bar.LayoutSubViews ();
        // TODO: Assert specific layout expectations for horizontal orientation

        bar.Orientation = Orientation.Vertical;
        bar.LayoutSubViews ();
        // TODO: Assert specific layout expectations for vertical orientation
    }
}
