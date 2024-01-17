using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;
public class RadioGroupTests {
	readonly ITestOutputHelper _output;

	public RadioGroupTests (ITestOutputHelper output)
	{
		this._output = output;
	}

	[Fact]
	public void Constructors_Defaults ()
	{
		var rg = new RadioGroup ();
		Assert.True (rg.CanFocus);
		Assert.Empty (rg.RadioLabels);
		Assert.Equal (Rect.Empty, rg.Frame);
		Assert.Equal (0, rg.SelectedItem);

		rg = new RadioGroup (new string [] { "Test" });
		Assert.True (rg.CanFocus);
		Assert.Single (rg.RadioLabels);
		Assert.Equal (new Rect (0, 0, 0, 0), rg.Frame);
		Assert.Equal (0, rg.SelectedItem);

		rg = new RadioGroup (new string [] { "Test" }) {
			X = 1,
			Y = 2,
			Width = 20,
			Height = 5,
		};
		Assert.True (rg.CanFocus);
		Assert.Single (rg.RadioLabels);
		Assert.Equal (new Rect (1, 2, 20, 5), rg.Frame);
		Assert.Equal (0, rg.SelectedItem);

		rg = new RadioGroup (new string [] { "Test" }) {
			X = 1,
			Y = 2,
		};

		var view = new View () {
			Width = 30,
			Height = 40,
		};
		view.Add (rg);
		view.BeginInit ();
		view.EndInit ();
		view.LayoutSubviews ();

		Assert.True (rg.CanFocus);
		Assert.Single (rg.RadioLabels);
		Assert.Equal (new Rect (1, 2, 6, 1), rg.Frame);
		Assert.Equal (0, rg.SelectedItem);
	}

	[Fact]
	public void Initialize_SelectedItem_With_Minus_One ()
	{
		var rg = new RadioGroup (new string [] { "Test" }, -1);
		Assert.Equal (-1, rg.SelectedItem);
		Assert.True (rg.NewKeyDownEvent (new (KeyCode.Space)));
		Assert.Equal (0, rg.SelectedItem);
	}

	[Fact, AutoInitShutdown]
	public void Orientation_Width_Height_Vertical_Horizontal_Space ()
	{
		var rg = new RadioGroup (new string [] { "Test", "New Test 你" });
		var win = new Window () {
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		win.Add (rg);
		Application.Top.Add (win);

		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (30, 5);

		Assert.Equal (Orientation.Vertical, rg.Orientation);
		Assert.Equal (2, rg.RadioLabels.Length);
		Assert.Equal (0, rg.X);
		Assert.Equal (0, rg.Y);
		Assert.Equal (13, rg.Frame.Width);
		Assert.Equal (2, rg.Frame.Height);
		var expected = @$"
┌────────────────────────────┐
│{CM.Glyphs.Selected} Test                      │
│{CM.Glyphs.UnSelected} New Test 你               │
│                            │
└────────────────────────────┘
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 30, 5), pos);

		rg.Orientation = Orientation.Horizontal;
		Application.Refresh ();

		Assert.Equal (Orientation.Horizontal, rg.Orientation);
		Assert.Equal (2, rg.HorizontalSpace);
		Assert.Equal (0, rg.X);
		Assert.Equal (0, rg.Y);
		Assert.Equal (21, rg.Width);
		Assert.Equal (1, rg.Height);

		expected = @$"
┌────────────────────────────┐
│{CM.Glyphs.Selected} Test  {CM.Glyphs.UnSelected} New Test 你       │
│                            │
│                            │
└────────────────────────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 30, 5), pos);

		rg.HorizontalSpace = 4;
		Application.Refresh ();

		Assert.Equal (Orientation.Horizontal, rg.Orientation);
		Assert.Equal (4, rg.HorizontalSpace);
		Assert.Equal (0, rg.X);
		Assert.Equal (0, rg.Y);
		Assert.Equal (23, rg.Width);
		Assert.Equal (1, rg.Height);
		expected = @$"
