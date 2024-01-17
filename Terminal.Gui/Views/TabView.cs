using System;
using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui;

/// <summary>
/// Control that hosts multiple sub views, presenting a single one at once.
/// </summary>
public class TabView : View {
	private Tab _selectedTab;

	/// <summary>
	/// The default <see cref="MaxTabTextWidth"/> to set on new <see cref="TabView"/> controls.
	/// </summary>
	public const uint DefaultMaxTabTextWidth = 30;

	/// <summary>
	/// This sub view is the 2 or 3 line control that represents the actual tabs themselves.
	/// </summary>
	TabRowView _tabsBar;

	/// <summary>
	/// This sub view is the main client area of the current tab.  It hosts the <see cref="Tab.View"/> 
	/// of the tab, the <see cref="SelectedTab"/>.
	/// </summary>
	View _contentView;
	private List<Tab> _tabs = new List<Tab> ();

	/// <summary>
	/// All tabs currently hosted by the control.
	/// </summary>
	/// <value></value>
	public IReadOnlyCollection<Tab> Tabs { get => _tabs.AsReadOnly (); }

	/// <summary>
	/// When there are too many tabs to render, this indicates the first
	/// tab to render on the screen.
	/// </summary>
	/// <value></value>
	public int TabScrollOffset {
		get => _tabScrollOffset;
		set {
			_tabScrollOffset = EnsureValidScrollOffsets (value);
		}
	}

	/// <summary>
	/// The maximum number of characters to render in a Tab header.  This prevents one long tab 
	/// from pushing out all the others.
	/// </summary>
	public uint MaxTabTextWidth { get; set; } = DefaultMaxTabTextWidth;

	/// <summary>
	/// Event for when <see cref="SelectedTab"/> changes.
	/// </summary>
	public event EventHandler<TabChangedEventArgs> SelectedTabChanged;

	/// <summary>
	/// Event fired when a <see cref="Tab"/> is clicked.  Can be used to cancel navigation,
	/// show context menu (e.g. on right click) etc.
	/// </summary>
	public event EventHandler<TabMouseEventArgs> TabClicked;

	/// <summary>
	/// The currently selected member of <see cref="Tabs"/> chosen by the user.
	/// </summary>
	/// <value></value>
	public Tab SelectedTab {
		get => _selectedTab;
		set {
			UnSetCurrentTabs ();

			var old = _selectedTab;

			if (_selectedTab != null) {
				if (_selectedTab.View != null) {
					// remove old content
					_contentView.Remove (_selectedTab.View);
				}
			}

			_selectedTab = value;

			if (value != null) {
				// add new content
				if (_selectedTab.View != null) {
					_contentView.Add (_selectedTab.View);
				}
			}

			EnsureSelectedTabIsVisible ();

			if (old != value) {
				if (old?.HasFocus == true) {
					SelectedTab.SetFocus ();
				}
				OnSelectedTabChanged (old, value);
			}
		}
	}

	/// <summary>
	/// Render choices for how to display tabs.  After making changes, call <see cref="ApplyStyleChanges()"/>.
	/// </summary>
	/// <value></value>
	public TabStyle Style { get; set; } = new TabStyle ();

	/// <summary>
	/// Initializes a <see cref="TabView"/> class using <see cref="LayoutStyle.Computed"/> layout.
	/// </summary>
	public TabView () : base ()
	{
		CanFocus = true;
		_tabsBar = new TabRowView (this);
		_contentView = new View ();

		ApplyStyleChanges ();

		base.Add (_tabsBar);
		base.Add (_contentView);

		// Things this view knows how to do
		AddCommand (Command.Left, () => { SwitchTabBy (-1); return true; });
		AddCommand (Command.Right, () => { SwitchTabBy (1); return true; });
		AddCommand (Command.LeftHome, () => { TabScrollOffset = 0; SelectedTab = Tabs.FirstOrDefault (); return true; });
		AddCommand (Command.RightEnd, () => { TabScrollOffset = Tabs.Count - 1; SelectedTab = Tabs.LastOrDefault (); return true; });
		AddCommand (Command.NextView, () => { _contentView.SetFocus (); return true; });
		AddCommand (Command.PreviousView, () => { SuperView?.FocusPrev (); return true; });
		AddCommand (Command.PageDown, () => { TabScrollOffset += _tabLocations.Length; SelectedTab = Tabs.ElementAt (TabScrollOffset); return true; });
		AddCommand (Command.PageUp, () => { TabScrollOffset -= _tabLocations.Length; SelectedTab = Tabs.ElementAt (TabScrollOffset); return true; });

		// Default keybindings for this view
		KeyBindings.Add (KeyCode.CursorLeft, Command.Left);
		KeyBindings.Add (KeyCode.CursorRight, Command.Right);
		KeyBindings.Add (KeyCode.Home, Command.LeftHome);
		KeyBindings.Add (KeyCode.End, Command.RightEnd);
		KeyBindings.Add (KeyCode.CursorDown, Command.NextView);
		KeyBindings.Add (KeyCode.CursorUp, Command.PreviousView);
		KeyBindings.Add (KeyCode.PageDown, Command.PageDown);
		KeyBindings.Add (KeyCode.PageUp, Command.PageUp);
	}

