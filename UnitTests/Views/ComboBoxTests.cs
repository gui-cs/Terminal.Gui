using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests {
	public class ComboBoxTests {
		ITestOutputHelper output;

		public ComboBoxTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact, AutoInitShutdown]
		public void Constructors_Defaults ()
		{
			var cb = new ComboBox ();
			cb.BeginInit ();
			cb.EndInit ();
			cb.LayoutSubviews ();
			Assert.Equal (string.Empty, cb.Text);
			Assert.Null (cb.Source);
			Assert.False (cb.AutoSize);
			Assert.Equal (new Rect (0, 0, 0, 2), cb.Frame);
			Assert.Equal (-1, cb.SelectedItem);

			cb = new ComboBox ("Test");
			cb.BeginInit ();
			cb.EndInit ();
			cb.LayoutSubviews ();
			Assert.Equal ("Test", cb.Text);
			Assert.Null (cb.Source);
			Assert.False (cb.AutoSize);
			Assert.Equal (new Rect (0, 0, 0, 2), cb.Frame);
			Assert.Equal (-1, cb.SelectedItem);

			cb = new ComboBox (new Rect (1, 2, 10, 20), new List<string> () { "One", "Two", "Three" });
			cb.BeginInit ();
			cb.EndInit ();
			cb.LayoutSubviews ();
			Assert.Equal (string.Empty, cb.Text);
			Assert.NotNull (cb.Source);
			Assert.False (cb.AutoSize);
			Assert.Equal (new Rect (1, 2, 10, 20), cb.Frame);
			Assert.Equal (-1, cb.SelectedItem);

			cb = new ComboBox (new List<string> () { "One", "Two", "Three" });
			cb.BeginInit ();
			cb.EndInit ();
			cb.LayoutSubviews ();
			Assert.Equal (string.Empty, cb.Text);
			Assert.NotNull (cb.Source);
			Assert.False (cb.AutoSize);
			Assert.Equal (new Rect (0, 0, 0, 2), cb.Frame);
			Assert.Equal (-1, cb.SelectedItem);
		}

		[Fact]
		[AutoInitShutdown]
		public void Constructor_With_Source_Initialize_With_The_Passed_SelectedItem ()
		{
			var cb = new ComboBox (new List<string> () { "One", "Two", "Three" }) {
				SelectedItem = 1
			};
			cb.BeginInit ();
			cb.EndInit ();
			cb.LayoutSubviews ();
			Assert.Equal ("Two", cb.Text);
			Assert.NotNull (cb.Source);
			Assert.False (cb.AutoSize);
			Assert.Equal (new Rect (0, 0, 0, 2), cb.Frame);
			Assert.Equal (1, cb.SelectedItem);
		}

		[Fact]
		[AutoInitShutdown]
		public void EnsureKeyEventsDoNotCauseExceptions ()
		{
			var comboBox = new ComboBox ("0");

			var source = Enumerable.Range (0, 15).Select (x => x.ToString ()).ToArray ();
			comboBox.SetSource (source);

			Application.Top.Add (comboBox);

			foreach (var key in (KeyCode [])Enum.GetValues (typeof (KeyCode))) {
				Assert.Null (Record.Exception (() => comboBox.NewKeyDownEvent (new (key))));
			}
		}

		[Fact]
		[AutoInitShutdown]
		public void KeyBindings_Command ()
		{
			List<string> source = new List<string> () { "One", "Two", "Three" };
			ComboBox cb = new ComboBox () { Width = 10 };
			cb.SetSource (source);
			Application.Top.Add (cb);
			Application.Top.FocusFirst ();
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal (string.Empty, cb.Text);
			var opened = false;
			cb.OpenSelectedItem += (s, _) => opened = true;
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.Enter)));
			Assert.False (opened);
			cb.Text = "Tw";
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.Enter)));
			Assert.True (opened);
			Assert.Equal ("Tw", cb.Text);
			Assert.False (cb.IsShow);
			cb.SetSource (null);
			Assert.False (cb.NewKeyDownEvent (new (KeyCode.Enter)));
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.F4))); // with no source also expand empty
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			cb.SetSource (source);
			cb.Text = "";
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.F4))); // collapse
			Assert.False (cb.IsShow);
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.F4))); // expand
			Assert.True (cb.IsShow);
			cb.Collapse ();
			Assert.False (cb.IsShow);
			Assert.True (cb.HasFocus);
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.CursorDown))); // losing focus
			Assert.False (cb.IsShow);
			Assert.False (cb.HasFocus);
			Application.Top.FocusFirst (); // Gets focus again
			Assert.False (cb.IsShow);
			Assert.True (cb.HasFocus);
			cb.Expand ();
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.CursorDown)));
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.CursorDown)));
			Assert.True (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.CursorDown)));
			Assert.True (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.CursorUp)));
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.CursorUp)));
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.CursorUp)));
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Application.Begin (Application.Top);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
One      ▼
One       
", output);

			Assert.True (cb.NewKeyDownEvent (new (KeyCode.PageDown)));
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
			Application.Begin (Application.Top);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
