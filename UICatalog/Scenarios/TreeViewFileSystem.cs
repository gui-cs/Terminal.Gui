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

		/// <summary>
		/// A tree view where nodes are files and folders
		/// </summary>
		TreeView<FileSystemInfo> _treeViewFiles;

		/// <summary>
		/// A tree view where nodes are <see cref="ITreeNode"/>
		/// </summary>
		TreeView _treeViewNodes;
		
		/// <summary>
		/// Currently showing tree view (either <see cref="_treeViewFiles"/> or <see cref="_treeViewNodes"/>)
		/// </summary>
		ITreeView _treeView;

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
				new MenuBarItem ("_View", new MenuItem [] {
					new MenuItem ("_ShowLines", "", () => ShowLines()),
					new MenuItem ("_ShowExpandableSymbol", "", () => ShowExpandableSymbol()),
				}),
			});
			Top.Add (menu);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.F2, "~F2~ File Tree", () => SwitchToFileTree()),
				new StatusItem(Key.F3, "~F3~ Clear Objects", () => ClearObjects()),
				new StatusItem(Key.F4, "~F4~ Simple Tree", () => SwitchToSimpleTree()),
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Quit()),
			});
			Top.Add (statusBar);


			_treeViewFiles = new TreeView<FileSystemInfo> () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};
			
			// setup delegates
			_treeViewFiles.TreeBuilder = new DelegateTreeBuilder<FileSystemInfo>(

				// Determines how to compute children of any given branch
				GetChildren,
				// As a shortcut to enumerating half the file system, tell tree that all directories are expandable (even if they turn out to be empty later on)				
				(o)=>o is DirectoryInfo
			);

			// Determines how to represent objects as strings on the screen
			_treeViewFiles.AspectGetter = FileSystemAspectGetter;

			_treeViewNodes = new TreeView() {
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
		}


		private void ShowLines ()
		{
			_treeView.ShowBranchLines = !_treeView.ShowBranchLines;
			_treeView.SetNeedsDisplay();
		}
		
		private void ShowExpandableSymbol ()
		{
			_treeView.ShowExpandableSymbol = !_treeView.ShowExpandableSymbol;
			_treeView.SetNeedsDisplay();
		}
	
		private void SwitchToSimpleTree ()
		{
			Win.Remove (_treeViewFiles);
			Win.Add(_treeViewNodes);
			_treeView = _treeViewNodes;

			ClearObjects();
		
			// Add 2 root nodes with simple set of subfolders
			_treeViewNodes.AddObject(CreateSimpleRoot());
			_treeViewNodes.AddObject(CreateSimpleRoot());
			
		}

		private ITreeNode CreateSimpleRoot ()
		{
			
			return new TreeNode("Root"){
				Children = new List<ITreeNode>()
				{
					new TreeNode("Folder_1"){
					Children = new List<ITreeNode>()
					{
						new TreeNode("Folder_1.1"){
							Children = new List<ITreeNode>()
							{
								new TreeNode("File_1.1.1"),
								new TreeNode("File_1.1.2")
							}},
						new TreeNode("Folder_1.2"){
							Children = new List<ITreeNode>()
							{
								new TreeNode("File_1.2.1"),
								new TreeNode("File_1.2.2")
							}},
						new TreeNode("File_1.1")
					}},
					new TreeNode("Folder_2"){
					Children = new List<ITreeNode>()
					{
						new TreeNode("Folder_2.1"){
							Children = new List<ITreeNode>()
							{
								new TreeNode("File_2.1.1"),
								new TreeNode("File_2.1.2")
							}},
						new TreeNode("Folder_2.2"){
							Children = new List<ITreeNode>()
							{
								new TreeNode("File_2.2.1"),
								new TreeNode("File_2.2.2")
							}},
						new TreeNode("File_2.1")
					}},
					new TreeNode("Folder_3"){
					Children = new List<ITreeNode>()
					{
						new TreeNode("Folder_3.1"){
							Children = new List<ITreeNode>()
							{
								new TreeNode("File_3.1.1"),
								new TreeNode("File_3.1.2")
							}},
						new TreeNode("Folder_3.2"){
							Children = new List<ITreeNode>()
							{
								new TreeNode("File_3.2.1"),
								new TreeNode("File_3.2.2")
							}},
						new TreeNode("File_3.1")
					}}
				}
			};
		}

		private void ClearObjects()
		{
			_treeView?.ClearObjects();
		}
		private void SwitchToFileTree()
		{
			// switch trees
			Win.Remove(_treeViewNodes);
			Win.Add (_treeViewFiles);
			_treeView = _treeViewFiles;

			ClearObjects();

			_treeViewFiles.AddObjects(DriveInfo.GetDrives().Select(d=>d.RootDirectory));
		}

		private IEnumerable<FileSystemInfo> GetChildren(FileSystemInfo model)
		{
			// If it is a directory it's children are all contained files and dirs
			if(model is DirectoryInfo d) {
				try {
					return d.GetFileSystemInfos()
						//show directories first
						.OrderBy(a=>a is DirectoryInfo ? 0:1)
						.ThenBy(b=>b.Name);
				}
				catch(SystemException) {

					// Access violation or other error getting the file list for directory
					return Enumerable.Empty<FileSystemInfo>();
				}
			}

		    return Enumerable.Empty<FileSystemInfo>();;
		}
		private string FileSystemAspectGetter(FileSystemInfo model)
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
