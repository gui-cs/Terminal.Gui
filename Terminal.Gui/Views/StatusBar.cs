using System;
using NStack;

namespace Terminal.Gui
{
	public class StatusItem 
	{
		public StatusItem(Key shortCut, ustring title, Action action) 
		{
			Title = title ?? "";
			ShortCut = shortCut;
			Action = action;
		}

		/// <summary>
		/// This is the global setting that can be used as a global shortcut to invoke the action on the menu.
		/// </summary>
		public Key ShortCut;

		/// <summary>
		/// Gets or sets the title.
		/// </summary>
		/// <value>The title.</value>
		public ustring Title { get; set; }

		/// <summary>
		/// Gets or sets the action to be invoked when the menu is triggered
		/// </summary>
		/// <value>Method to invoke.</value>
		public Action Action { get; set; }
	};

	public class StatusBar : View
	{
		public StatusItem [] Items { get; set; }

		public StatusBar(StatusItem [] items) : base()
		{
			X = 0;
			Y = Application.Driver.Rows-1; // TODO: using internals of Application
			Width = Dim.Fill ();
			Height = 1;
			Items = items;
			CanFocus = false;
			ColorScheme = Colors.Menu;
		}

		Attribute ToggleScheme(Attribute scheme) 
		{
			var result = scheme==ColorScheme.Normal ? ColorScheme.HotNormal : ColorScheme.Normal;
			Driver.SetAttribute(result);
			return result;
		} 

		public override void Redraw(Rect region) {
			Move (0, 0);
			Driver.SetAttribute (ColorScheme.Normal);
			for (int i = 0; i < Frame.Width; i++)
				Driver.AddRune (' ');

			Move (1, 0);
			var scheme = ColorScheme.Normal;
			Driver.SetAttribute(scheme);
			for(int i=0; i<Items.Length; i++) {
				var title = Items[i].Title;
				for(int n=0; n<title.Length; n++) {
					if(title[n]=='~') {
						scheme = ToggleScheme(scheme);
						continue;
					}
					Driver.AddRune(title[n]);
				}
				Driver.AddRune (' ');
			}
		}

		public override bool ProcessHotKey (KeyEvent kb)
		{
			foreach(var item in Items) {
				if(kb.Key==item.ShortCut) {
					if( item.Action!=null ) item.Action();
					return true;
				}
			}
			return false;
		}
	}
}