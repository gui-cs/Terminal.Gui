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
		public event EventHandler<ustring> Changed;

		readonly IList<string> listsource;
		IList<string> searchset;
		readonly TextField search;
		readonly ListView listview;
		readonly int height;

		/// <summary>
		/// Public constructor
		/// </summary>
		/// <param name="x">The x coordinate</param>
		/// <param name="y">The y coordinate</param>
		/// <param name="w">The width</param>		
		/// <param name="h">The height</param>
		/// <param name="source">Auto completetion source</param>
		public TextFieldAutoComplete(int x, int y, int w, int h, IList<string> source) : base()
		{
			listsource = searchset = source;
			height = h;
			search = new TextField(x, y, w, "");
			search.Changed += Search_Changed;

			listview = new ListView(new Rect(x, y + 1, w, Math.Min(height, searchset.Count())), listsource.ToList())
			{ 
				//LayoutStyle = LayoutStyle.Computed,
			};
			
			this.Add(listview);
			this.Add(search);
			this.SetFocus(search);

			this.OnEnter += (object sender, EventArgs e) => { this.SetFocus(search); };
		}

		public override bool ProcessKey(KeyEvent e)
		{
			if (e.Key == Key.Tab)
			{
				base.ProcessKey(e);
				return false; // allow tab-out to next control
			}
		
			if (e.Key == Key.Enter)
			{
				if (Text == null)
					return false; // allow tab-out to next control

				search.Text = Text;
				search.CursorPosition = search.Text.Length;
				searchset.Clear();
				listview.Clear();
				this.SetFocus(search);

				Changed?.Invoke(this, search.Text);

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

			// Unix emulation
			if (e.Key == Key.ControlU || e.Key == Key.Esc)
			{
				Reset();
				return true;
			}

			return base.ProcessKey(e);
		}

		/// <summary>
		/// The currenlty selected list item
		/// </summary>
		public string Text
		{
			get
			{
				if (listview.Source.Count == 0 || searchset.Count() == 0)
					return search.Text.ToString();

				return searchset.ToList()[listview.SelectedItem] as string;				
			}
		}

		/// <summary>
		/// Reset to full original list
		/// </summary>
		private void Reset()
		{
			search.Text = "";
			searchset = listsource;
			listview.SetSource(searchset.ToList());
			this.SetFocus(search);
		}

		private void Search_Changed(object sender, ustring e)
		{
			
		   if (string.IsNullOrEmpty(search.Text.ToString()))
				searchset = listsource;
			else
				searchset = listsource.Where(x => x.ToLower().Contains(search.Text.ToString().ToLower())).ToList();

			listview.SetSource(searchset.ToList());
			listview.Height = Math.Min(height,  searchset.Count());
		}
	}
}
