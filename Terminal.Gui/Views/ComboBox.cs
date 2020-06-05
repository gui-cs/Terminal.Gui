//
// ComboBox.cs: ComboBox control
//
// Authors:
//   Ross Ferguson (ross.c.ferguson@btinternet.com)
//

using System;
using System.Linq;
using System.Collections.Generic;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	/// ComboBox control
	/// </summary>
	public class ComboBox : View {
		/// <summary>
		///   Changed event, raised when the selection has been confirmed.
		/// </summary>
		/// <remarks>
		///   Client code can hook up to this event, it is
		///   raised when the selection has been confirmed.
		/// </remarks>
		public Action<ustring> SelectedItemChanged;

		IList<string> listsource;
		IList<string> searchset;
		ustring text = "";
		TextField search;
		ListView listview;
		int x;
		int y;
		int height;
		int width;
		bool autoHide = true;

		/// <summary>
		/// Public constructor
		/// </summary>
		public ComboBox () : base()
		{
			search = new TextField ("");
			listview = new ListView () { LayoutStyle = LayoutStyle.Computed, CanFocus = true };

			Initialize ();
		}

		/// <summary>
		/// Public constructor
		/// </summary>
		/// <param name="x">The x coordinate</param>
		/// <param name="y">The y coordinate</param>
		/// <param name="w">The width</param>
		/// <param name="h">The height</param>
		/// <param name="source">Auto completion source</param>
		public ComboBox (int x, int y, int w, int h, IList<string> source)
		{
			SetSource (source);
			this.x = x;
			this.y = y;
			height = h;
			width = w;

			search = new TextField ("") { X = x, Y = y, Width = w };

			listview = new ListView (new Rect (x, y + 1, w, 0), listsource.ToList ()) {
				LayoutStyle = LayoutStyle.Computed,
			};

			Initialize ();
		}

		private void Initialize()
		{
			search.Changed += Search_Changed;

			listview.SelectedChanged += (object sender, ListViewItemEventArgs e) => {
				if(searchset.Count > 0)
					SetValue (searchset [listview.SelectedItem]);
			};

			// TODO: LayoutComplete event breaks cursor up/down. Revert to Application.Loaded 
			Application.Loaded += (object sender, Application.ResizedEventArgs a) => {
				// Determine if this view is hosted inside a dialog
				for (View view = this.SuperView; view != null; view = view.SuperView) {
					if (view is Dialog) {
						autoHide = false;
						break;
					}
				}

				searchset = autoHide ? new List<string> () : listsource;

				// Needs to be re-applied for LayoutStyle.Computed
				// If Dim or Pos are null, these are the from the parametrized constructor
				if (X == null) 
					listview.X = x;

				if (Y == null)
					listview.Y = y + 1;
				else
					listview.Y = Pos.Bottom (search);

				if (Width == null) {
					listview.Width = CalculateWidth ();
					search.Width = width;
				} else {
					width = GetDimAsInt (Width, a, vertical: false);
					search.Width = width;
					listview.Width = CalculateWidth ();
				}

				if (Height == null) {
					var h = CalculatetHeight ();
					listview.Height = h;
					this.Height = h + 1; // adjust view to account for search box
				} else {
					if (height == 0)
						height = GetDimAsInt (Height, a, vertical: true);

					listview.Height = CalculatetHeight ();
					this.Height = height + 1; // adjust view to account for search box
				}

				if (this.Text != null)
					Search_Changed (search, Text);

				if (autoHide)
					listview.ColorScheme = Colors.Menu;
				else
					search.ColorScheme = Colors.Menu;
			};

			search.MouseClick += Search_MouseClick;

			this.Add(listview, search);
			this.SetFocus(search);
		}

		/// <summary>
		/// Set search list source
		/// </summary>
		public void SetSource(IList<string> source)
		{
			listsource = new List<string> (source);
		}

		private void Search_MouseClick (object sender, MouseEventArgs e)
		{
			if (e.MouseEvent.Flags != MouseFlags.Button1Clicked)
				return;

			SuperView.SetFocus ((View)sender);
		}

		///<inheritdoc/>
		public override bool OnEnter ()
		{
			if (!search.HasFocus)
				this.SetFocus (search);

			search.CursorPosition = search.Text.Length;

			return true;
		}

		///<inheritdoc/>
		public override bool ProcessKey(KeyEvent e)
		{
			if (e.Key == Key.Tab) {
				base.ProcessKey(e);
				return false; // allow tab-out to next control
			}

			if (e.Key == Key.Enter && listview.HasFocus) {
				if (listview.Source.Count == 0 || searchset.Count == 0) {
					text = "";
					return true;
				}

				SetValue( searchset [listview.SelectedItem]);
				search.CursorPosition = search.Text.Length;
				Search_Changed (search, search.Text);
				Changed?.Invoke (this, text);

				searchset.Clear();
				listview.Clear ();
				listview.Height = 0;
				this.SetFocus(search);

				return true;
			}

			if (e.Key == Key.CursorDown && search.HasFocus && listview.SelectedItem == 0 && searchset.Count > 0) { // jump to list
				this.SetFocus (listview);
				SetValue (searchset [listview.SelectedItem]);
				return true;
			}

			if (e.Key == Key.CursorUp && search.HasFocus) // stop odd behavior on KeyUp when search has focus
				return true;

			if (e.Key == Key.CursorUp && listview.HasFocus && listview.SelectedItem == 0 && searchset.Count > 0) // jump back to search
			{
				search.CursorPosition = search.Text.Length;
				this.SetFocus (search);
				return true;
			}

			if (e.Key == Key.Esc) {
				this.SetFocus (search);
				search.Text = text = "";
				Changed?.Invoke (this, search.Text);
				return true;
			}

			// Unix emulation
			if (e.Key == Key.ControlU)
			{
				Reset();
				return true;
			}

			return base.ProcessKey(e);
		}

		/// <summary>
		/// The currently selected list item
		/// </summary>
		public ustring Text
		{
			get
			{
				return text;
			}
			set {
				search.Text = text = value;
			}
		}

		private void SetValue(ustring text)
		{
			search.Changed -= Search_Changed;
			this.text = search.Text = text;
			search.CursorPosition = 0;
			search.Changed += Search_Changed;
		}

		/// <summary>
		/// Reset to full original list
		/// </summary>
		private void Reset()
		{
			search.Text = text = "";
			Changed?.Invoke (this, search.Text);
			searchset = autoHide ? new List<string> () : listsource;

			listview.SetSource(searchset.ToList());
			listview.Height = CalculatetHeight ();

			this.SetFocus(search);
		}

		private void Search_Changed (object sender, ustring text)
		{
			if (listsource == null) // Object initialization
				return;

			if (string.IsNullOrEmpty (search.Text.ToString()))
				searchset = autoHide ? new List<string> () : listsource;
			else
				searchset = listsource.Where (x => x.StartsWith (search.Text.ToString (), StringComparison.CurrentCultureIgnoreCase)).ToList ();

			listview.SetSource (searchset.ToList ());
			listview.Height = CalculatetHeight ();

			listview.Redraw (new Rect (0, 0, width, height)); // for any view behind this
			this.SuperView?.BringSubviewToFront (this);
		}

		/// <summary>
		/// Internal height of dynamic search list
		/// </summary>
		/// <returns></returns>
		private int CalculatetHeight ()
		{
			return Math.Min (height, searchset.Count);
		}

		/// <summary>
		/// Internal width of search list
		/// </summary>
		/// <returns></returns>
		private int CalculateWidth ()
		{
			return autoHide ? Math.Max (1, width - 1) : width;
		}

		/// <summary>
		/// Get DimAbsolute as integer value
		/// </summary>
		/// <param name="dim"></param>
		/// <param name="a"></param>
		/// <param name="vertical"></param>
		/// <returns></returns>
		private int GetDimAsInt (Dim dim, Application.ResizedEventArgs a, bool vertical)
		{
			if (dim is Dim.DimAbsolute)
				return dim.Anchor (0);

			if (dim is Dim.DimFill || dim is Dim.DimFactor)
				return vertical ? dim.Anchor (a.Rows) : dim.Anchor (a.Cols);

			return 0;
		}
	}
}