┌────────────────────────────┐
│{CM.Glyphs.Selected} Test    {CM.Glyphs.UnSelected} New Test 你     │
│                            │
│                            │
└────────────────────────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 30, 5), pos);
	}

	[Fact]
	public void SelectedItemChanged_Event ()
	{
		var previousSelectedItem = -1;
		var selectedItem = -1;
		var rg = new RadioGroup (new string [] { "Test", "New Test" });
		rg.SelectedItemChanged += (s, e) => {
			previousSelectedItem = e.PreviousSelectedItem;
			selectedItem = e.SelectedItem;
		};

		rg.SelectedItem = 1;
		Assert.Equal (0, previousSelectedItem);
		Assert.Equal (selectedItem, rg.SelectedItem);
	}

	[Fact]
	public void KeyBindings_Command ()
	{
		var rg = new RadioGroup (new string [] { "Test", "New Test" });

		Assert.True (rg.NewKeyDownEvent (new (KeyCode.CursorUp)));
		Assert.True (rg.NewKeyDownEvent (new (KeyCode.CursorDown)));
		Assert.True (rg.NewKeyDownEvent (new (KeyCode.Home)));
		Assert.True (rg.NewKeyDownEvent (new (KeyCode.End)));
		Assert.True (rg.NewKeyDownEvent (new (KeyCode.Space)));
		Assert.Equal (1, rg.SelectedItem);
	}

	[Fact]
	public void KeyBindings_Are_Added_Correctly ()
	{
		var rg = new RadioGroup (new string [] { "_Left", "_Right" });
		Assert.NotEmpty (rg.KeyBindings.GetCommands (KeyCode.L));
		Assert.NotEmpty (rg.KeyBindings.GetCommands (KeyCode.R));

		Assert.NotEmpty (rg.KeyBindings.GetCommands (KeyCode.L | KeyCode.ShiftMask));
		Assert.NotEmpty (rg.KeyBindings.GetCommands (KeyCode.L | KeyCode.AltMask));

		Assert.NotEmpty (rg.KeyBindings.GetCommands (KeyCode.R | KeyCode.ShiftMask));
		Assert.NotEmpty (rg.KeyBindings.GetCommands (KeyCode.R | KeyCode.AltMask));

	}

	[Fact]
	public void KeyBindings_HotKeys ()
	{
		var rg = new RadioGroup (new string [] { "_Left", "_Right", "Cen_tered", "_Justified" });
		Assert.NotEmpty (rg.KeyBindings.GetCommands (KeyCode.L));
		Assert.NotEmpty (rg.KeyBindings.GetCommands (KeyCode.L | KeyCode.ShiftMask));
		Assert.NotEmpty (rg.KeyBindings.GetCommands (KeyCode.L | KeyCode.AltMask));

		// BUGBUG: These tests only test that RG works on it's own, not if it's a subview
		Assert.True (rg.NewKeyDownEvent (new (KeyCode.T)));
		Assert.Equal (2, rg.SelectedItem);
		Assert.True (rg.NewKeyDownEvent (new (KeyCode.L)));
		Assert.Equal (0, rg.SelectedItem);
		Assert.True (rg.NewKeyDownEvent (new (KeyCode.J)));
		Assert.Equal (3, rg.SelectedItem);
		Assert.True (rg.NewKeyDownEvent (new (KeyCode.R)));
		Assert.Equal (1, rg.SelectedItem);

		Assert.True (rg.NewKeyDownEvent (new (KeyCode.T | KeyCode.AltMask)));
		Assert.Equal (2, rg.SelectedItem);
		Assert.True (rg.NewKeyDownEvent (new (KeyCode.L | KeyCode.AltMask)));
		Assert.Equal (0, rg.SelectedItem);
		Assert.True (rg.NewKeyDownEvent (new (KeyCode.J | KeyCode.AltMask)));
		Assert.Equal (3, rg.SelectedItem);
		Assert.True (rg.NewKeyDownEvent (new (KeyCode.R | KeyCode.AltMask)));
		Assert.Equal (1, rg.SelectedItem);

		var superView = new View ();
		superView.Add (rg);
		Assert.True (superView.NewKeyDownEvent (new (KeyCode.T)));
		Assert.Equal (2, rg.SelectedItem);
		Assert.True (superView.NewKeyDownEvent (new (KeyCode.L)));
		Assert.Equal (0, rg.SelectedItem);
		Assert.True (superView.NewKeyDownEvent (new (KeyCode.J)));
		Assert.Equal (3, rg.SelectedItem);
		Assert.True (superView.NewKeyDownEvent (new (KeyCode.R)));
		Assert.Equal (1, rg.SelectedItem);

		Assert.True (superView.NewKeyDownEvent (new (KeyCode.T | KeyCode.AltMask)));
		Assert.Equal (2, rg.SelectedItem);
		Assert.True (superView.NewKeyDownEvent (new (KeyCode.L | KeyCode.AltMask)));
		Assert.Equal (0, rg.SelectedItem);
		Assert.True (superView.NewKeyDownEvent (new (KeyCode.J | KeyCode.AltMask)));
		Assert.Equal (3, rg.SelectedItem);
		Assert.True (superView.NewKeyDownEvent (new (KeyCode.R | KeyCode.AltMask)));
		Assert.Equal (1, rg.SelectedItem);

	}
}
