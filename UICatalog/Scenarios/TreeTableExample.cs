using System;
using System.IO;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "TreeTableExample", Description: "Mount multiple TreeView in a TableView.")]
	[ScenarioCategory ("Controls"), ScenarioCategory ("TreeView"), ScenarioCategory ("Files and IO")]
	public class TreeTableExample : Scenario {
		public override void Setup ()
		{
			base.Setup ();

			Win.Title = this.GetName ();
			Win.Height = Dim.Fill ();
			Application.Top.LayoutSubviews ();

			var tbl = new TableView{
				Width = Dim.Fill(),
				Height = Dim.Fill(),
			};

            var source = new TreeTableSource<FileSystemInfo>(tbl,new (){
                {"Name", f=>f.Name},
                {"CreationTime", f=>f.CreationTime}
            });

            foreach(var folder in Enum.GetValues(typeof(Environment.SpecialFolder)))
            {
                var path = Environment.GetFolderPath((Environment.SpecialFolder)folder);

                if(string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                var tree = new TreeView<FileSystemInfo>();
                tree.TreeBuilder = new DelegateTreeBuilder<FileSystemInfo>(
                    e=>e is DirectoryInfo d ? d.GetFileSystemInfos() :
                     Enumerable.Empty<FileSystemInfo>());

                tree.AddObject(new DirectoryInfo(path));

                source.AddRow(tree);
            }

            tbl.Table = source;

            Win.Add(tbl);

		}

	}
}
