//
// ComboBox.cs: ComboBox control
//
// Authors:
//   Ross Ferguson (ross.c.ferguson@btinternet.com)
//
// TODO:
//  LayoutComplete() resize Height implement
//	Cursor rolls of end of list when Height = Dim.Fill() and list fills frame
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	/// ComboBox control
	/// </summary>
	public class ComboBox : View {

		IListDataSource source;
		/// <summary>
		/// Gets or sets the <see cref="IListDataSource"/> backing this <see cref="ComboBox"/>, enabling custom rendering.
		/// </summary>
		/// <value>The source.</value>
		/// <remarks>
		///  Use <see cref="SetSource"/> to set a new <see cref="IList"/> source.
		/// </remarks>
		public IListDataSource Source {
			get => source;
			set {
				source = value;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Sets the source of the <see cref="ComboBox"/> to an <see cref="IList"/>.
		/// </summary>
		/// <value>An object implementing the IList interface.</value>
		/// <remarks>
		///  Use the <see cref="Source"/> property to set a new <see cref="IListDataSource"/> source and use custome rendering.
		/// </remarks>
		public void SetSource (IList source)
		{
			if (source == null)
				Source = null;
			else {
				Source = MakeWrapper (source);
			}
		}

		/// <summary>
		///   Changed event, raised when the selection has been confirmed.
		/// </summary>
		/// <remarks>
		///   Client code can hook up to this event, it is
		///   raised when the selection has been confirmed.
		/// </remarks>
		public event EventHandler<ustring> SelectedItemChanged;

		IList searchset;
		ustring text = "";
		readonly TextField search;
		readonly ListView listview;
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
		/// <param name="rect"></param>
		/// <param name="source"></param>
		public ComboBox (Rect rect, IList source) : base (rect)
		{
			SetSource (source);
			this.height = rect.Height;
			this.width = rect.Width;

			search = new TextField ("") { Width = width };
			listview = new ListView (rect, source) { LayoutStyle = LayoutStyle.Computed };

			Initialize ();
		}

		static IListDataSource MakeWrapper (IList source)
		{
			return new ListWrapper (source);
		}

		private void Initialize()
		{
			ColorScheme = Colors.Base;

			search.TextChanged += Search_Changed;

			// On resize
			LayoutComplete += (LayoutEventArgs a) => {

				search.Width = Bounds.Width;
				listview.Width = autoHide ? Bounds.Width - 1 : Bounds.Width;
			};

			listview.SelectedItemChanged += (ListViewItemEventArgs e) => {

				if(searchset.Count > 0)
					SetValue ((string)searchset [listview.SelectedItem]);
			};

			Application.Loaded += (Application.ResizedEventArgs a) => {
				// Determine if this view is hosted inside a dialog
				for (View view = this.SuperView; view != null; view = view.SuperView) {
					if (view is Dialog) {
						autoHide = false;
						break;
					}
				}

				ResetSearchSet ();

				ColorScheme = autoHide ? Colors.Base : ColorScheme = null;

				// Needs to be re-applied for LayoutStyle.Computed
				// If Dim or Pos are null, these are the from the parametrized constructor
				listview.Y = 1;

				if (Width == null) {
					listview.Width = CalculateWidth ();
					search.Width = width;
				} else {
					width = GetDimAsInt (Width, vertical: false);
					search.Width = width;
					listview.Width = CalculateWidth ();
				}

				if (Height == null) {
					var h = CalculatetHeight ();
					listview.Height = h;
					this.Height = h + 1; // adjust view to account for search box
				} else {
					if (height == 0)
						height = GetDimAsInt (Height, vertical: true);

					listview.Height = CalculatetHeight ();
					this.Height = height + 1; // adjust view to account for search box
				}

				if (this.Text != null)
					Search_Changed (Text);

				if (autoHide)
					listview.ColorScheme = Colors.Menu;
				else
					search.ColorScheme = Colors.Menu;
			};

			search.MouseClick += Search_MouseClick;

			this.Add(listview, search);
			this.SetFocus(search);
		}

		private void Search_MouseClick (MouseEventArgs e)
		{
			if (e.MouseEvent.Flags != MouseFlags.Button1Clicked)
				return;

			SuperView.SetFocus (search);
		}

		///<inheritdoc/>
		public override bool OnEnter ()
		{
			if (!search.HasFocus)
				this.SetFocus (search);

			search.CursorPosition = search.Text.Length;

			return true;
		}

		/// <summary>
		/// Invokes the SelectedChanged event if it is defined.
		/// </summary>
		/// <returns></returns>
		public virtual bool OnSelectedChanged ()
		{
			// Note: Cannot rely on "listview.SelectedItem != lastSelectedItem" because the list is dynamic. 
			// So we cannot optimize. Ie: Don't call if not changed
			SelectedItemChanged?.Invoke (this, search.Text);

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

				SetValue((string)searchset [listview.SelectedItem]);
				search.CursorPosition = search.Text.Length;
				Search_Changed (search.Text);
				OnSelectedChanged ();

				searchset.Clear();
				listview.Clear ();
				listview.Height = 0;
				this.SetFocus(search);

				return true;
			}

			if (e.Key == Key.CursorDown && search.HasFocus && listview.SelectedItem == 0 && searchset.Count > 0) { // jump to list
				this.SetFocus (listview);
				SetValue ((string)searchset [listview.SelectedItem]);
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
				OnSelectedChanged ();
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
			search.TextChanged -= Search_Changed;
			this.text = search.Text = text;
			search.CursorPosition = 0;
			search.TextChanged += Search_Changed;
		}

		/// <summary>
		/// Reset to full original list
		/// </summary>
		private void Reset()
		{
			search.Text = text = "";
			OnSelectedChanged();

			ResetSearchSet ();

			listview.SetSource(searchset);
			listview.Height = CalculatetHeight ();

			this.SetFocus(search);
		}

		private void ResetSearchSet()
		{
			if (autoHide) {
				if (searchset == null)
					searchset = new List<string> ();
				else
					searchset.Clear ();
			} else
				searchset = source.ToList ();
		}

		private void Search_Changed (ustring text)
		{
			if (source == null) // Object initialization
				return;

			if (string.IsNullOrEmpty (search.Text.ToString ()))
				ResetSearchSet ();
			else
				searchset = source.ToList().Cast<string>().Where (x => x.StartsWith (search.Text.ToString (), StringComparison.CurrentCultureIgnoreCase)).ToList();

			listview.SetSource (searchset);
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
		/// Get Dim as integer value
		/// </summary>
		/// <param name="dim"></param>
		/// <param name="vertical"></param>
		/// <returns></returns>n
		private int GetDimAsInt (Dim dim, bool vertical)
		{
			if (dim is Dim.DimAbsolute)
				return dim.Anchor (0);
			else { // Dim.Fill Dim.Factor
				if(autoHide)
					return vertical ? dim.Anchor (SuperView.Bounds.Height) : dim.Anchor (SuperView.Bounds.Width);
				else 
					return vertical ? dim.Anchor (Bounds.Height) : dim.Anchor (Bounds.Width);
			}
		}
	}
}
