using UnitTests;

namespace ViewsTests.TabView;

// Copilot

/// <summary>
///     Tests for the <see cref="Tab"/> class.
/// </summary>
public class TabTests : TestDriverBase
{
    [Fact]
    public void Constructor_SetsExpectedDefaults ()
    {
        Tab tab = new ();

        Assert.True (tab.CanFocus);
        Assert.True (tab.SuperViewRendersLineCanvas);
        Assert.Equal (BorderSettings.Tab | BorderSettings.Title, tab.Border.Settings);
        Assert.Equal (ViewArrangement.Overlapped, tab.Arrangement);
        Assert.Equal (0, tab.TabIndex);
    }

    [Fact]
    public void TabIndex_CanBeSetInternally ()
    {
        Tab tab = new () { Title = "Test" };
        Assert.Equal (0, tab.TabIndex);

        // TabIndex is internal set, so we test through Tabs container
        Tabs tabs = new ();
        Tab tab1 = new () { Title = "Tab1" };
        Tab tab2 = new () { Title = "Tab2" };
        tabs.Add (tab1, tab2);

        Assert.Equal (0, tab1.TabIndex);
        Assert.Equal (1, tab2.TabIndex);
    }

    [Fact]
    public void Title_SetsTitle ()
    {
        Tab tab = new () { Title = "_MyTab" };
        Assert.Equal ("_MyTab", tab.Title);
    }
}
