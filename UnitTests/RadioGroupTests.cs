using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Terminal.Gui.Views {
	public class RadioGroupTests {
		[Fact]
		public void Constructors_Defaults ()
		{
			var rg = new RadioGroup ();
			Assert.True (rg.CanFocus);
			Assert.Empty (rg.RadioLabels);
			Assert.Equal (0, rg.X);
			Assert.Equal (0, rg.Y);
			Assert.Equal (0, rg.Width);
			Assert.Equal (0, rg.Height);
			Assert.Equal (0, rg.SelectedItem);

			rg = new RadioGroup (new NStack.ustring [] { "Test" });
			Assert.True (rg.CanFocus);
			Assert.Single (rg.RadioLabels);
			Assert.Equal (0, rg.X);
			Assert.Equal (0, rg.Y);
			Assert.Equal (7, rg.Width);
			Assert.Equal (1, rg.Height);
			Assert.Equal (0, rg.SelectedItem);

			rg = new RadioGroup (new Rect (1, 2, 20, 5), new NStack.ustring [] { "Test" });
			Assert.True (rg.CanFocus);
			Assert.Single (rg.RadioLabels);
			Assert.Equal (1, rg.X);
			Assert.Equal (2, rg.Y);
			Assert.Equal (20, rg.Width);
			Assert.Equal (5, rg.Height);
			Assert.Equal (0, rg.SelectedItem);

			rg = new RadioGroup (1, 2, new NStack.ustring [] { "Test" });
			Assert.True (rg.CanFocus);
			Assert.Single (rg.RadioLabels);
			Assert.Equal (1, rg.X);
			Assert.Equal (2, rg.Y);
			Assert.Equal (7, rg.Width);
			Assert.Equal (1, rg.Height);
			Assert.Equal (0, rg.SelectedItem);
		}

		[Fact]
		public void Initialize_SelectedItem_With_Minus_One ()
		{
			var rg = new RadioGroup (new NStack.ustring [] { "Test" }, -1);
			Assert.Equal (-1, rg.SelectedItem);
			Assert.True (rg.ProcessKey (new KeyEvent (Key.Space, new KeyModifiers ())));
			Assert.Equal (0, rg.SelectedItem);
		}

		[Fact]
		public void DisplayMode_Width_Height_HorizontalSpace ()
		{
			var rg = new RadioGroup (new NStack.ustring [] { "Test", "New Test" });
			Assert.Equal (DisplayModeLayout.Vertical, rg.DisplayMode);
			Assert.Equal (2, rg.RadioLabels.Length);
			Assert.Equal (0, rg.X);
			Assert.Equal (0, rg.Y);
			Assert.Equal (11, rg.Width);
			Assert.Equal (2, rg.Height);

			rg.DisplayMode = DisplayModeLayout.Horizontal;
			Assert.Equal (DisplayModeLayout.Horizontal, rg.DisplayMode);
			Assert.Equal (2, rg.HorizontalSpace);
			Assert.Equal (0, rg.X);
			Assert.Equal (0, rg.Y);
			Assert.Equal (16, rg.Width);
			Assert.Equal (1, rg.Height);

			rg.HorizontalSpace = 4;
			Assert.Equal (DisplayModeLayout.Horizontal, rg.DisplayMode);
			Assert.Equal (4, rg.HorizontalSpace);
			Assert.Equal (0, rg.X);
			Assert.Equal (0, rg.Y);
			Assert.Equal (20, rg.Width);
			Assert.Equal (1, rg.Height);
		}

		[Fact]
		public void SelectedItemChanged_Event ()
		{
			var previousSelectedItem = -1;
			var selectedItem = -1;
			var rg = new RadioGroup (new NStack.ustring [] { "Test", "New Test" });
			rg.SelectedItemChanged += (e) => {
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
			var rg = new RadioGroup (new NStack.ustring [] { "Test", "New Test" });

			Assert.True (rg.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.True (rg.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (rg.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));
			Assert.True (rg.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.True (rg.ProcessKey (new KeyEvent (Key.Space, new KeyModifiers ())));
			Assert.Equal (1, rg.SelectedItem);
		}
	}
}
