//
// TextFieldAutoComplete.cs: TextField with AutoComplete
//
// Author:
//   Ross Ferguson (ross.c.ferguson@btinternet.com)
//
// TODO:
//   * Completion list auto appears when hosted directly in a Window as opposed to a dialog
//

using System;
using System.Linq;
using System.Collections.Generic;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	/// TextField with AutoComplete
	/// </summary>
	public class TextFieldAutoComplete : View {
		/// <summary>
		///   Changed event, raised when the selection has been confirmed.
		/// </summary>
		/// <remarks>
		///   Client code can hook up to this event, it is
		///   raised when the selection has been confirmed.
		/// </remarks>
		public event EventHandler<ustring> Changed;

		readonly IList<string> listsource;
		IList<string> searchset;
		ustring text = "";
		readonly TextField search;
		readonly ListView listview;
		readonly int height;
		readonly int width;

		/// <summary>
		/// Public constructor
		/// </summary>
		/// <param name="x">The x coordinate</param>
		/// <param name="y">The y coordinate</param>
		/// <param name="w">The width</param>		
		/// <param name="h">The height</param>
		/// <param name="source">Auto completetion source</param>
		public TextFieldAutoComplete(int x, int y, int w, int h, IList<string> source)
		{
			listsource = searchset = source;
			height = h;
			width = w;
			search = new TextField(x, y, w, "");
			search.Changed += Search_Changed;

			listview = new ListView(new Rect(x, y + 1, w, CalculatetHeight()), listsource.ToList())
			{
				LayoutStyle = LayoutStyle.Computed,
				ColorScheme = Colors.Dialog
			};

			// Needs to be re-applied for LayoutStyle.Computed
			listview.X = x;
			listview.Y = y + 1;
			listview.Width = w;
			listview.Height = CalculatetHeight ();

			this.Add(listview);
			this.Add(search);
			this.SetFocus(search);

			this.OnEnter += (object sender, EventArgs e) => {
				this.SetFocus(search);
				search.CursorPosition = search.Text.Length;
			};
		}

		public override bool ProcessKey(KeyEvent e)
		{
			if (e.Key == Key.Tab)
			{
				base.ProcessKey(e);
				return false; // allow tab-out to next control
			}

			if (e.Key == Key.Enter && listview.HasFocus) {

				if (listview.Source.Count == 0 || searchset.Count == 0) {
					text = "";
					return true;
				}

				search.Text = text =  searchset [listview.SelectedItem];
				search.CursorPosition = search.Text.Length;
				Changed?.Invoke (this, text);

				searchset.Clear();
				listview.Clear();
				this.SetFocus(search);

				return true;
			}

			if (e.Key == Key.CursorDown && search.HasFocus) // jump to list
			{
				this.SetFocus(listview);
				listview.SelectedItem = 0;
				return true;
			}

			if(e.Key == Key.CursorUp && listview.SelectedItem == 0 && listview.HasFocus) // jump back to search
			{
				this.SetFocus(search);
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
		/// The currenlty selected list item
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

		/// <summary>
		/// Reset to full original list
		/// </summary>
		private void Reset()
		{
			search.Text = text = "";
			Changed?.Invoke (this, search.Text);
			searchset = listsource;

			listview.SetSource(searchset.ToList());
			listview.Height = CalculatetHeight ();
			listview.Redraw (new Rect (0, 0, width, height));

			this.SetFocus(search);
		}

		private void Search_Changed (object sender, ustring text)
		{
			// Cannot use text argument as its old value (pre-change)
			if (string.IsNullOrEmpty (search.Text.ToString())) {
				searchset = listsource;
			} 
			else
				searchset = listsource.Where (x => x.StartsWith (search.Text.ToString (), StringComparison.CurrentCultureIgnoreCase)).ToList ();

			listview.SetSource (searchset.ToList ());
			listview.Height = CalculatetHeight ();
			listview.Redraw (new Rect (0, 0, width, height));
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
		/// Determine if this view is hosted inside a dialog
		/// </summary>
		/// <returns></returns>
		private bool IsDialogHosted()
		{
			for (View v = this.SuperView; v != null; v = v.SuperView) {

				if (v.GetType () == typeof (Dialog))
					return true;
			}
			return false;
		}
	}
}
