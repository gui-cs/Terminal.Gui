using System;
using System.IO;
using System.Linq;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace UICatalog.Scenarios {

	[ScenarioMetadata (Name: "Search Collection Nav", Description: "Demonstrates & tests SearchCollectionNavigator.")]
	[ScenarioCategory ("Controls"), ScenarioCategory ("ListView")]
	[ScenarioCategory ("Controls"), ScenarioCategory ("TreeView")]
	[ScenarioCategory ("Controls"), ScenarioCategory ("Text")]
	public class SearchCollectionNavigatorTester : Scenario {
		// Don't create a Window, just return the top-level view
		public override void Init (Toplevel top, ColorScheme colorScheme)
		{
			Application.Init ();
			Top = top != null ? top : Application.Top;
			Top.ColorScheme = Colors.Base;
		}

		System.Collections.Generic.List<string> _items = new string [] {
				"a",
				"b",
				"bb",
				"c",
				"ccc",
				"ccc",
				"cccc",
				"ddd",
				"dddd",
				"dddd",
				"ddddd",
				"dddddd",
				"ddddddd",
				"this",
				"this is a test",
				"this was a test",
				"this and",
				"that and that",
				"the",
				"think",
				"thunk",
				"thunks",
				"zip",
				"zap",
				"zoo",
				"@jack",
				"@sign",
				"@at",
				"@ateme",
				"n@",
				"n@brown",
				".net",
				"$100.00",
				"$101.00",
				"$101.10",
				"$101.11",
				"$200.00",
				"$210.99",
				"$$",
				"appricot",
				"arm",
				"丗丙业丞",
				"丗丙丛",
				"text",
				"egg",
				"candle",
				" <- space",
				"q",
				"quit",
				"quitter"
			}.ToList<string> ();

		public override void Setup ()
		{
			var allowMarking = new MenuItem ("Allow _Marking", "", null) {
				CheckType = MenuItemCheckStyle.Checked,
				Checked = false
			};
			allowMarking.Action = () => allowMarking.Checked = _listView.AllowsMarking = !_listView.AllowsMarking;

			var allowMultiSelection = new MenuItem ("Allow Multi _Selection", "", null) {
				CheckType = MenuItemCheckStyle.Checked,
				Checked = false
			};
			allowMultiSelection.Action = () => allowMultiSelection.Checked = _listView.AllowsMultipleSelection = !_listView.AllowsMultipleSelection;
			allowMultiSelection.CanExecute = () => allowMarking.Checked;

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_Configure", new MenuItem [] {
					allowMarking,
					allowMultiSelection,
					null,
					new MenuItem ("_Quit", "", () => Quit(), null, null, Key.Q | Key.CtrlMask),
				}),
				new MenuBarItem("_Quit", "CTRL-Q", () => Quit())
			});

			Top.Add (menu);

			_items.Sort (StringComparer.OrdinalIgnoreCase);

			CreateListView ();
			var vsep = new LineView (Terminal.Gui.Graphs.Orientation.Vertical) {
				X = Pos.Right (_listView),
				Y = 1,
				Height = Dim.Fill ()
			};
			Top.Add (vsep);
			CreateTreeView ();

		}

		ListView _listView = null;

		private void CreateListView ()
		{
			var label = new Label () {
				Text = "ListView",
				TextAlignment = TextAlignment.Centered,
				X = 0,
				Y = 1, // for menu
				Width = Dim.Percent (50),
				Height = 1,
			};
			Top.Add (label);

			_listView = new ListView () {
				X = 0,
				Y = Pos.Bottom(label), 
				Width = Dim.Percent (50) - 1,
				Height = Dim.Fill (),
				AllowsMarking = false,
				AllowsMultipleSelection = false,
				ColorScheme = Colors.TopLevel
			};
			Top.Add (_listView);
			
			_listView.SetSource (_items);
		}

		TreeView _treeView = null;

		private void CreateTreeView ()
		{
			var label = new Label () {
				Text = "TreeView",
				TextAlignment = TextAlignment.Centered,
				X = Pos.Right(_listView) + 2,
				Y = 1, // for menu
				Width = Dim.Percent (50),
				Height = 1,
			};
			Top.Add (label);

			_treeView = new TreeView () {
				X = Pos.Right (_listView) + 1,
				Y = Pos.Bottom (label),
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				ColorScheme = Colors.TopLevel
			};
			Top.Add (_treeView);
			
			var root = new TreeNode ("Alpha examples");
			//root.Children = items.Where (i => char.IsLetterOrDigit (i [0])).Select (i => new TreeNode (i)).Cast<ITreeNode>().ToList ();
			//_treeView.AddObject (root);
			root = new TreeNode ("Non-Alpha examples");
			root.Children = _items.Where (i => !char.IsLetterOrDigit (i [0])).Select (i => new TreeNode (i)).Cast<ITreeNode> ().ToList ();
			_treeView.AddObject (root);
			_treeView.ExpandAll ();
		}
		private void Quit ()
		{
			Application.RequestStop ();
		}
	}
}
