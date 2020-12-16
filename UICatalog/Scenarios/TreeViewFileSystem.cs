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
		TreeView<FileSystemInfo> treeViewFiles;

		/// <summary>
		/// A tree view where nodes are <see cref="ITreeNode"/>
		/// </summary>
		TreeView treeViewNodes;
		
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
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Quit()),
			});
			Top.Add (statusBar);

			var lblFiles = new Label("File Tree:"){
				X=0,
				Y=1
			};
			Win.Add(lblFiles);

			treeViewFiles = new TreeView<FileSystemInfo> () {
				X = 0,
				Y = Pos.Bottom(lblFiles),
				Width = 40,
				Height = 9,
			};
			
			SetupFileTree();

			Win.Add(treeViewFiles);
			
			var lblNodeTree = new Label("Node Tree:"){
				X=0,
				Y=Pos.Bottom(treeViewFiles)+1
			};

			Win.Add(lblNodeTree);
			
			treeViewNodes = new TreeView() {
				X = 0,
				Y = Pos.Bottom(lblNodeTree),
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};

			SetupNodeTree();

			Win.Add(treeViewNodes);
		}

		private void SetupNodeTree ()
		{		
			// Add 2 root nodes with simple set of subfolders
			treeViewNodes.AddObject(CreateSimpleRoot());
			treeViewNodes.AddObject(CreateSimpleRoot());
		}

		private void SetupFileTree ()
		{
			
			// setup delegates
			treeViewFiles.TreeBuilder = new DelegateTreeBuilder<FileSystemInfo>(

				// Determines how to compute children of any given branch
				GetChildren,
				// As a shortcut to enumerating half the file system, tell tree that all directories are expandable (even if they turn out to be empty later on)				
				(o)=>o is DirectoryInfo
			);

			// Determines how to represent objects as strings on the screen
			treeViewFiles.AspectGetter = FileSystemAspectGetter;

			treeViewFiles.AddObjects(DriveInfo.GetDrives().Select(d=>d.RootDirectory));
		}

		private void ShowLines ()
		{
			treeViewNodes.ShowBranchLines = !treeViewNodes.ShowBranchLines;
			treeViewNodes.SetNeedsDisplay();

			treeViewFiles.ShowBranchLines = !treeViewFiles.ShowBranchLines;
			treeViewFiles.SetNeedsDisplay();
		}
		
		private void ShowExpandableSymbol ()
		{
			treeViewNodes.ShowExpandableSymbol = !treeViewNodes.ShowExpandableSymbol;
			treeViewNodes.SetNeedsDisplay();

			treeViewFiles.ShowExpandableSymbol = !treeViewFiles.ShowExpandableSymbol;
			treeViewFiles.SetNeedsDisplay();
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
