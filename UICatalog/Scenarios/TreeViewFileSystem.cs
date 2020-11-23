using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "TreeViewFileSystem", Description: "Hierarchical file system explorer based on TreeView")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Dialogs")]
	[ScenarioCategory ("Text")]
	[ScenarioCategory ("Dialogs")]
	[ScenarioCategory ("TopLevel")]
	class TreeViewFileSystem : Scenario {

		TreeView _treeView;

		public override void Setup ()
		{
			Win.Title = this.GetName();
			Win.Y = 1; // menu
			Win.Height = Dim.Fill (1); // status bar
			Top.LayoutSubviews ();

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_Quit", "", () => Quit()),
				}),
			});
			Top.Add (menu);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.F2, "~F2~ Add Root Drives", () => AddRootDrives()),
				new StatusItem(Key.F3, "~F3~ Remove Root Object", () => RemoveRoot()),
				new StatusItem(Key.F4, "~F4~ Clear Objects", () => ClearObjects()),
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Quit()),
			});
			Top.Add (statusBar);


			_treeView = new TreeView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};

			string root = System.IO.Path.GetPathRoot(Environment.CurrentDirectory);

			if(root == null)
			{
				MessageBox.ErrorQuery(10,5,"Error","Unable to determine file system root","ok");
				return;
			}

			// Determines how to compute children of any given branch
			_treeView.ChildrenGetter = GetChildren;

			// Determines how to represent objects as strings on the screen
			_treeView.AspectGetter = AspectGetter;

			Win.Add (_treeView);
		}

		private void ClearObjects()
		{
			_treeView.ClearObjects();
		}
		private void AddRootDrives()
		{
			_treeView.AddObjects(DriveInfo.GetDrives().Select(d=>d.RootDirectory));
		}
		private void RemoveRoot()
		{
			if(_treeView.SelectedObject == null)
				MessageBox.ErrorQuery(10,5,"Error","No object selected","ok");
			else {
				_treeView.Remove(_treeView.SelectedObject);
			}
		}

		private IEnumerable<object> GetChildren(object model)
		{
			// If it is a directory it's children are all contained files and dirs
			if(model is DirectoryInfo d) {
				try {
					return d.GetDirectories().Cast<object>().Union(d.GetFileSystemInfos());
				}
				catch(SystemException ex) {
					return new []{ex};
				}
			}

		    return new object[0];
		}
		private string AspectGetter(object model)
		{
			if(model is DirectoryInfo d)
				return d.Name;
			if(model is FileInfo f)
				return f.Name;

			return model.ToString();
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}
	}
}
