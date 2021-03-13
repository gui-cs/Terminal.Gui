using NStack;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Terminal.Gui {

	/// <summary>
	/// A single tab in a <see cref="TabView"/>
	/// </summary>
	public class Tab {
		private ustring text;

		/// <summary>
		/// The text to display in a <see cref="TabView"/>
		/// </summary>
		/// <value></value>
		public ustring Text { get => text ?? "Unamed"; set => text = value; }

		/// <summary>
		/// The control to display when the tab is selected
		/// </summary>
		/// <value></value>
		public View View { get; set; }

		/// <summary>
		/// Creates a new unamed tab with no controls inside
		/// </summary>
		public Tab ()
		{

		}

		/// <summary>
		/// Creates a new tab with the given text hosting a view
		/// </summary>
		/// <param name="text"></param>
		/// <param name="view"></param>
		public Tab (string text, View view)
		{
			this.Text = text;
			this.View = view;
		}
	}

	/// <summary>
	/// Describes render stylistic selections of a <see cref="TabView"/>
	/// </summary>
	public class TabStyle {

		/// <summary>
		/// True to show the top lip of tabs.  False to directly begin with tab text during 
		/// rendering.  When true header line occupies 3 pixels, when false only 2.
		/// Defaults to true.
		/// 
		/// <para>When <see cref="TabsOnBottom"/> is enabled this instead applies to the
		///  bottomost line of the control</para>
		/// </summary> 
		public bool ShowHeaderOverline { get; set; } = true;

		/// <summary>
		/// True to render tabs at the bottom of the view instead of the top
		/// </summary>
		public bool TabsOnBottom { get; set; } = false;

	}

	/// <summary>
	/// Control that hosts multiple sub views, presenting a single one at once
	/// </summary>
	public class TabView : View {
		/// <summary>
		/// All tabs currently hosted by the control, after making changes call <see cref="View.SetNeedsDisplay()"/>
		/// </summary>
		/// <value></value>
		public List<Tab> Tabs { get; set; } = new List<Tab> ();

		/// <summary>
		/// The currently selected member of <see cref="Tabs"/> chosen by the user
		/// </summary>
		/// <value></value>
		public Tab SelectedTab { get; set; }

		/// <summary>
		/// Render choices for how to display tabs
		/// </summary>
		/// <value></value>
		public TabStyle Style { get; set; } = new TabStyle ();


		/// <summary>
		/// Initialzies a <see cref="TabView"/> class using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		public TabView () : base ()
		{
			CanFocus = true;
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			Move (0, 0);
			Driver.SetAttribute (ColorScheme.Normal);

			var tabLocations = MeasureTabs ().ToArray ();
			var width = bounds.Width;

			int currentLine = 0;

			if (Style.ShowHeaderOverline) {
				RenderOverline (tabLocations, width);
				currentLine++;
			}

			Move (0, currentLine);
			RenderTabLine (tabLocations, width, currentLine);
			currentLine++;

			//RenderTabUnderline (tabLocations, width);

		}


		/// <inheritdoc/>
		public override bool ProcessKey (KeyEvent keyEvent)
		{


			switch (keyEvent.Key) {
			case Key.CursorLeft:
				SwitchTabBy(-1);
				break;
			case Key.CursorRight:
				SwitchTabBy(1);
				break;
			default:
				// Not a keystroke we care about
				return false;
			}
			PositionCursor ();
			return true;
		}

		/// <summary>
		/// Changes the <see cref="SelectedTab"/> by the given <paramref name="amount"/>.  Positive for right, 
		/// negative for left.
		/// the first tab will become selected
		/// </summary>
		/// <param name="amount"></param>
		public void SwitchTabBy(int amount)
		{
			if(Tabs.Count == 0) {
				return;
			}

			// if there is only one tab anyway or nothing is selected
			if (Tabs.Count == 1 || SelectedTab == null) {
				SelectedTab = Tabs [0];
				SetNeedsDisplay ();
				return;
			}

			var currentIdx = Tabs.IndexOf(SelectedTab);

			// Currently selected tab has vanished!
			if (currentIdx == -1) {
				SelectedTab = Tabs [0];
				SetNeedsDisplay ();
				return;
			}

			var newIdx = Math.Max (0, Math.Min (currentIdx + amount, Tabs.Count - 1));

			SelectedTab = Tabs[newIdx];
			SetNeedsDisplay ();
		}

		private void RenderOverline (TabToRender [] tabLocations, int width)
		{
			// Renders the top line of selected tab like 
			//          

			Move (0, 0);

			var selected = tabLocations.FirstOrDefault (t => t.IsSelected);

			// Clear out everything
			Driver.AddStr (new string (' ', width));

			// Nothing is selected... odd but we are done
			if (selected == null) {
				return;
			}


			Move (selected.X - 1,0);
			Driver.AddRune (Driver.ULCorner);

			for (int i = 0; i < selected.Width; i++) {

				if (selected.X + i > width) {
					// we ran out of space horizontally
					return;
				}
				
				Driver.AddRune (Driver.HLine);
			}

			// Add the end of the selected tab
			Driver.AddRune (Driver.URCorner);

		}

		private void RenderTabLine(TabToRender [] tabLocations, int width, int currentLine)
		{
			foreach (var toRender in tabLocations) {

				if (toRender.IsSelected) {
					Move (toRender.X - 1, currentLine);
					Driver.AddRune (Driver.VLine);
				}

				Move (toRender.X, currentLine);
				Driver.SetAttribute(HasFocus && toRender.IsSelected ? ColorScheme.Focus: ColorScheme.Normal);
				Driver.AddStr (toRender.Tab.Text);
				Driver.SetAttribute (ColorScheme.Normal);

				if (toRender.IsSelected) {
					Driver.AddRune (Driver.VLine);
				}
			}
		}

		/// <summary>
		/// Returns which tabs to render at each x location
		/// </summary>
		/// <returns></returns>
		private IEnumerable<TabToRender> MeasureTabs ()
		{
			int i = 1;
			var toReturn = new Dictionary<int, Tab> ();

			foreach (var tab in Tabs) {
				var tabTextWidth = tab.Text.Sum (c => Rune.ColumnWidth (c));
				yield return new TabToRender (i, tab, Equals (SelectedTab, tab), tabTextWidth);
				i += tabTextWidth + 1;
			}
		}

		private class TabToRender {
			public int X { get; set; }
			public Tab Tab { get; set; }

			/// <summary>
			/// True if the tab that is being rendered is the selected one
			/// </summary>
			/// <value></value>
			public bool IsSelected { get; set; }
			public int Width { get; }

			public TabToRender (int x, Tab tab, bool isSelected, int width)
			{
				X = x;
				Tab = tab;
				IsSelected = isSelected;
				Width = width;
			}
		}
	}

}