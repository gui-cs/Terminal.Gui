using Xunit;
using Xunit.Abstractions;
using GraphViewTests = Terminal.Gui.Views.GraphViewTests;

namespace Terminal.Gui.Core {
	public class ContextMenuTests {
		readonly ITestOutputHelper output;

		public ContextMenuTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		[AutoInitShutdown]
		public void ContextMenu_Constructors ()
		{
			var cm = new ContextMenu ();
			Assert.Equal (new Point (0, 0), cm.Position);
			Assert.Empty (cm.MenuItens.Children);
			Assert.Null (cm.Host);
			cm.Position = new Point (20, 10);
			cm.MenuItens = new MenuBarItem (new MenuItem [] {
				new MenuItem ("First", "", null)
			});
			Assert.Equal (new Point (20, 10), cm.Position);
			Assert.Single (cm.MenuItens.Children);

			cm = new ContextMenu (5, 10,
				new MenuBarItem (new MenuItem [] {
					new MenuItem ("One", "", null),
					new MenuItem ("Two", "", null)
				})
			);
			Assert.Equal (new Point (5, 10), cm.Position);
			Assert.Equal (2, cm.MenuItens.Children.Length);
			Assert.Null (cm.Host);

			cm = new ContextMenu (new View () { X = 5, Y = 10 },
				new MenuBarItem (new MenuItem [] {
					new MenuItem ("One", "", null),
					new MenuItem ("Two", "", null)
				})
			);
			Assert.Equal (new Point (6, 10), cm.Position);
			Assert.Equal (2, cm.MenuItens.Children.Length);
			Assert.NotNull (cm.Host);
		}

		[Fact]
		[AutoInitShutdown]
		public void Show_Hide_IsShow ()
		{
			var cm = new ContextMenu (10, 5,
				new MenuBarItem (new MenuItem [] {
					new MenuItem ("One", "", null),
					new MenuItem ("Two", "", null)
				})
			);

			cm.Show ();
			Assert.True (ContextMenu.IsShow);

			Application.Begin (Application.Top);

			var expected = @"
          ┌──────┐
          │ One  │
          │ Two  │
          └──────┘
";

			GraphViewTests.AssertDriverContentsAre (expected, output);

			cm.Hide ();
			Assert.False (ContextMenu.IsShow);

			Application.Refresh ();

			expected = "";

			GraphViewTests.AssertDriverContentsAre (expected, output);
		}

		[Fact]
		[AutoInitShutdown]
		public void Position_Changing ()
		{
			var cm = new ContextMenu (10, 5,
				new MenuBarItem (new MenuItem [] {
					new MenuItem ("One", "", null),
					new MenuItem ("Two", "", null)
				})
			);

			cm.Show ();
			Application.Begin (Application.Top);

			var expected = @"
          ┌──────┐
          │ One  │
          │ Two  │
          └──────┘
";

			GraphViewTests.AssertDriverContentsAre (expected, output);

			cm.Position = new Point (5, 10);

			cm.Show ();
			Application.Refresh ();

			expected = @"
     ┌──────┐
     │ One  │
     │ Two  │
     └──────┘
";

			GraphViewTests.AssertDriverContentsAre (expected, output);

		}

		[Fact]
		[AutoInitShutdown]
		public void MenuItens_Changing ()
		{
			var cm = new ContextMenu (10, 5,
				new MenuBarItem (new MenuItem [] {
					new MenuItem ("One", "", null),
					new MenuItem ("Two", "", null)
				})
			);

			cm.Show ();
			Application.Begin (Application.Top);

			var expected = @"
          ┌──────┐
          │ One  │
          │ Two  │
          └──────┘
";

			GraphViewTests.AssertDriverContentsAre (expected, output);

			cm.MenuItens = new MenuBarItem (new MenuItem [] {
				new MenuItem ("First", "", null),
				new MenuItem ("Second", "", null),
				new MenuItem ("Third", "", null)
			});


			cm.Show ();
			Application.Refresh ();

			expected = @"
          ┌─────────┐
          │ First   │
          │ Second  │
          │ Third   │
          └─────────┘
";

			GraphViewTests.AssertDriverContentsAre (expected, output);

		}

