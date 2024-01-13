using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;
public class AdornmentTests {
	readonly ITestOutputHelper _output;

	public AdornmentTests (ITestOutputHelper output)
	{
		_output = output;
	}


	[Fact]
	public void GetAdornmentsThickness ()
	{
		var view = new View ();
		Assert.Equal (Thickness.Empty, view.GetAdornmentsThickness ());

		view.Margin.Thickness = new Thickness (1);
		Assert.Equal (new Thickness (1), view.GetAdornmentsThickness ());

		view.Border.Thickness = new Thickness (1);
		Assert.Equal (new Thickness (2), view.GetAdornmentsThickness ());

		view.Padding.Thickness = new Thickness (1);
		Assert.Equal (new Thickness (3), view.GetAdornmentsThickness ());

		view.Padding.Thickness = new Thickness (2);
		Assert.Equal (new Thickness (4), view.GetAdornmentsThickness ());

		view.Padding.Thickness = new Thickness (1, 2, 3, 4);
		Assert.Equal (new Thickness (3, 4, 5, 6), view.GetAdornmentsThickness ());

		view.Margin.Thickness = new Thickness (1, 2, 3, 4);
		Assert.Equal (new Thickness (3, 5, 7, 9), view.GetAdornmentsThickness ());
		view.Dispose ();
	}
}
