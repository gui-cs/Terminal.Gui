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
		/// True to show a solid box around the edge of the control.  Defaults to true.
		/// </summary>
		public bool ShowBorder { get; set; } = true;

		/// <summary>
		/// True to render tabs at the bottom of the view instead of the top
		/// </summary>
		public bool TabsOnBottom { get; set; } = false;

	}

	/// <summary>
	/// Control that hosts multiple sub views, presenting a single one at once
	/// </summary>
	public class TabView : View {
		private Tab selectedTab;

		/// <summary>
		/// It seems like a control cannot have both subviews and be
		/// focusable.  Therefore this proxy view is needed. It allows
		/// tab based navigation to switch between the <see cref="contentView"/>
		/// and the tabs
		/// </summary>
		TabRowView tabsBar;

		View contentView;

		/// <summary>
		/// All tabs currently hosted by the control, after making changes call <see cref="View.SetNeedsDisplay()"/>
		/// </summary>
		/// <value></value>
		public List<Tab> Tabs { get; set; } = new List<Tab> ();

		/// <summary>
		/// The currently selected member of <see cref="Tabs"/> chosen by the user
		/// </summary>
		/// <value></value>
		public Tab SelectedTab {
			get => selectedTab;
			set {

				if (selectedTab != null) {
					// remove old content
					contentView.Remove (selectedTab.View);
				}

				selectedTab = value;

				if (value != null) {

					// add new content
					contentView.Add (selectedTab.View);
				}
			}
		}

		/// <summary>
		/// Render choices for how to display tabs.  After making changes, call <see cref="ApplyStyleChanges()"/>
		/// </summary>
		/// <value></value>
		public TabStyle Style { get; set; } = new TabStyle ();


		/// <summary>
		/// Initialzies a <see cref="TabView"/> class using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		public TabView () : base ()
		{
			CanFocus = true;
			contentView = new View ();
			tabsBar = new TabRowView (this);

			ApplyStyleChanges ();

			base.Add (tabsBar);
			base.Add (contentView);
		}

		/// <summary>
		/// Updates the control to use the latest state settings in <see cref="Style"/>.
		/// This can change the size of the client area of the tab (for rendering the 
		/// selected tab's content).  This method includes a call 
		/// to <see cref="View.SetNeedsDisplay()"/>
		/// </summary>
		public void ApplyStyleChanges ()
		{

			contentView.X = Style.ShowBorder ? 1 : 0;
			contentView.Y = GetTabHeight (true);

			contentView.Height = Dim.Fill (GetTabHeight (false));
			contentView.Width = Dim.Fill (Style.ShowBorder ? 1 : 0);

			tabsBar.Y = Style.ShowBorder ? 1 : 0;

			SetNeedsDisplay ();
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

			if (Style.ShowBorder) {
				DrawFrame (new Rect (0, currentLine, bounds.Width, bounds.Height - currentLine), 0, true);
			} else {

				Move (0, currentLine);

				for (int x = 0; x < width; x++) {
					Driver.AddRune (Driver.HLine);
				}
			}


			RenderSelectedTabWhitespace (tabLocations, width, currentLine);

			contentView.Redraw (contentView.Bounds);
		}

		/// <summary>
		/// Disposes the control and all <see cref="Tabs"/>
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);

			// The selected tab will automatically be disposed but
			// any tabs not visible will need to be manually disposed

			foreach (var tab in Tabs) {
				if (!Equals (SelectedTab, tab)) {
					tab.View.Dispose ();
				}

			}
		}


		/// <inheritdoc/>
		public override bool ProcessKey (KeyEvent keyEvent)
		{
			if (HasFocus && CanFocus && Focused == tabsBar) {
				switch (keyEvent.Key) {

				case Key.CursorLeft:
					SwitchTabBy (-1);
					return true;
				case Key.CursorRight:
					SwitchTabBy (1);
					return true;
				}
			}

			return base.ProcessKey (keyEvent);
		}

		/// <summary>
		/// Changes the <see cref="SelectedTab"/> by the given <paramref name="amount"/>.  Positive for right, 
		/// negative for left.
		/// the first tab will become selected
		/// </summary>
		/// <param name="amount"></param>
		public void SwitchTabBy (int amount)
		{
			if (Tabs.Count == 0) {
				return;
			}

			// if there is only one tab anyway or nothing is selected
			if (Tabs.Count == 1 || SelectedTab == null) {
				SelectedTab = Tabs [0];
				SetNeedsDisplay ();
				return;
			}

			var currentIdx = Tabs.IndexOf (SelectedTab);

			// Currently selected tab has vanished!
			if (currentIdx == -1) {
				SelectedTab = Tabs [0];
				SetNeedsDisplay ();
				return;
			}

			var newIdx = Math.Max (0, Math.Min (currentIdx + amount, Tabs.Count - 1));

			SelectedTab = Tabs [newIdx];
			SetNeedsDisplay ();
		}



		/// <summary>
		/// Returns the number of rows occupied by rendering the tabs, this depends 
		/// on <see cref="TabStyle.ShowHeaderOverline"/> and can be 0 (e.g. if 
		/// <see cref="TabStyle.TabsOnBottom"/> and you ask for <paramref name="top"/>).
		/// </summary>
		/// <param name="top">True to measure the space required at the top of the control,
		/// false to measure space at the bottom</param>
		/// <returns></returns>
		private int GetTabHeight (bool top)
		{
			if (top && Style.TabsOnBottom) {
				return 0;
			}

			return Style.ShowHeaderOverline ? 3 : 2;
		}

		private void RenderOverline (TabToRender [] tabLocations, int width)
		{
			Move (0, 0);

			var selected = tabLocations.FirstOrDefault (t => t.IsSelected);

			// Clear out everything
			Driver.AddStr (new string (' ', width));

			// Nothing is selected... odd but we are done
			if (selected == null) {
				return;
			}


			Move (selected.X - 1, 0);
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

		private void RenderTabLine (TabToRender [] tabLocations, int width, int currentLine)
		{
			foreach (var toRender in tabLocations) {

				if (toRender.IsSelected) {
					Move (toRender.X - 1, currentLine);
					Driver.AddRune (Driver.VLine);
				}

				Move (toRender.X, currentLine);
				Driver.SetAttribute (HasFocus && toRender.IsSelected ? ColorScheme.Focus : ColorScheme.Normal);
				Driver.AddStr (toRender.Tab.Text);
				Driver.SetAttribute (ColorScheme.Normal);

				if (toRender.IsSelected) {
					Driver.AddRune (Driver.VLine);
				}
			}
		}

		/// <summary>
		/// Draws whitespace over the top of the bounding rectangle so the selected tab appears
		/// to flow into the box
		/// </summary>
		/// <param name="tabLocations"></param>
		/// <param name="width"></param>
		/// <param name="currentLine"></param>
		private void RenderSelectedTabWhitespace (TabToRender [] tabLocations, int width, int currentLine)
		{

			var selected = tabLocations.FirstOrDefault (t => t.IsSelected);

			if (selected == null) {
				return;
			}

			Move (selected.X - 1, currentLine);
			Driver.AddRune (selected.X == 1 ? Driver.VLine : Driver.LRCorner);

			Driver.AddStr (new string (' ', selected.Width));
			Driver.AddRune (Driver.LLCorner);
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

		private class TabRowView : View {

			readonly TabView host;

			public TabRowView (TabView host)
			{
				this.host = host;

				CanFocus = true;
				Height = 1;
				Width = Dim.Fill ();
			}

			/// <summary>
			/// Positions the cursor at the start of the currently selected tab
			/// </summary>
			public override void PositionCursor ()
			{
				base.PositionCursor ();

				var selected = host.MeasureTabs().FirstOrDefault (t => Equals (host.SelectedTab, t.Tab));
	
				if (selected == null) {
					return;
				}


				Move (selected.X, 0);
			}
		}
	}

}