		[Fact, AutoInitShutdown]
		public void Key_Changing ()
		{
			var lbl = new Label ("Original");

			var cm = new ContextMenu ();

			lbl.KeyPress += (e) => {
				if (e.KeyEvent.Key == cm.Key) {
					lbl.Text = "Replaced";
					e.Handled = true;
				}
			};

			var top = Application.Top;
			top.Add (lbl);
			Application.Begin (top);

			Assert.True (lbl.ProcessKey (new KeyEvent (cm.Key, new KeyModifiers ())));
			Assert.Equal ("Replaced", lbl.Text);

			lbl.Text = "Original";
			cm.Key = Key.Space | Key.CtrlMask;
			Assert.True (lbl.ProcessKey (new KeyEvent (cm.Key, new KeyModifiers ())));
			Assert.Equal ("Replaced", lbl.Text);
		}

		[Fact, AutoInitShutdown]
		public void MouseFlags_Changing ()
		{
			var lbl = new Label ("Original");

			var cm = new ContextMenu ();

			lbl.MouseClick += (e) => {
				if (e.MouseEvent.Flags == cm.MouseFlags) {
					lbl.Text = "Replaced";
					e.Handled = true;
				}
			};

			var top = Application.Top;
			top.Add (lbl);
			Application.Begin (top);

			Assert.True (lbl.OnMouseEvent (new MouseEvent () { Flags = cm.MouseFlags }));
			Assert.Equal ("Replaced", lbl.Text);

			lbl.Text = "Original";
			cm.MouseFlags = MouseFlags.Button2Clicked;
			Assert.True (lbl.OnMouseEvent (new MouseEvent () { Flags = cm.MouseFlags }));
			Assert.Equal ("Replaced", lbl.Text);
		}

		[Fact, AutoInitShutdown]
		public void KeyChanged_Event ()
		{
			var oldKey = Key.Null;
			var cm = new ContextMenu ();

			cm.KeyChanged += (e) => oldKey = e;

			cm.Key = Key.Space | Key.CtrlMask;
			Assert.Equal (Key.Space | Key.CtrlMask, cm.Key);
			Assert.Equal (Key.F10 | Key.ShiftMask, oldKey);
		}

		[Fact, AutoInitShutdown]
		public void MouseFlagsChanged_Event ()
		{
			var oldMouseFlags = new MouseFlags ();
			var cm = new ContextMenu ();

			cm.MouseFlagsChanged += (e) => oldMouseFlags = e;

			cm.MouseFlags = MouseFlags.Button2Clicked;
			Assert.Equal (MouseFlags.Button2Clicked, cm.MouseFlags);
			Assert.Equal (MouseFlags.Button3Clicked, oldMouseFlags);
		}

		[Fact, AutoInitShutdown]
		public void Show_Ensures_Display_Inside_The_Container_But_Preserves_Position ()
		{
			var cm = new ContextMenu (80, 25,
				new MenuBarItem (new MenuItem [] {
					new MenuItem ("One", "", null),
					new MenuItem ("Two", "", null)
				})
			);

			Assert.Equal (new Point (80, 25), cm.Position);

			cm.Show ();
			Assert.Equal (new Point (80, 25), cm.Position);
			Application.Begin (Application.Top);

			var expected = @"
                                                                        ┌──────┐
                                                                        │ One  │
                                                                        │ Two  │
                                                                        └──────┘
";

			var pos = GraphViewTests.AssertDriverContentsWithPosAre (expected, output);
			Assert.Equal (new Point (72, 21), pos);

			cm.Hide ();
			Assert.Equal (new Point (80, 25), cm.Position);
		}

