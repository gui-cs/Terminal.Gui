using Xunit;

namespace Terminal.Gui.ViewsTests;
public class TabTests {

	[Fact]
	public void Constructor_Defaults ()
	{
		Tab tab = new Tab ();
		Assert.Equal ("Unamed", tab.DisplayText);
		Assert.Null (tab.View);
		Assert.Equal (LineStyle.Rounded, tab.BorderStyle);
		Assert.True (tab.CanFocus);
	}
}
