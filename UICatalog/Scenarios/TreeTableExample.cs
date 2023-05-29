using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "TreeTableExample", Description: "Mount multiple TreeView in a TableView.")]
	[ScenarioCategory ("Controls"), ScenarioCategory ("TreeView"), ScenarioCategory ("Files and IO")]
	public class TreeTableExample : Scenario {

		List<View> toDispose = new List<View> ();

		public override void Setup ()
		{
			base.Setup ();

			Win.Title = this.GetName ();
			Win.Height = Dim.Fill ();
			Application.Top.LayoutSubviews ();

			var tbl = new TableView {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};

			var source = new TreeTableSource<FileSystemInfo> (tbl, "Name", new (){
		{"Extension", f=>f.Extension},
		{"CreationTime", f=>f.CreationTime}

	    });

			foreach (var folder in Enum.GetValues (typeof (Environment.SpecialFolder))) {
				var path = Environment.GetFolderPath ((Environment.SpecialFolder)folder);

				if (string.IsNullOrWhiteSpace (path)) {
					continue;
				}

				var tree = new TreeView<FileSystemInfo> ();
				tree.AspectGetter = (f)=>f.Name;

				tree.TreeBuilder = new DelegateTreeBuilder<FileSystemInfo> (GetChildren);

				tree.AddObject (new DirectoryInfo (path));

				source.AddRow (tree);
				toDispose.Add (tree);
			}

			tbl.Table = source;

			Win.Add (tbl);

		}


		private IEnumerable<FileSystemInfo> GetChildren (FileSystemInfo arg)
		{
			try {
				return arg is DirectoryInfo d ?
					d.GetFileSystemInfos () :
					Enumerable.Empty<FileSystemInfo> ();
			} catch (Exception) {
				// Permission denied etc
				return Enumerable.Empty<FileSystemInfo> ();
			}

		}

		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);

			foreach(var d in toDispose) {
				d.Dispose ();
			}
		}
	}
}
