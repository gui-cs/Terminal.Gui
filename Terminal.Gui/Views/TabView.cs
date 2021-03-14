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
		private List<Tab> tabs = new List<Tab> ();

		/// <summary>
		/// All tabs currently hosted by the control, after making changes call <see cref="View.SetNeedsDisplay()"/>
		/// </summary>
		/// <value></value>
		public IReadOnlyCollection<Tab> Tabs { get => tabs.AsReadOnly(); }

		/// <summary>
		/// When there are too many tabs to render, this indicates the first
		/// tab to render on the screen.
		/// </summary>
		/// <value></value>
		public int TabScrollOffset { get; set; }

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

				EnsureSelectedTabIsVisible ();
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
			contentView.Width = Dim.Fill (Style.ShowBorder ? 1 : 0);

			if (Style.TabsOnBottom) {
				// Tabs are along the bottom so just dodge the border
				contentView.Y = Style.ShowBorder ? 1 : 0;

				// Fill client area leaving space at bottom for tabs
				contentView.Height = Dim.Fill (GetTabHeight (false));

				tabsBar.Y = Pos.Bottom (this) - (Style.ShowHeaderOverline ? 2 : 1);
			} else {

				// Tabs are along the top
				contentView.Y = GetTabHeight (true);

				// Fill client area leaving space at bottom for border
				contentView.Height = Dim.Fill (Style.ShowBorder ? 1 : 0);

				// Should be able to just use 1 or 0 but switching between top/bottom tabs repeatedly breaks in ValidatePosDim if just using 1/0 without Pos.Top
				tabsBar.Y = Pos.Top (this) + (Style.ShowBorder ? 1 : 0);
			}


			SetNeedsDisplay ();
		}



		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			Move (0, 0);
			Driver.SetAttribute (ColorScheme.Normal);

			var tabLocations = CalculateViewport (bounds).ToArray ();
			var width = bounds.Width;

			int currentLine = 0;

			if (!Style.TabsOnBottom) {

				if (Style.ShowHeaderOverline) {
					RenderOverline (tabLocations, width, currentLine);
					currentLine++;
				}

				Move (0, currentLine);
				RenderTabLine (tabLocations, width, currentLine);

				currentLine++;
			}

			if (Style.ShowBorder) {

				// How muc space do we need to leave at the bottom to show the tabs
				int spaceAtBottom = Math.Max (0, GetTabHeight (false) - 1);

				DrawFrame (new Rect (0, currentLine, bounds.Width,
			       bounds.Height - spaceAtBottom - currentLine), 0, true);
			}

			// if we drew border then that will include a line under the tabs.  Otherwise we have to
			// draw that line manually
			if (!Style.ShowBorder) {

				// Prepare to draw the horizontal line below the tab text
				currentLine = Style.TabsOnBottom ? bounds.Height - GetTabHeight (false) : GetTabHeight (true) - 1;

				Move (0, currentLine);

				for (int x = 0; x < width; x++) {
					Driver.AddRune (Driver.HLine);
				}
			}


			if (Style.TabsOnBottom) {

				currentLine = bounds.Height - 1;

				Move (0, currentLine);

				if (Style.ShowHeaderOverline) {
					RenderOverline (tabLocations, width, currentLine);
					currentLine--;
				}

				Move (0, currentLine);
				RenderTabLine (tabLocations, width, currentLine);

				currentLine--;
			}

			RenderSelectedTabWhitespace (tabLocations, width, currentLine);

			// draw scroll indicators
			currentLine = Style.TabsOnBottom ? bounds.Height - GetTabHeight (false) : GetTabHeight (true) - 1;

			// if there are more tabs to the left not visible
			if (TabScrollOffset > 0) {
				Move (0, currentLine);

				// indicate that
				Driver.AddRune (Driver.LeftArrow);
			}

			// if there are mmore tabs to the right not visible
			if (tabLocations.LastOrDefault ()?.Tab != Tabs.LastOrDefault ()) {
				Move (bounds.Width - 1, currentLine);

				// indicate that
				Driver.AddRune (Driver.RightArrow);
			}

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
				case Key.Home:
					SelectedTab = Tabs.FirstOrDefault ();
					return true;
				case Key.End:
					SelectedTab = Tabs.LastOrDefault ();
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
				SelectedTab = Tabs.ElementAt (0);
				SetNeedsDisplay ();
				return;
			}

			var currentIdx = Tabs.IndexOf (SelectedTab);

			// Currently selected tab has vanished!
			if (currentIdx == -1) {
				SelectedTab = Tabs.ElementAt (0);
				SetNeedsDisplay ();
				return;
			}

			var newIdx = Math.Max (0, Math.Min (currentIdx + amount, Tabs.Count - 1));

			SelectedTab = tabs [newIdx];
			SetNeedsDisplay ();

			EnsureSelectedTabIsVisible ();
		}


		/// <summary>
		/// Updates <see cref="TabScrollOffset"/> to be a valid index of <see cref="Tabs"/>
		/// </summary>
		/// <remarks>Changes will not be immediately visible in the display until you call <see cref="View.SetNeedsDisplay()"/></remarks>
		public void EnsureValidScrollOffsets ()
		{
			TabScrollOffset = Math.Max (Math.Min (TabScrollOffset, Tabs.Count - 1), 0);
		}

		/// <summary>
		/// Updates <see cref="TabScrollOffset"/> to ensure that <see cref="SelectedTab"/> is visible
		/// </summary>
		public void EnsureSelectedTabIsVisible ()
		{
			if (SelectedTab == null) {
				return;
			}

			// if current viewport does not include the selected tab
			if (!CalculateViewport (Bounds).Any (r => Equals (SelectedTab, r.Tab))) {

				// Set scroll offset so the first tab rendered is the
				TabScrollOffset = Math.Max (0, Tabs.IndexOf (SelectedTab));
			}
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

			if (!top && !Style.TabsOnBottom) {
				return 0;
			}

			return Style.ShowHeaderOverline ? 3 : 2;
		}

		private void RenderOverline (TabToRender [] tabLocations, int width, int y)
		{
			Move (0, y);

			var selected = tabLocations.FirstOrDefault (t => t.IsSelected);

			// Clear out everything
			Driver.AddStr (new string (' ', width));

			// Nothing is selected... odd but we are done
			if (selected == null) {
				return;
			}


			Move (selected.X - 1, y);
			Driver.AddRune (Style.TabsOnBottom ? Driver.LLCorner : Driver.ULCorner);

			for (int i = 0; i < selected.Width; i++) {

				if (selected.X + i > width) {
					// we ran out of space horizontally
					return;
				}

				Driver.AddRune (Driver.HLine);
			}

			// Add the end of the selected tab
			Driver.AddRune (Style.TabsOnBottom ? Driver.LRCorner : Driver.URCorner);

		}

		private void RenderTabLine (TabToRender [] tabLocations, int width, int currentLine)
		{
            // clear any old text
            Move(0,currentLine);
            Driver.AddStr(new string(' ',width));

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

			Driver.AddRune (selected.X == 1 ? Driver.VLine :
		(Style.TabsOnBottom ? Driver.URCorner : Driver.LRCorner));

			Driver.AddStr (new string (' ', selected.Width));


			Driver.AddRune (selected.X + selected.Width == width - 1 ?
	     Driver.VLine :
			(Style.TabsOnBottom ? Driver.ULCorner : Driver.LLCorner));
		}

		/// <summary>
		/// Returns which tabs to render at each x location
		/// </summary>
		/// <returns></returns>
		private IEnumerable<TabToRender> CalculateViewport (Rect bounds)
		{
			int i = 1;

			// Starting at the first or scrolled to tab
			foreach (var tab in Tabs.Skip (TabScrollOffset)) {

				// while there is space for the tab
				var tabTextWidth = tab.Text.Sum (c => Rune.ColumnWidth (c));

				// if there is not enough space for this tab
				if (i + tabTextWidth >= bounds.Width) {
					break;
				}

				// there is enough space!
				yield return new TabToRender (i, tab, Equals (SelectedTab, tab), tabTextWidth);
				i += tabTextWidth + 1;
			}
		}

		/// <summary>
		/// Adds the given <paramref name="tab"/> to <see cref="Tabs"/>
		/// </summary>
		/// <param name="tab"></param>
		/// <param name="andSelect">True to make the newly added Tab the <see cref="SelectedTab"/></param>
		public void AddTab (Tab tab, bool andSelect)
		{
			tabs.Add (tab);

            if(SelectedTab == null || andSelect){
                SelectedTab = tab;
                EnsureSelectedTabIsVisible();
            }

			SetNeedsDisplay ();
		}


		/// <summary>
		/// Removes the given <paramref name="tab"/> from <see cref="Tabs"/>.
        /// Optionally disposes the tabs hosted <see cref="Tab.View"/>>
		/// </summary>
		/// <param name="tab"></param>
		/// <param name="dispose">True to dispose of the tabs control</param>
		public void RemoveTab (Tab tab, bool dispose)
		{
            if(tab == null || !tabs.Contains(tab))
            {
                return;
            }

            // what tab was selected before closing
            var idx = tabs.IndexOf(tab);

			tabs.Remove (tab);

            if(dispose){
                tab.View.Dispose();
            }

            // if the currently selected tab is no longer a member of Tabs
            if(!Tabs.Contains(SelectedTab))
            {
                // select the tab closest to the one that disapeared
                var toSelect = Math.Max(idx-1,0);

                if(toSelect < Tabs.Count){
                    SelectedTab = Tabs.ElementAt(toSelect);
                }
                else
                {
                    SelectedTab = Tabs.LastOrDefault();
                }

            }

            EnsureSelectedTabIsVisible();
			SetNeedsDisplay ();
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

				var selected = host.CalculateViewport (Bounds).FirstOrDefault (t => Equals (host.SelectedTab, t.Tab));

				if (selected == null) {
					return;
				}


				Move (selected.X, 0);
			}
		}
	}

}