Two      ▼
Two       
", output);

			Assert.True (cb.NewKeyDownEvent (new (KeyCode.PageDown)));
			Assert.True (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);
			Application.Begin (Application.Top);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
Three    ▼
Three     
", output);
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.PageUp)));
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.PageUp)));
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.F4)));
			Assert.False (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.End)));
			Assert.False (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.Home)));
			Assert.False (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.F4)));
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.End)));
			Assert.True (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.Home)));
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.Esc)));
			Assert.False (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.CursorDown))); // losing focus
			Assert.False (cb.HasFocus);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Application.Top.FocusFirst (); // Gets focus again
			Assert.True (cb.HasFocus);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.U | KeyCode.CtrlMask)));
			Assert.True (cb.HasFocus);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
			Assert.Equal (3, cb.Source.Count);
		}

		[Fact]
		[AutoInitShutdown]
		public void Source_Equal_Null_Or_Count_Equal_Zero_Sets_SelectedItem_Equal_To_Minus_One ()
		{
			var cb = new ComboBox ();
			Application.Top.Add (cb);
			Application.Top.FocusFirst ();
			Assert.Null (cb.Source);
			Assert.Equal (-1, cb.SelectedItem);
			var source = new List<string> ();
			cb.SetSource (source);
			Assert.NotNull (cb.Source);
			Assert.Equal (0, cb.Source.Count);
			Assert.Equal (-1, cb.SelectedItem);
			source.Add ("One");
			Assert.Equal (1, cb.Source.Count);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.F4)));
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			source.Add ("Two");
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			cb.Text = "T";
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("T", cb.Text);
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.Enter)));
			Assert.False (cb.IsShow);
			Assert.Equal (2, cb.Source.Count);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("T", cb.Text);
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.Esc)));
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem); // retains last accept selected item
			Assert.Equal ("", cb.Text); // clear text
			cb.SetSource (new List<string> ());
			Assert.Equal (0, cb.Source.Count);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
		}

		[Fact, AutoInitShutdown]
		public void HideDropdownListOnClick_Gets_Sets ()
		{
			var selected = "";
			var cb = new ComboBox {
				Height = 4,
				Width = 5
			};
			cb.SetSource (new List<string> { "One", "Two", "Three" });
			cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
			Application.Top.Add (cb);
			Application.Begin (Application.Top);

			Assert.False (cb.HideDropdownListOnClick);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);

			Assert.True (cb.Subviews [1].MouseEvent (new MouseEvent {
				X = 0,
				Y = 1,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
			Assert.True (cb.Subviews [1].MouseEvent (new MouseEvent {
				X = 0,
				Y = 1,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);

			cb.HideDropdownListOnClick = true;

			Assert.True (cb.Subviews [1].MouseEvent (new MouseEvent {
				X = 0,
				Y = 2,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("Three", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.True (cb.Subviews [1].MouseEvent (new MouseEvent {
				X = 0,
				Y = 2,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("Three", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("Three", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);
			Assert.True (cb.Subviews [1].MouseEvent (new MouseEvent {
				X = 0,
				Y = 0,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("One", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
		}

		[Fact, AutoInitShutdown]
		public void HideDropdownListOnClick_True_OpenSelectedItem_With_Mouse_And_Key_And_Mouse ()
		{
			var selected = "";
			var cb = new ComboBox {
				Height = 4,
				Width = 5,
				HideDropdownListOnClick = true
			};
			cb.SetSource (new List<string> { "One", "Two", "Three" });
			cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
			Application.Top.Add (cb);
			Application.Begin (Application.Top);

			Assert.True (cb.HideDropdownListOnClick);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.Subviews [1].NewKeyDownEvent (new (KeyCode.CursorDown)));
			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.Subviews [1].NewKeyDownEvent (new (KeyCode.CursorUp)));
			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
		}

		[Fact, AutoInitShutdown]
		public void HideDropdownListOnClick_True_OpenSelectedItem_With_Mouse_And_Key_CursorDown_And_Esc ()
		{
			var selected = "";
			var cb = new ComboBox {
				Height = 4,
				Width = 5,
				HideDropdownListOnClick = true
			};
			cb.SetSource (new List<string> { "One", "Two", "Three" });
			cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
			Application.Top.Add (cb);
			Application.Begin (Application.Top);

			Assert.True (cb.HideDropdownListOnClick);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.Subviews [1].NewKeyDownEvent (new (KeyCode.CursorDown)));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.Subviews [1].NewKeyDownEvent (new (KeyCode.Enter)));
			Assert.Equal ("Two", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);

			Assert.True (cb.NewKeyDownEvent (new (KeyCode.F4)));
			Assert.Equal ("Two", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
			Assert.True (cb.Subviews [1].NewKeyDownEvent (new (KeyCode.CursorDown)));
			Assert.Equal ("Two", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);

			Assert.True (cb.NewKeyDownEvent (new (KeyCode.Esc)));
			Assert.Equal ("Two", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
		}

		[Fact, AutoInitShutdown]
		public void HideDropdownListOnClick_False_OpenSelectedItem_With_Mouse_And_Key_CursorDown_And_Esc ()
		{
			var selected = "";
			var cb = new ComboBox {
				Height = 4,
				Width = 5,
				HideDropdownListOnClick = false
			};
			cb.SetSource (new List<string> { "One", "Two", "Three" });
			cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
			Application.Top.Add (cb);
			Application.Begin (Application.Top);

			Assert.False (cb.HideDropdownListOnClick);
			Assert.False (cb.ReadOnly);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);

			Assert.True (cb.Subviews [1].NewKeyDownEvent (new (KeyCode.CursorDown)));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);

			Assert.True (cb.Subviews [1].NewKeyDownEvent (new (KeyCode.Enter)));
			Assert.Equal ("Two", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);

			Assert.True (cb.NewKeyDownEvent (new (KeyCode.F4)));
			Assert.Equal ("Two", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
			Assert.True (cb.Subviews [1].NewKeyDownEvent (new (KeyCode.CursorDown)));
			Assert.Equal ("Two", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);

			Assert.True (cb.NewKeyDownEvent (new (KeyCode.Esc)));
			Assert.Equal ("Two", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
		}

		[Fact, AutoInitShutdown]
		public void HideDropdownListOnClick_False_ReadOnly_True_OpenSelectedItem_With_Mouse_And_Key_CursorDown_And_Esc ()
		{
			var selected = "";
			var cb = new ComboBox {
				Height = 4,
				Width = 5,
				HideDropdownListOnClick = false,
				ReadOnly = true
			};
			cb.SetSource (new List<string> { "One", "Two", "Three" });
			cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
			Application.Top.Add (cb);
			Application.Begin (Application.Top);

			Assert.False (cb.HideDropdownListOnClick);
			Assert.True (cb.ReadOnly);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);

			Assert.True (cb.Subviews [1].NewKeyDownEvent (new (KeyCode.CursorDown)));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);

			Assert.True (cb.Subviews [1].NewKeyDownEvent (new (KeyCode.Enter)));
			Assert.Equal ("Two", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);

			Assert.True (cb.NewKeyDownEvent (new (KeyCode.F4)));
			Assert.Equal ("Two", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
			Assert.True (cb.Subviews [1].NewKeyDownEvent (new (KeyCode.CursorDown)));
			Assert.Equal ("Two", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);

			Assert.True (cb.NewKeyDownEvent (new (KeyCode.Esc)));
			Assert.Equal ("Two", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
		}

		[Fact, AutoInitShutdown]
		public void HideDropdownListOnClick_True_OpenSelectedItem_With_Mouse_And_Key_F4 ()
		{
			var selected = "";
			var cb = new ComboBox {
				Height = 4,
				Width = 5,
				HideDropdownListOnClick = true
			};
			cb.SetSource (new List<string> { "One", "Two", "Three" });
			cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
			Application.Top.Add (cb);
			Application.Begin (Application.Top);

			Assert.True (cb.HideDropdownListOnClick);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.Subviews [1].NewKeyDownEvent (new (KeyCode.CursorDown)));
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.F4)));
			Assert.Equal ("", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
		}

		[Fact, AutoInitShutdown]
		public void HideDropdownListOnClick_False_OpenSelectedItem_With_Mouse_And_Key_F4 ()
		{
			var selected = "";
			var cb = new ComboBox {
				Height = 4,
				Width = 5,
				HideDropdownListOnClick = false
			};
			cb.SetSource (new List<string> { "One", "Two", "Three" });
			cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
			Application.Top.Add (cb);
			Application.Begin (Application.Top);

			Assert.False (cb.HideDropdownListOnClick);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);

			Assert.True (cb.Subviews [1].NewKeyDownEvent (new (KeyCode.CursorDown)));
			Assert.True (cb.NewKeyDownEvent (new (KeyCode.F4)));
			Assert.Equal ("Two", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
		}

		[Fact, AutoInitShutdown]
		public void HideDropdownListOnClick_True_Colapse_On_Click_Outside_Frame ()
		{
			var selected = "";
			var cb = new ComboBox {
				Height = 4,
				Width = 5,
				HideDropdownListOnClick = true
			};
			cb.SetSource (new List<string> { "One", "Two", "Three" });
			cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
			Application.Top.Add (cb);
			Application.Begin (Application.Top);

			Assert.True (cb.HideDropdownListOnClick);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.True (cb.Subviews [1].MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.Subviews [1].MouseEvent (new MouseEvent {
				X = -1,
				Y = 0,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.NewKeyDownEvent (new (KeyCode.F4)));
			Assert.True (cb.Subviews [1].MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
			Assert.True (cb.Subviews [1].MouseEvent (new MouseEvent {
				X = 0,
				Y = -1,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.NewKeyDownEvent (new (KeyCode.F4)));
			Assert.True (cb.Subviews [1].MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
			Assert.True (cb.Subviews [1].MouseEvent (new MouseEvent {
				X = cb.Frame.Width,
				Y = 0,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.NewKeyDownEvent (new (KeyCode.F4)));
			Assert.True (cb.Subviews [1].MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
			Assert.True (cb.Subviews [1].MouseEvent (new MouseEvent {
				X = 0,
				Y = cb.Frame.Height,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
		}

		[Fact, AutoInitShutdown]
		public void HideDropdownListOnClick_True_Highlight_Current_Item ()
		{
			var selected = "";
			var cb = new ComboBox {
				Width = 6,
				Height = 4,
				HideDropdownListOnClick = true,
			};
			cb.SetSource (new List<string> { "One", "Two", "Three" });
			cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
			Application.Top.Add (cb);
			Application.Begin (Application.Top);

			Assert.True (cb.HideDropdownListOnClick);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
			cb.Draw ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
     ▼
One   
Two   
Three ", output);

			var attributes = new Attribute [] {
				// 0
				cb.Subviews [0].ColorScheme.Focus,
				// 1
				cb.Subviews [1].ColorScheme.HotFocus,
				// 2
				cb.Subviews [1].GetNormalColor ()
			};

			TestHelpers.AssertDriverAttributesAre (@"
000000
222222
222222
222222", driver: Application.Driver, attributes);

			Assert.True (cb.Subviews [1].NewKeyDownEvent (new (KeyCode.CursorDown)));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
			cb.Draw ();
			TestHelpers.AssertDriverAttributesAre (@"
000000
222222
000002
222222", driver: Application.Driver, attributes);

			Assert.True (cb.Subviews [1].NewKeyDownEvent (new (KeyCode.CursorDown)));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
			cb.Draw ();
			TestHelpers.AssertDriverAttributesAre (@"
000000
222222
222222
000002", driver: Application.Driver, attributes);

			Assert.True (cb.Subviews [1].NewKeyDownEvent (new (KeyCode.Enter)));
			Assert.Equal ("Three", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);

			Assert.True (cb.NewKeyDownEvent (new (KeyCode.F4)));
			Assert.Equal ("Three", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);
			cb.Draw ();
			TestHelpers.AssertDriverAttributesAre (@"
000000
222222
222222
000002", driver: Application.Driver, attributes);

			Assert.True (cb.Subviews [1].NewKeyDownEvent (new (KeyCode.CursorUp)));
			Assert.Equal ("Three", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);
			cb.Draw ();
			TestHelpers.AssertDriverAttributesAre (@"
000000
222222
000002
111112", driver: Application.Driver, attributes);

			Assert.True (cb.Subviews [1].NewKeyDownEvent (new (KeyCode.CursorUp)));
			Assert.Equal ("Three", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);
			cb.Draw ();
			TestHelpers.AssertDriverAttributesAre (@"
000000
000002
222222
111112", driver: Application.Driver, attributes);

			Assert.True (cb.NewKeyDownEvent (new (KeyCode.F4)));
			Assert.Equal ("Three", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);
		}

		[Fact, AutoInitShutdown]
		public void Expanded_Collapsed_Events ()
		{
			var cb = new ComboBox {
				Height = 4,
				Width = 5
			};
			var list = new List<string> { "One", "Two", "Three" };

			cb.Expanded += (s, e) => cb.SetSource (list);
			cb.Collapsed += (s, e) => cb.Source = null;

			Application.Top.Add (cb);
			Application.Begin (Application.Top);

			Assert.Null (cb.Source);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.NewKeyDownEvent (new (KeyCode.F4)));
			Assert.NotNull (cb.Source);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.NewKeyDownEvent (new (KeyCode.F4)));
			Assert.Null (cb.Source);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
		}
	}
}