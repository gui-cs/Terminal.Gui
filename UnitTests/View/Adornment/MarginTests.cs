using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;
public class MarginTests {
	readonly ITestOutputHelper _output;

	public MarginTests (ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact, SetupFakeDriver]
	public void Margin_Uses_SuperView_ColorScheme ()
	{
		((FakeDriver)Application.Driver).SetBufferSize (5, 5);
		var view = new View () {
			Height = 3,
			Width = 3
		};
		view.Margin.Thickness = new Thickness (1);

		var superView = new View ();
		
		superView.ColorScheme = new ColorScheme () {
			Normal = new Attribute (Color.Red, Color.Green),
			Focus = new Attribute (Color.Green, Color.Red),
		};

		superView.Add (view);
		Assert.Equal (ColorName.Red, view.Margin.GetNormalColor ().Foreground.ColorName);
		Assert.Equal (ColorName.Red, superView.GetNormalColor ().Foreground.ColorName);
		Assert.Equal (superView.GetNormalColor (), view.Margin.GetNormalColor ());
		Assert.Equal (superView.GetFocusColor (), view.Margin.GetFocusColor ());

		superView.BeginInit ();
		superView.EndInit ();
		ConsoleDriver.Diagnostics = ConsoleDriver.DiagnosticFlags.FramePadding;
		view.Draw ();
		ConsoleDriver.Diagnostics = ConsoleDriver.DiagnosticFlags.Off;

		TestHelpers.AssertDriverContentsAre (@"
LTR
L R
BBB", _output);
		TestHelpers.AssertDriverAttributesAre ("0", null, superView.GetNormalColor ());
	}
}
