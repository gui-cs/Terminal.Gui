//
// TextFieldAutoComplete.cs: TextField with AutoComplete
//
// Author:
//   Ross Ferguson (ross.c.ferguson@btinternet.com)
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
		readonly bool isAutoHide;

		/// <summary>
		/// Public constructor
		/// </summary>
		/// <param name="x">The x coordinate</param>
		/// <param name="y">The y coordinate</param>
		/// <param name="w">The width</param>		
		/// <param name="h">The height</param>
		/// <param name="source">Auto completetion source</param>
		/// <param name="autoHide">Completetion list hidden until start of typing. Use when hosting in Window as opposed to a Dialog</param>
		public TextFieldAutoComplete(int x, int y, int w, int h, IList<string> source, bool autoHide = false)
		{
			listsource = new List<string>(source);
			isAutoHide = autoHide;
			searchset = isAutoHide ? new List<string> () : listsource;
			height = h;
			width = w;
			isAutoHide = autoHide;
			search = new TextField(x, y, w, "");
			search.Changed += Search_Changed;

			listview = new ListView(new Rect(x, y + 1, w, CalculatetHeight()), listsource.ToList())
			{
				LayoutStyle = LayoutStyle.Computed,
				ColorScheme = Colors.Dialog
			};
			listview.SelectedChanged += () => {
				SetValue (searchset [listview.SelectedItem]);
			};


			// Needs to be re-applied for LayoutStyle.Computed
			listview.X = x;
			listview.Y = y + 1;
			listview.Width = w;
			listview.Height = CalculatetHeight ();

			this.Add(listview);
			this.Add(search);
			this.SetFocus(search);
		}

		public override bool OnEnter ()
		{
			if (!search.HasFocus)
				this.SetFocus (search);

			search.CursorPosition = search.Text.Length;

			return true;
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

				SetValue( searchset [listview.SelectedItem]);
				search.CursorPosition = search.Text.Length;
				Changed?.Invoke (this, text);

				searchset.Clear();
				listview.SetSource(new List<string> ());
				listview.Height = 0;
				this.SetFocus(search);

				return true;
			}

			if (e.Key == Key.CursorDown && search.HasFocus && listview.SelectedItem == 0) { // jump to list
				this.SetFocus (listview);
				SetValue (searchset [listview.SelectedItem]);
				return true;
			}

			if (e.Key == Key.CursorUp && listview.HasFocus && listview.SelectedItem == 0) // jump back to search
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
			searchset = isAutoHide ? new List<string> () : listsource;

			listview.SetSource(searchset.ToList());
			listview.Height = CalculatetHeight ();

			this.SetFocus(search);
		}

		private void Search_Changed (object sender, ustring text)
		{
			if (string.IsNullOrEmpty (search.Text.ToString())) {
				searchset = isAutoHide ? new List<string> () : listsource;
			} 
			else
				searchset = listsource.Where (x => x.StartsWith (search.Text.ToString (), StringComparison.CurrentCultureIgnoreCase)).ToList ();

			listview.SetSource (searchset.ToList ());
			listview.Height = CalculatetHeight ();
			listview.Redraw (new Rect (0, 0, width, height)); // for any view behind this
		}

		/// <summary>
		/// Internal height of dynamic search list
		/// </summary>
		/// <returns></returns>
		private int CalculatetHeight ()
		{
			return Math.Min (height, searchset.Count);
		}
	}
}