	/// <summary>
	/// Updates the control to use the latest state settings in <see cref="Style"/>.
	/// This can change the size of the client area of the tab (for rendering the 
	/// selected tab's content).  This method includes a call 
	/// to <see cref="View.SetNeedsDisplay()"/>.
	/// </summary>
	public void ApplyStyleChanges ()
	{
		_contentView.BorderStyle = Style.ShowBorder ? LineStyle.Single : LineStyle.None;
		_contentView.Width = Dim.Fill ();

		if (Style.TabsOnBottom) {
			// Tabs are along the bottom so just dodge the border
			if (Style.ShowBorder) {
				_contentView.Border.Thickness = new Thickness (1, 1, 1, 0);
			}

			_contentView.Y = 0;

			var tabHeight = GetTabHeight (false);

			// Fill client area leaving space at bottom for tabs
			_contentView.Height = Dim.Fill (tabHeight);

			_tabsBar.Height = tabHeight;

			_tabsBar.Y = Pos.Bottom (_contentView);

		} else {

			// Tabs are along the top
			if (Style.ShowBorder) {
				_contentView.Border.Thickness = new Thickness (1, 0, 1, 1);
			}

			_tabsBar.Y = 0;

			var tabHeight = GetTabHeight (true);

			//move content down to make space for tabs
			_contentView.Y = Pos.Bottom (_tabsBar);

			// Fill client area leaving space at bottom for border
			_contentView.Height = Dim.Fill ();

			// The top tab should be 2 or 3 rows high and on the top

			_tabsBar.Height = tabHeight;

			// Should be able to just use 0 but switching between top/bottom tabs repeatedly breaks in ValidatePosDim if just using the absolute value 0
		}
		if (IsInitialized) {
			LayoutSubviews ();
		}
		SetNeedsDisplay ();
	}

	///<inheritdoc/>
	public override void OnDrawContent (Rect contentArea)
	{
		Driver.SetAttribute (GetNormalColor ());

		if (Tabs.Any ()) {
			var savedClip = ClipToBounds ();
			_tabsBar.OnDrawContent (contentArea);
			_contentView.SetNeedsDisplay ();
			_contentView.Draw ();
			Driver.Clip = savedClip;
		}
	}

	///<inheritdoc/>
	public override void OnDrawContentComplete (Rect contentArea)
	{
		_tabsBar.OnDrawContentComplete (contentArea);
	}

	/// <summary>
	/// Disposes the control and all <see cref="Tabs"/>.
	/// </summary>
	/// <param name="disposing"></param>
	protected override void Dispose (bool disposing)
	{
		base.Dispose (disposing);

		// The selected tab will automatically be disposed but
		// any tabs not visible will need to be manually disposed

		foreach (var tab in Tabs) {
			if (!Equals (SelectedTab, tab)) {
				tab.View?.Dispose ();
			}
		}
	}

	/// <summary>
	/// Raises the <see cref="SelectedTabChanged"/> event.
	/// </summary>
	protected virtual void OnSelectedTabChanged (Tab oldTab, Tab newTab)
	{

		SelectedTabChanged?.Invoke (this, new TabChangedEventArgs (oldTab, newTab));
	}

