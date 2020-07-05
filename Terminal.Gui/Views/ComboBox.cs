//
// ComboBox.cs: ComboBox control
//
// Authors:
//   Ross Ferguson (ross.c.ferguson@btinternet.com)
//

using System;
using System.Collections;
using System.Collections.Generic;
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

				// Only need to refresh list if its been added to a container view
				if(SuperView != null && SuperView.Subviews.Contains(this)) { 
					Search_Changed ("");
					SetNeedsDisplay ();
				}
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
			if (source == null) {
				Source = null;
			} else {
				listview.SetSource (source);
				Source = listview.Source;
			}
		}

		/// <summary>
		/// This event is raised when the selected item in the <see cref="ComboBox"/> has changed.
		/// </summary>
		public Action<ListViewItemEventArgs> SelectedItemChanged;

		/// <summary>
		/// This event is raised when the user Double Clicks on an item or presses ENTER to open the selected item.
		/// </summary>
		public Action<ListViewItemEventArgs> OpenSelectedItem;

		IList searchset;
		ustring text = "";
		readonly TextField search;
		readonly ListView listview;
		bool autoHide = true;

		/// <summary>
		/// Public constructor
		/// </summary>
		public ComboBox () : base ()
		{
			search = new TextField ("");
			listview = new ListView () { LayoutStyle = LayoutStyle.Computed, CanFocus = true };

			Initialize ();
		}

		/// <summary>
		/// Public constructor
		/// </summary>
		/// <param name="text"></param>
		public ComboBox (ustring text) : base ()
		{
			search = new TextField ("");
			listview = new ListView () { LayoutStyle = LayoutStyle.Computed, CanFocus = true };
						
			Initialize ();
			Text = text;
		}

		/// <summary>
		/// Public constructor
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="source"></param>
		public ComboBox (Rect rect, IList source) : base (rect)
		{
			search = new TextField ("") { Width = rect.Width };
			listview = new ListView (rect, source) { LayoutStyle = LayoutStyle.Computed, ColorScheme = Colors.Base };

			Initialize ();
			SetSource (source);
		}

		private void Initialize ()
		{
			search.TextChanged += Search_Changed;
			search.MouseClick += Search_MouseClick;

			listview.Y = Pos.Bottom (search);
			listview.OpenSelectedItem += (ListViewItemEventArgs a) => Selected ();

			this.Add (listview, search);
			this.SetFocus (search);

			// On resize
			LayoutComplete += (LayoutEventArgs a) => {
				if (!autoHide && search.Frame.Width != Bounds.Width ||
					autoHide && search.Frame.Width != Bounds.Width - 1) {
					search.Width = Bounds.Width;
					listview.Width = listview.Width = autoHide ? Bounds.Width - 1 : Bounds.Width;
					listview.Height = CalculatetHeight ();
					search.SetRelativeLayout (Bounds);
					listview.SetRelativeLayout (Bounds);
				}
			};

			listview.SelectedItemChanged += (ListViewItemEventArgs e) => {

				if (searchset.Count > 0) {
					SetValue (searchset [listview.SelectedItem]);
				}
			};

			Added += (View v) => {

				// Determine if this view is hosted inside a dialog
				for (View view = this.SuperView; view != null; view = view.SuperView) {
					if (view is Dialog) {
						autoHide = false;
						break;
					}
				}

				SetNeedsLayout ();
				SetNeedsDisplay ();
				Search_Changed (Text);
			};
		}

		/// <summary>
		/// Gets the index of the currently selected item in the <see cref="Source"/>
		/// </summary>
		/// <value>The selected item or -1 none selected.</value>
		public int SelectedItem { private set; get; }

		bool isShow = false;

		///<inheritdoc/>
		public new ColorScheme ColorScheme {
			get {
				return base.ColorScheme;
			}
			set {
				listview.ColorScheme = value;			
				base.ColorScheme = value;
				SetNeedsDisplay ();
			}
		}

		private void Search_MouseClick (MouseEventArgs me)
		{
			if (me.MouseEvent.X == Bounds.Right - 1 && me.MouseEvent.Y == Bounds.Top && me.MouseEvent.Flags == MouseFlags.Button1Pressed
			&& search.Text == "" && autoHide) {

				if (isShow) {
					HideList ();
					isShow = false;
				} else {
					// force deep copy
					foreach (var item in Source.ToList()) { 
						searchset.Add (item);
					}

					ShowList ();
					isShow = true;
				}
			} else { 
				SuperView.SetFocus (search);
			}
		}

		///<inheritdoc/>
		public override bool OnEnter ()
		{
			if (!search.HasFocus) {
				this.SetFocus (search);
			}

			search.CursorPosition = search.Text.RuneCount;

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
			SelectedItemChanged?.Invoke (new ListViewItemEventArgs(SelectedItem, search.Text));

			return true;
		}

		/// <summary>
		/// Invokes the OnOpenSelectedItem event if it is defined.
		/// </summary>
		/// <returns></returns>
		public virtual bool OnOpenSelectedItem ()
		{
			var value = search.Text;
			OpenSelectedItem?.Invoke (new ListViewItemEventArgs (SelectedItem, value));

			return true;
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			base.Redraw (bounds);

			if (!autoHide) {
				return;
			}

			Move (Bounds.Right - 1, 0);
			Driver.AddRune (Driver.DownArrow);
		}

		///<inheritdoc/>
		public override bool ProcessKey (KeyEvent e)
		{
			if (e.Key == Key.Tab) {
				base.ProcessKey (e);
				return false; // allow tab-out to next control
			}

			if (e.Key == Key.Enter && listview.HasFocus) {
				Selected ();
				return true;
			}

			if (e.Key == Key.CursorDown && search.HasFocus && searchset.Count > 0) { // jump to list
				this.SetFocus (listview);
				SetValue (searchset [listview.SelectedItem]);
				return true;
			}

			if (e.Key == Key.CursorUp && search.HasFocus) { // stop odd behavior on KeyUp when search has focus
				return true;
			}

			if (e.Key == Key.CursorUp && listview.HasFocus && listview.SelectedItem == 0 && searchset.Count > 0) // jump back to search
			{
				search.CursorPosition = search.Text.RuneCount;
				this.SetFocus (search);
				return true;
			}

			if(e.Key == Key.PageDown) { 
				listview.MovePageDown ();
				return true;
			}

			if (e.Key == Key.PageUp) { 
				listview.MovePageUp ();
				return true;
			}

			if (e.Key == Key.Esc) {
				this.SetFocus (search);
				search.Text = text = "";
				OnSelectedChanged ();
				return true;
			}

			// Unix emulation
			if (e.Key == Key.ControlU) {
				Reset ();
				return true;
			}

			return base.ProcessKey (e);
		}

		/// <summary>
		/// The currently selected list item
		/// </summary>
		public new ustring Text {
			get {
				return text;
			}
			set {
				search.Text = text = value;
			}
		}

		private void SetValue (object text)
		{
			search.TextChanged -= Search_Changed;
			this.text = search.Text = text.ToString();
			search.CursorPosition = 0;
			search.TextChanged += Search_Changed;
			SelectedItem = GetSelectedItemFromSource (this.text);
			OnSelectedChanged ();
		}

		private void Selected ()
		{
			if (listview.Source.Count == 0 || searchset.Count == 0) {
				text = "";
				return;
			}

			SetValue (searchset [listview.SelectedItem]);
			search.CursorPosition = search.Text.RuneCount;
			Search_Changed (search.Text);
			OnOpenSelectedItem ();
			Reset (keepSearchText: true);
		}

		private int GetSelectedItemFromSource (ustring value)
		{
			if (source == null) {
				return -1;
			}
			for (int i = 0; i < source.Count; i++) {
				if (source.ToList () [i].ToString () == value) {
					return i;
				}
			}
			return -1;
		}

		/// <summary>
		/// Reset to full original list
		/// </summary>
		private void Reset (bool keepSearchText = false)
		{
			if (!keepSearchText) {
				search.Text = text = "";
			}

			ResetSearchSet ();

			listview.SetSource (searchset);
			listview.Height = CalculatetHeight ();

			this.SetFocus (search);
		}

		private void ResetSearchSet (bool noCopy = false)
		{
			if (searchset == null) {
				searchset = new List<object> ();
			} else { 
				searchset.Clear ();
			}

			if (autoHide || noCopy)
				return;

			// force deep copy
			foreach (var item in Source.ToList ()) {
				searchset.Add (item);
			}
		}

		private void Search_Changed (ustring text)
		{
			if (source == null) { // Object initialization		
				return;
			}

			if (ustring.IsNullOrEmpty (search.Text)) {
				ResetSearchSet ();
			} else {
				ResetSearchSet (noCopy: true);

				foreach (var item in source.ToList ()) { // Iterate to preserver object type and force deep copy
					if (item.ToString().StartsWith (search.Text.ToString(), StringComparison.CurrentCultureIgnoreCase)) { 
						searchset.Add (item);
					}
				}
			}

			ShowList ();
		}

		/// <summary>
		/// Show the search list
		/// </summary>
		/// 
		/// Consider making public
		private void ShowList ()
		{
			listview.SetSource (searchset);
			listview.Clear (); // Ensure list shrinks in Dialog as you type
			listview.Height = CalculatetHeight ();
			this.SuperView?.BringSubviewToFront (this);
		}

		/// <summary>
		/// Hide the search list
		/// </summary>
		/// 
		/// Consider making public
		private void HideList ()
		{
			Reset ();
		}

		/// <summary>
		/// Internal height of dynamic search list
		/// </summary>
		/// <returns></returns>
		private int CalculatetHeight ()
		{
			if (Bounds.Height == 0)
				return 0;

			return Math.Min (Bounds.Height - 1, searchset?.Count ?? 0);
		}
	}
}
