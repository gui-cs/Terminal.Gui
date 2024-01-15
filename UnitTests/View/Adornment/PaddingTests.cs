using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;
public class PaddingTests {
	readonly ITestOutputHelper _output;

	public PaddingTests (ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact, SetupFakeDriver]
	public void Padding_Uses_Parent_ColorScheme ()
	{
		((FakeDriver)Application.Driver).SetBufferSize (5, 5);
		var view = new View () {
			Height = 3,
			Width = 3
		};
		view.Padding.Thickness = new Thickness (1);

		view.ColorScheme = new ColorScheme () {
			Normal = new Attribute (Color.Red, Color.Green),
			Focus = new Attribute (Color.Green, Color.Red),
		};
		
		Assert.Equal (ColorName.Red, view.Padding.GetNormalColor ().Foreground.ColorName);
		Assert.Equal (view.GetNormalColor (), view.Padding.GetNormalColor ());

		view.BeginInit ();
		view.EndInit ();
		ConsoleDriver.Diagnostics = ConsoleDriver.DiagnosticFlags.FramePadding;
		view.Draw ();
		ConsoleDriver.Diagnostics = ConsoleDriver.DiagnosticFlags.Off;

		TestHelpers.AssertDriverContentsAre (@"
LTR
L R
BBB", _output);
		TestHelpers.AssertDriverAttributesAre ("0", null, view.GetNormalColor ());
	}
}