	/// <summary>
	/// Changes the <see cref="SelectedTab"/> by the given <paramref name="amount"/>. 
	/// Positive for right, negative for left.  If no tab is currently selected then
	/// the first tab will become selected.
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

		SelectedTab = _tabs [newIdx];
		SetNeedsDisplay ();

		EnsureSelectedTabIsVisible ();
	}

	/// <summary>
	/// Updates <see cref="TabScrollOffset"/> to be a valid index of <see cref="Tabs"/>.
	/// </summary>
	/// <param name="value">The value to validate.</param>
	/// <remarks>Changes will not be immediately visible in the display until you call <see cref="View.SetNeedsDisplay()"/>.</remarks>
	/// <returns>The valid <see cref="TabScrollOffset"/> for the given value.</returns>
	public int EnsureValidScrollOffsets (int value)
	{
		return Math.Max (Math.Min (value, Tabs.Count - 1), 0);
	}

	/// <summary>
	/// Updates <see cref="TabScrollOffset"/> to ensure that <see cref="SelectedTab"/> is visible.
	/// </summary>
	public void EnsureSelectedTabIsVisible ()
	{
		if (!IsInitialized || SelectedTab == null) {
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
	/// on <see cref="TabStyle.ShowTopLine"/> and can be 0 (e.g. if 
	/// <see cref="TabStyle.TabsOnBottom"/> and you ask for <paramref name="top"/>).
	/// </summary>
	/// <param name="top">True to measure the space required at the top of the control,
	/// false to measure space at the bottom.</param>.
	/// <returns></returns>
	private int GetTabHeight (bool top)
	{
		if (top && Style.TabsOnBottom) {
			return 0;
		}

		if (!top && !Style.TabsOnBottom) {
			return 0;
		}

		return Style.ShowTopLine ? 3 : 2;
	}

	private TabToRender [] _tabLocations;
	private int _tabScrollOffset;

	/// <summary>
	/// Returns which tabs to render at each x location.
	/// </summary>
	/// <returns></returns>
	private IEnumerable<TabToRender> CalculateViewport (Rect bounds)
	{
		UnSetCurrentTabs ();

		int i = 1;
		View prevTab = null;

		// Starting at the first or scrolled to tab
		foreach (var tab in Tabs.Skip (TabScrollOffset)) {

			if (prevTab != null) {
				tab.X = Pos.Right (prevTab);
			} else {
				tab.X = 0;
			}
			tab.Y = 0;

			// while there is space for the tab
			var tabTextWidth = tab.DisplayText.EnumerateRunes ().Sum (c => c.GetColumns ());

			string text = tab.DisplayText;

			// The maximum number of characters to use for the tab name as specified
			// by the user (MaxTabTextWidth).  But not more than the width of the view
			// or we won't even be able to render a single tab!
			var maxWidth = Math.Max (0, Math.Min (bounds.Width - 3, MaxTabTextWidth));

			prevTab = tab;

			tab.Width = 2;
			tab.Height = Style.ShowTopLine ? 3 : 2;

			// if tab view is width <= 3 don't render any tabs
			if (maxWidth == 0) {
				tab.Visible = true;
				tab.MouseClick += Tab_MouseClick;
				yield return new TabToRender (i, tab, string.Empty, Equals (SelectedTab, tab), 0);
				break;
			}

			if (tabTextWidth > maxWidth) {
				text = tab.DisplayText.Substring (0, (int)maxWidth);
				tabTextWidth = (int)maxWidth;
			}

			tab.Width = Math.Max (tabTextWidth + 2, 1);
			tab.Height = Style.ShowTopLine ? 3 : 2;

			// if there is not enough space for this tab
			if (i + tabTextWidth >= bounds.Width) {
				tab.Visible = false;
				break;
			}

			// there is enough space!
			tab.Visible = true;
			tab.MouseClick += Tab_MouseClick;
			yield return new TabToRender (i, tab, text, Equals (SelectedTab, tab), tabTextWidth);
			i += tabTextWidth + 1;
		}
	}

	private void UnSetCurrentTabs ()
	{
		if (_tabLocations != null) {
			foreach (var tabToRender in _tabLocations) {
				tabToRender.Tab.MouseClick -= Tab_MouseClick;
				tabToRender.Tab.Visible = false;
			}
			_tabLocations = null;
		}
	}

	private void Tab_MouseClick (object sender, MouseEventEventArgs e)
	{
		e.Handled = _tabsBar.MouseEvent (e.MouseEvent);
	}

	/// <summary>
	/// Adds the given <paramref name="tab"/> to <see cref="Tabs"/>.
	/// </summary>
	/// <param name="tab"></param>
	/// <param name="andSelect">True to make the newly added Tab the <see cref="SelectedTab"/>.</param>
	public void AddTab (Tab tab, bool andSelect)
	{
		if (_tabs.Contains (tab)) {
			return;
		}

		_tabs.Add (tab);
		_tabsBar.Add (tab);

		if (SelectedTab == null || andSelect) {
			SelectedTab = tab;

			EnsureSelectedTabIsVisible ();

			tab.View?.SetFocus ();
		}

		SetNeedsDisplay ();
	}

	/// <summary>
	/// Removes the given <paramref name="tab"/> from <see cref="Tabs"/>.
	/// Caller is responsible for disposing the tab's hosted <see cref="Tab.View"/>
	/// if appropriate.
	/// </summary>
	/// <param name="tab"></param>
	public void RemoveTab (Tab tab)
	{
		if (tab == null || !_tabs.Contains (tab)) {
			return;
		}

		// what tab was selected before closing
		var idx = _tabs.IndexOf (tab);

		_tabs.Remove (tab);

		// if the currently selected tab is no longer a member of Tabs
		if (SelectedTab == null || !Tabs.Contains (SelectedTab)) {
			// select the tab closest to the one that disappeared
			var toSelect = Math.Max (idx - 1, 0);

			if (toSelect < Tabs.Count) {
				SelectedTab = Tabs.ElementAt (toSelect);
			} else {
				SelectedTab = Tabs.LastOrDefault ();
			}

		}

		EnsureSelectedTabIsVisible ();
		SetNeedsDisplay ();
	}

	private class TabToRender {
		public int X { get; set; }
		public Tab Tab { get; set; }

		/// <summary>
		/// True if the tab that is being rendered is the selected one.
		/// </summary>
		/// <value></value>
		public bool IsSelected { get; set; }
		public int Width { get; }
		public string TextToRender { get; }

		public TabToRender (int x, Tab tab, string textToRender, bool isSelected, int width)
		{
			X = x;
			Tab = tab;
			IsSelected = isSelected;
			Width = width;
			TextToRender = textToRender;
		}
	}

	private class TabRowView : View {
		readonly TabView _host;
		View _rightScrollIndicator;
		View _leftScrollIndicator;

		public TabRowView (TabView host)
		{
			this._host = host;

			CanFocus = true;
			Height = 1;
			Width = Dim.Fill ();

			_rightScrollIndicator = new View () { Id = "rightScrollIndicator", Width = 1, Height = 1, Visible = false, Text = CM.Glyphs.RightArrow.ToString () };
			_rightScrollIndicator.MouseClick += _host.Tab_MouseClick;
			_leftScrollIndicator = new View () { Id = "leftScrollIndicator", Width = 1, Height = 1, Visible = false, Text = CM.Glyphs.LeftArrow.ToString () };
			_leftScrollIndicator.MouseClick += _host.Tab_MouseClick;

			Add (_rightScrollIndicator, _leftScrollIndicator);
		}

		public override bool OnEnter (View view)
		{
			Driver.SetCursorVisibility (CursorVisibility.Invisible);
			return base.OnEnter (view);
		}

		public override void OnDrawContent (Rect contentArea)
		{
			_host._tabLocations = _host.CalculateViewport (Bounds).ToArray ();

			// clear any old text
			Clear ();

			RenderTabLine ();

			RenderUnderline ();
			Driver.SetAttribute (GetNormalColor ());
		}

		public override void OnDrawContentComplete (Rect contentArea)
		{
			if (_host._tabLocations == null) {
				return;
			}

			var tabLocations = _host._tabLocations;
			var selectedTab = -1;

			for (int i = 0; i < tabLocations.Length; i++) {
				View tab = tabLocations [i].Tab;
				var vts = tab.BoundsToScreen (tab.Bounds);
				LineCanvas lc = new LineCanvas ();
				var selectedOffset = _host.Style.ShowTopLine && tabLocations [i].IsSelected ? 0 : 1;

				if (tabLocations [i].IsSelected) {
					selectedTab = i;

					if (i == 0 && _host.TabScrollOffset == 0) {
						if (_host.Style.TabsOnBottom) {
							// Upper left vertical line
							lc.AddLine (new Point (vts.X - 1, vts.Y - 1), -1, Orientation.Vertical, tab.BorderStyle);
						} else {
							// Lower left vertical line
							lc.AddLine (new Point (vts.X - 1, vts.Bottom - selectedOffset), -1, Orientation.Vertical, tab.BorderStyle);
						}
					} else if (i > 0 && i <= tabLocations.Length - 1) {
						if (_host.Style.TabsOnBottom) {
							// URCorner
							lc.AddLine (new Point (vts.X - 1, vts.Y - 1), 1, Orientation.Vertical, tab.BorderStyle);
							lc.AddLine (new Point (vts.X - 1, vts.Y - 1), -1, Orientation.Horizontal, tab.BorderStyle);
						} else {
							// LRCorner
							lc.AddLine (new Point (vts.X - 1, vts.Bottom - selectedOffset), -1, Orientation.Vertical, tab.BorderStyle);
							lc.AddLine (new Point (vts.X - 1, vts.Bottom - selectedOffset), -1, Orientation.Horizontal, tab.BorderStyle);
						}

						if (_host.Style.ShowTopLine) {
							if (_host.Style.TabsOnBottom) {
								// Lower left tee
								lc.AddLine (new Point (vts.X - 1, vts.Bottom), -1, Orientation.Vertical, tab.BorderStyle);
								lc.AddLine (new Point (vts.X - 1, vts.Bottom), 0, Orientation.Horizontal, tab.BorderStyle);
							} else {
								// Upper left tee
								lc.AddLine (new Point (vts.X - 1, vts.Y - 1), 1, Orientation.Vertical, tab.BorderStyle);
								lc.AddLine (new Point (vts.X - 1, vts.Y - 1), 0, Orientation.Horizontal, tab.BorderStyle);
							}
						}
					}

					if (i < tabLocations.Length - 1) {
						if (_host.Style.ShowTopLine) {
							if (_host.Style.TabsOnBottom) {
								// Lower right tee
								lc.AddLine (new Point (vts.Right, vts.Bottom), -1, Orientation.Vertical, tab.BorderStyle);
								lc.AddLine (new Point (vts.Right, vts.Bottom), 0, Orientation.Horizontal, tab.BorderStyle);
							} else {
								// Upper right tee
								lc.AddLine (new Point (vts.Right, vts.Y - 1), 1, Orientation.Vertical, tab.BorderStyle);
								lc.AddLine (new Point (vts.Right, vts.Y - 1), 0, Orientation.Horizontal, tab.BorderStyle);
							}
						}
					}

					if (_host.Style.TabsOnBottom) {
						//URCorner
						lc.AddLine (new Point (vts.Right, vts.Y - 1), 1, Orientation.Vertical, tab.BorderStyle);
						lc.AddLine (new Point (vts.Right, vts.Y - 1), 1, Orientation.Horizontal, tab.BorderStyle);
					} else {
						//LLCorner
						lc.AddLine (new Point (vts.Right, vts.Bottom - selectedOffset), -1, Orientation.Vertical, tab.BorderStyle);
						lc.AddLine (new Point (vts.Right, vts.Bottom - selectedOffset), 1, Orientation.Horizontal, tab.BorderStyle);
					}

				} else if (selectedTab == -1) {
					if (i == 0 && string.IsNullOrEmpty (tab.Text)) {
						if (_host.Style.TabsOnBottom) {
							if (_host.Style.ShowTopLine) {
								// LLCorner
								lc.AddLine (new Point (vts.X - 1, vts.Bottom), -1, Orientation.Vertical, tab.BorderStyle);
								lc.AddLine (new Point (vts.X - 1, vts.Bottom), 1, Orientation.Horizontal, tab.BorderStyle);
							}
							// ULCorner
							lc.AddLine (new Point (vts.X - 1, vts.Y - 1), 1, Orientation.Vertical, tab.BorderStyle);
							lc.AddLine (new Point (vts.X - 1, vts.Y - 1), 1, Orientation.Horizontal, tab.BorderStyle);
						} else {
							if (_host.Style.ShowTopLine) {
								// ULCorner
								lc.AddLine (new Point (vts.X - 1, vts.Y - 1), 1, Orientation.Vertical, tab.BorderStyle);
								lc.AddLine (new Point (vts.X - 1, vts.Y - 1), 1, Orientation.Horizontal, tab.BorderStyle);
							}
							// LLCorner
							lc.AddLine (new Point (vts.X - 1, vts.Bottom), -1, Orientation.Vertical, tab.BorderStyle);
							lc.AddLine (new Point (vts.X - 1, vts.Bottom), 1, Orientation.Horizontal, tab.BorderStyle);
						}
					} else if (i > 0) {
						if (_host.Style.ShowTopLine || _host.Style.TabsOnBottom) {
							// Upper left tee
							lc.AddLine (new Point (vts.X - 1, vts.Y - 1), 1, Orientation.Vertical, tab.BorderStyle);
							lc.AddLine (new Point (vts.X - 1, vts.Y - 1), 0, Orientation.Horizontal, tab.BorderStyle);
						}

						// Lower left tee
						lc.AddLine (new Point (vts.X - 1, vts.Bottom), -1, Orientation.Vertical, tab.BorderStyle);
						lc.AddLine (new Point (vts.X - 1, vts.Bottom), 0, Orientation.Horizontal, tab.BorderStyle);

					}
				} else if (i < tabLocations.Length - 1) {
					if (_host.Style.ShowTopLine) {
						// Upper right tee
						lc.AddLine (new Point (vts.Right, vts.Y - 1), 1, Orientation.Vertical, tab.BorderStyle);
						lc.AddLine (new Point (vts.Right, vts.Y - 1), 0, Orientation.Horizontal, tab.BorderStyle);
					}

					if (_host.Style.ShowTopLine || !_host.Style.TabsOnBottom) {
						// Lower right tee
						lc.AddLine (new Point (vts.Right, vts.Bottom), -1, Orientation.Vertical, tab.BorderStyle);
						lc.AddLine (new Point (vts.Right, vts.Bottom), 0, Orientation.Horizontal, tab.BorderStyle);
					} else {
						// Upper right tee
						lc.AddLine (new Point (vts.Right, vts.Y - 1), 1, Orientation.Vertical, tab.BorderStyle);
						lc.AddLine (new Point (vts.Right, vts.Y - 1), 0, Orientation.Horizontal, tab.BorderStyle);
					}
				}

				if (i == 0 && i != selectedTab && _host.TabScrollOffset == 0 && _host.Style.ShowBorder) {
					if (_host.Style.TabsOnBottom) {
						// Upper left vertical line
						lc.AddLine (new Point (vts.X - 1, vts.Y - 1), 0, Orientation.Vertical, tab.BorderStyle);
						lc.AddLine (new Point (vts.X - 1, vts.Y - 1), 1, Orientation.Horizontal, tab.BorderStyle);
					} else {
						// Lower left vertical line
						lc.AddLine (new Point (vts.X - 1, vts.Bottom), 0, Orientation.Vertical, tab.BorderStyle);
						lc.AddLine (new Point (vts.X - 1, vts.Bottom), 1, Orientation.Horizontal, tab.BorderStyle);
					}
				}

				if (i == tabLocations.Length - 1 && i != selectedTab) {
					if (_host.Style.TabsOnBottom) {
						// Upper right tee
						lc.AddLine (new Point (vts.Right, vts.Y - 1), 1, Orientation.Vertical, tab.BorderStyle);
						lc.AddLine (new Point (vts.Right, vts.Y - 1), 0, Orientation.Horizontal, tab.BorderStyle);
					} else {
						// Lower right tee
						lc.AddLine (new Point (vts.Right, vts.Bottom), -1, Orientation.Vertical, tab.BorderStyle);
						lc.AddLine (new Point (vts.Right, vts.Bottom), 0, Orientation.Horizontal, tab.BorderStyle);
					}
				}

				if (i == tabLocations.Length - 1) {
					var arrowOffset = 1;
					var lastSelectedTab = !_host.Style.ShowTopLine && i == selectedTab ? 1 : _host.Style.TabsOnBottom ? 1 : 0;
					var tabsBarVts = BoundsToScreen (Bounds);
					var lineLength = tabsBarVts.Right - vts.Right;
					// Right horizontal line
					if (ShouldDrawRightScrollIndicator ()) {
						if (lineLength - arrowOffset > 0) {
							if (_host.Style.TabsOnBottom) {
								lc.AddLine (new Point (vts.Right, vts.Y - lastSelectedTab), lineLength - arrowOffset, Orientation.Horizontal, tab.BorderStyle);
							} else {
								lc.AddLine (new Point (vts.Right, vts.Bottom - lastSelectedTab), lineLength - arrowOffset, Orientation.Horizontal, tab.BorderStyle);
							}
						}
					} else {
						if (_host.Style.TabsOnBottom) {
							lc.AddLine (new Point (vts.Right, vts.Y - lastSelectedTab), lineLength, Orientation.Horizontal, tab.BorderStyle);
						} else {
							lc.AddLine (new Point (vts.Right, vts.Bottom - lastSelectedTab), lineLength, Orientation.Horizontal, tab.BorderStyle);
						}
						if (_host.Style.ShowBorder) {
							if (_host.Style.TabsOnBottom) {
								// More LRCorner
								lc.AddLine (new Point (tabsBarVts.Right - 1, vts.Y - lastSelectedTab), -1, Orientation.Vertical, tab.BorderStyle);
							} else {
								// More URCorner
								lc.AddLine (new Point (tabsBarVts.Right - 1, vts.Bottom - lastSelectedTab), 1, Orientation.Vertical, tab.BorderStyle);
							}
						}
					}
				}

				tab.LineCanvas.Merge (lc);
				tab.OnRenderLineCanvas ();
			}
		}

		/// <summary>
		/// Renders the line with the tab names in it.
		/// </summary>
		private void RenderTabLine ()
		{
			var tabLocations = _host._tabLocations;
			int y;

			if (_host.Style.TabsOnBottom) {

				y = 1;
			} else {
				y = _host.Style.ShowTopLine ? 1 : 0;
			}

			View selected = null;
			var topLine = _host.Style.ShowTopLine ? 1 : 0;
			var width = Bounds.Width;

			foreach (var toRender in tabLocations) {
				var tab = toRender.Tab;

				if (toRender.IsSelected) {
					selected = tab;
					if (_host.Style.TabsOnBottom) {
						tab.Border.Thickness = new Thickness (1, 0, 1, topLine);
						tab.Margin.Thickness = new Thickness (0, 1, 0, 0);
					} else {
						tab.Border.Thickness = new Thickness (1, topLine, 1, 0);
						tab.Margin.Thickness = new Thickness (0, 0, 0, topLine);
					}
				} else if (selected == null) {
					if (_host.Style.TabsOnBottom) {
						tab.Border.Thickness = new Thickness (1, 1, 0, topLine);
						tab.Margin.Thickness = new Thickness (0, 0, 0, 0);
					} else {
						tab.Border.Thickness = new Thickness (1, topLine, 0, 1);
						tab.Margin.Thickness = new Thickness (0, 0, 0, 0);
					}
					tab.Width = Math.Max (tab.Width.Anchor (0) - 1, 1);
				} else {
					if (_host.Style.TabsOnBottom) {
						tab.Border.Thickness = new Thickness (0, 1, 1, topLine);
						tab.Margin.Thickness = new Thickness (0, 0, 0, 0);
					} else {
						tab.Border.Thickness = new Thickness (0, topLine, 1, 1);
						tab.Margin.Thickness = new Thickness (0, 0, 0, 0);
					}
					tab.Width = Math.Max (tab.Width.Anchor (0) - 1, 1);
				}

				tab.Text = toRender.TextToRender;

				LayoutSubviews ();

				tab.OnDrawAdornments ();

				var prevAttr = Driver.GetAttribute ();

				// if tab is the selected one and focus is inside this control
				if (toRender.IsSelected && _host.HasFocus) {

					if (_host.Focused == this) {

						// if focus is the tab bar ourself then show that they can switch tabs
						prevAttr = ColorScheme.HotFocus;
					} else {

						// Focus is inside the tab
						prevAttr = ColorScheme.HotNormal;
					}
				}
				tab.TextFormatter.Draw (tab.BoundsToScreen (tab.Bounds), prevAttr, ColorScheme.HotNormal);

				tab.OnRenderLineCanvas ();

				Driver.SetAttribute (GetNormalColor ());
			}
		}

		/// <summary>
		/// Renders the line of the tab that adjoins the content of the tab.
		/// </summary>
		private void RenderUnderline ()
		{
			int y = GetUnderlineYPosition ();

			var selected = _host._tabLocations.FirstOrDefault (t => t.IsSelected);

			if (selected == null) {
				return;
			}

			// draw scroll indicators

			// if there are more tabs to the left not visible
			if (_host.TabScrollOffset > 0) {
				_leftScrollIndicator.X = 0;
				_leftScrollIndicator.Y = y;

				// indicate that
				_leftScrollIndicator.Visible = true;
				// Ensures this is clicked instead of the first tab
				BringSubviewToFront (_leftScrollIndicator);
				_leftScrollIndicator.Draw ();
			} else {
				_leftScrollIndicator.Visible = false;
			}

			// if there are more tabs to the right not visible
			if (ShouldDrawRightScrollIndicator ()) {
				_rightScrollIndicator.X = Bounds.Width - 1;
				_rightScrollIndicator.Y = y;

				// indicate that
				_rightScrollIndicator.Visible = true;
				// Ensures this is clicked instead of the last tab if under this
				BringSubviewToFront (_rightScrollIndicator);
				_rightScrollIndicator.Draw ();
			} else {
				_rightScrollIndicator.Visible = false;
			}
		}

		private bool ShouldDrawRightScrollIndicator ()
		{
			return _host._tabLocations.LastOrDefault ()?.Tab != _host.Tabs.LastOrDefault ();
		}

		private int GetUnderlineYPosition ()
		{
			if (_host.Style.TabsOnBottom) {

				return 0;
			} else {

				return _host.Style.ShowTopLine ? 2 : 1;
			}
		}

		public override bool MouseEvent (MouseEvent me)
		{
			var hit = me.View is Tab ? (Tab)me.View : null;

			bool isClick = me.Flags.HasFlag (MouseFlags.Button1Clicked) ||
				me.Flags.HasFlag (MouseFlags.Button2Clicked) ||
				me.Flags.HasFlag (MouseFlags.Button3Clicked);

			if (isClick) {
				_host.OnTabClicked (new TabMouseEventArgs (hit, me));

				// user canceled click
				if (me.Handled) {
					return true;
				}
			}

			if (!me.Flags.HasFlag (MouseFlags.Button1Clicked) &&
			!me.Flags.HasFlag (MouseFlags.Button1DoubleClicked) &&
			!me.Flags.HasFlag (MouseFlags.Button1TripleClicked))
				return false;

			if (!HasFocus && CanFocus) {
				SetFocus ();
			}

			if (me.Flags.HasFlag (MouseFlags.Button1Clicked) ||
			me.Flags.HasFlag (MouseFlags.Button1DoubleClicked) ||
			me.Flags.HasFlag (MouseFlags.Button1TripleClicked)) {

				int scrollIndicatorHit = 0;
				if (me.View != null && me.View.Id == "rightScrollIndicator") {
					scrollIndicatorHit = 1;
				} else if (me.View != null && me.View.Id == "leftScrollIndicator") {
					scrollIndicatorHit = -1;
				}

				if (scrollIndicatorHit != 0) {

					_host.SwitchTabBy (scrollIndicatorHit);

					SetNeedsDisplay ();
					return true;
				}

				if (hit != null) {
					_host.SelectedTab = hit;
					SetNeedsDisplay ();
					return true;
				}
			}

			return false;
		}
	}

	/// <summary>
	/// Raises the <see cref="TabClicked"/> event.
	/// </summary>
	/// <param name="tabMouseEventArgs"></param>
	protected virtual private void OnTabClicked (TabMouseEventArgs tabMouseEventArgs)
	{
		TabClicked?.Invoke (this, tabMouseEventArgs);
	}
}