		[Fact, AutoInitShutdown]
		public void Show_Ensures_Display_Inside_The_Container_Without_Overlap_The_Host ()
		{
			var cm = new ContextMenu (new View () { X = 69, Y = 24, Width = 10, Height = 1 },
				new MenuBarItem (new MenuItem [] {
					new MenuItem ("One", "", null),
					new MenuItem ("Two", "", null)
				})
			);

			Assert.Equal (new Point (70, 25), cm.Position);

			cm.Show ();
			Assert.Equal (new Point (70, 25), cm.Position);
			Application.Begin (Application.Top);

			var expected = @"
                                                                      ┌──────┐
                                                                      │ One  │
                                                                      │ Two  │
                                                                      └──────┘
";

			var pos = GraphViewTests.AssertDriverContentsWithPosAre (expected, output);
			Assert.Equal (new Point (70, 21), pos);

			cm.Hide ();
			Assert.Equal (new Point (70, 25), cm.Position);
		}

		[Fact, AutoInitShutdown]
		public void Show_Display_Below_The_Bottom_Host_If_Has_Enough_Space ()
		{
			var cm = new ContextMenu (new View () { X = 10, Y = 5, Width = 10, Height = 1 },
				new MenuBarItem (new MenuItem [] {
					new MenuItem ("One", "", null),
					new MenuItem ("Two", "", null)
				})
			);

			Assert.Equal (new Point (11, 6), cm.Position);

			cm.Host.X = 5;
			cm.Host.Y = 10;

			cm.Show ();
			Assert.Equal (new Point (6, 11), cm.Position);
			Application.Begin (Application.Top);

			var expected = @"
      ┌──────┐
      │ One  │
      │ Two  │
      └──────┘
";

			var pos = GraphViewTests.AssertDriverContentsWithPosAre (expected, output);
			Assert.Equal (new Point (6, 12), pos);

			cm.Hide ();
			Assert.Equal (new Point (6, 11), cm.Position);
		}

		[Fact, AutoInitShutdown]
		public void Show_Display_At_Zero_If_The_Toplevel_Width_Is_Less_Than_The_Menu_Width ()
		{
			var cm = new ContextMenu (0, 0,
				new MenuBarItem (new MenuItem [] {
					new MenuItem ("One", "", null),
					new MenuItem ("Two", "", null)
				})
			);

			Assert.Equal (new Point (0, 0), cm.Position);

			cm.Show ();
			Assert.Equal (new Point (0, 0), cm.Position);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (5, 25);

			var expected = @"
┌────
│ One
│ Two
└────
";

			var pos = GraphViewTests.AssertDriverContentsWithPosAre (expected, output);
			Assert.Equal (new Point (0, 1), pos);

			cm.Hide ();
			Assert.Equal (new Point (0, 0), cm.Position);
		}

		[Fact, AutoInitShutdown]
		public void Show_Display_At_Zero_If_The_Toplevel_Height_Is_Less_Than_The_Menu_Height ()
		{
			var cm = new ContextMenu (0, 0,
				new MenuBarItem (new MenuItem [] {
					new MenuItem ("One", "", null),
					new MenuItem ("Two", "", null)
				})
			);

			Assert.Equal (new Point (0, 0), cm.Position);

			cm.Show ();
			Assert.Equal (new Point (0, 0), cm.Position);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (80, 4);

			var expected = @"
┌──────┐
│ One  │
│ Two  │
";

			var pos = GraphViewTests.AssertDriverContentsWithPosAre (expected, output);
			Assert.Equal (new Point (0, 1), pos);

			cm.Hide ();
			Assert.Equal (new Point (0, 0), cm.Position);
		}

		[Fact, AutoInitShutdown]
		public void Hide_Is_Invoke_At_Container_Closing ()
		{
			var cm = new ContextMenu (80, 25,
				new MenuBarItem (new MenuItem [] {
					new MenuItem ("One", "", null),
					new MenuItem ("Two", "", null)
				})
			);

			var top = Application.Top;
			Application.Begin (top);
			top.Running = true;

			Assert.False (ContextMenu.IsShow);

			cm.Show ();
			Assert.True (ContextMenu.IsShow);

			top.RequestStop ();
			Assert.False (ContextMenu.IsShow);
		}
	}
}
