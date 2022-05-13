using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "TreeViewFileSystem", Description: "Hierarchical file system explorer based on TreeView.")]
	[ScenarioCategory ("Controls"), ScenarioCategory ("TreeView"), ScenarioCategory ("Files and IO")]
	public class TreeViewFileSystem : Scenario {

		/// <summary>
		/// A tree view where nodes are files and folders
		/// </summary>
		TreeView<FileSystemInfo> treeViewFiles;

		MenuItem miShowLines;
		private MenuItem miPlusMinus;
		private MenuItem miArrowSymbols;
		private MenuItem miNoSymbols;
		private MenuItem miColoredSymbols;
		private MenuItem miInvertSymbols;
		private MenuItem miUnicodeSymbols;
		private MenuItem miFullPaths;
		private MenuItem miLeaveLastRow;
		private MenuItem miCustomColors;
		private Terminal.Gui.Attribute green;
		private Terminal.Gui.Attribute red;

		public override void Setup ()
		{
			Win.Title = this.GetName ();
			Win.Y = 1; // menu
			Win.Height = Dim.Fill (1); // status bar
			Top.LayoutSubviews ();

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_Quit", "", () => Quit()),
				}),
				new MenuBarItem ("_View", new MenuItem [] {
					miShowLines = new MenuItem ("_ShowLines", "", () => ShowLines()){
					Checked = true, CheckType = MenuItemCheckStyle.Checked
						},
					null /*separator*/,
					miPlusMinus = new MenuItem ("_PlusMinusSymbols", "", () => SetExpandableSymbols('+','-')){Checked = true, CheckType = MenuItemCheckStyle.Radio},
					miArrowSymbols = new MenuItem ("_ArrowSymbols", "", () => SetExpandableSymbols('>','v')){Checked = false, CheckType = MenuItemCheckStyle.Radio},
					miNoSymbols = new MenuItem ("_NoSymbols", "", () => SetExpandableSymbols(null,null)){Checked = false, CheckType = MenuItemCheckStyle.Radio},
					miUnicodeSymbols = new MenuItem ("_Unicode", "", () => SetExpandableSymbols('ஹ','﷽')){Checked = false, CheckType = MenuItemCheckStyle.Radio},
					null /*separator*/,
					miColoredSymbols = new MenuItem ("_ColoredSymbols", "", () => ShowColoredExpandableSymbols()){Checked = false, CheckType = MenuItemCheckStyle.Checked},
					miInvertSymbols = new MenuItem ("_InvertSymbols", "", () => InvertExpandableSymbols()){Checked = false, CheckType = MenuItemCheckStyle.Checked},
					miFullPaths = new MenuItem ("_FullPaths", "", () => SetFullName()){Checked = false, CheckType = MenuItemCheckStyle.Checked},
					miLeaveLastRow = new MenuItem ("_LeaveLastRow", "", () => SetLeaveLastRow()){Checked = true, CheckType = MenuItemCheckStyle.Checked},
					miCustomColors = new MenuItem ("C_ustomColors", "", () => SetCustomColors()){Checked = false, CheckType = MenuItemCheckStyle.Checked},
				}),
			});
			Top.Add (menu);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Quit()),
			});
			Top.Add (statusBar);

			var lblFiles = new Label ("File Tree:") {
				X = 0,
				Y = 1
			};
			Win.Add (lblFiles);

			treeViewFiles = new TreeView<FileSystemInfo> () {
				X = 0,
				Y = Pos.Bottom (lblFiles),
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};

			treeViewFiles.ObjectActivated += TreeViewFiles_ObjectActivated;
			treeViewFiles.MouseClick += TreeViewFiles_MouseClick;
			treeViewFiles.KeyPress += TreeViewFiles_KeyPress;

			SetupFileTree ();

			Win.Add (treeViewFiles);

			SetupScrollBar ();

			green = Application.Driver.MakeAttribute (Color.Green, Color.Blue);
			red = Application.Driver.MakeAttribute (Color.Red, Color.Blue);
		}

		private void TreeViewFiles_KeyPress (View.KeyEventEventArgs obj)
		{
			if(obj.KeyEvent.Key == (Key.R | Key.CtrlMask)) {

				var selected = treeViewFiles.SelectedObject;
				
				// nothing is selected
				if (selected == null)
					return;
				
				var location = treeViewFiles.GetObjectRow (selected);

				//selected object is offscreen or somehow not found
				if (location == null || location < 0 || location > treeViewFiles.Frame.Height)
					return;

				ShowContextMenu (new Point (
					5 + treeViewFiles.Frame.X,
					location.Value + treeViewFiles.Frame.Y + 2),
					selected);
			}
		}

		private void TreeViewFiles_MouseClick (View.MouseEventArgs obj)
		{
			// if user right clicks
			if (obj.MouseEvent.Flags.HasFlag(MouseFlags.Button3Clicked)) {

				var rightClicked = treeViewFiles.GetObjectAtPoint (new Point (obj.MouseEvent.X, obj.MouseEvent.Y));

				// nothing was clicked
				if (rightClicked == null)
					return;

				ShowContextMenu (new Point (
					obj.MouseEvent.X + treeViewFiles.Frame.X,
					obj.MouseEvent.Y + treeViewFiles.Frame.Y + 2),
					rightClicked);
			}
		}

		private void ShowContextMenu (Point screenPoint, FileSystemInfo forObject)
		{
			var menu = new ContextMenu ();
			menu.Position = screenPoint;

			menu.MenuItems = new MenuBarItem (new [] { new MenuItem ("Properties", null, () => ShowPropertiesOf (forObject)) });
			
			Application.MainLoop.Invoke(menu.Show);
		}

		private void ShowPropertiesOf (FileSystemInfo fileSystemInfo)
		{
			if (fileSystemInfo is FileInfo f) {
				System.Text.StringBuilder sb = new System.Text.StringBuilder ();
				sb.AppendLine ($"Path:{f.DirectoryName}");
				sb.AppendLine ($"Size:{f.Length:N0} bytes");
				sb.AppendLine ($"Modified:{ f.LastWriteTime}");
				sb.AppendLine ($"Created:{ f.CreationTime}");

				MessageBox.Query (f.Name, sb.ToString (), "Close");
			}

			if (fileSystemInfo is DirectoryInfo dir) {

				System.Text.StringBuilder sb = new System.Text.StringBuilder ();
				sb.AppendLine ($"Path:{dir.Parent?.FullName}");
				sb.AppendLine ($"Modified:{ dir.LastWriteTime}");
				sb.AppendLine ($"Created:{ dir.CreationTime}");

				MessageBox.Query (dir.Name, sb.ToString (), "Close");
			}
		}

		private void SetupScrollBar ()
		{
			// When using scroll bar leave the last row of the control free (for over-rendering with scroll bar)
			treeViewFiles.Style.LeaveLastRow = true;

			var _scrollBar = new ScrollBarView (treeViewFiles, true);

			_scrollBar.ChangedPosition += () => {
				treeViewFiles.ScrollOffsetVertical = _scrollBar.Position;
				if (treeViewFiles.ScrollOffsetVertical != _scrollBar.Position) {
					_scrollBar.Position = treeViewFiles.ScrollOffsetVertical;
				}
				treeViewFiles.SetNeedsDisplay ();
			};

			_scrollBar.OtherScrollBarView.ChangedPosition += () => {
				treeViewFiles.ScrollOffsetHorizontal = _scrollBar.OtherScrollBarView.Position;
				if (treeViewFiles.ScrollOffsetHorizontal != _scrollBar.OtherScrollBarView.Position) {
					_scrollBar.OtherScrollBarView.Position = treeViewFiles.ScrollOffsetHorizontal;
				}
				treeViewFiles.SetNeedsDisplay ();
			};

			treeViewFiles.DrawContent += (e) => {
				_scrollBar.Size = treeViewFiles.ContentHeight;
				_scrollBar.Position = treeViewFiles.ScrollOffsetVertical;
				_scrollBar.OtherScrollBarView.Size = treeViewFiles.GetContentWidth (true);
				_scrollBar.OtherScrollBarView.Position = treeViewFiles.ScrollOffsetHorizontal;
				_scrollBar.Refresh ();
			};
		}

		private void SetupFileTree ()
		{

			// setup delegates
			treeViewFiles.TreeBuilder = new DelegateTreeBuilder<FileSystemInfo> (

				// Determines how to compute children of any given branch
				GetChildren,
				// As a shortcut to enumerating half the file system, tell tree that all directories are expandable (even if they turn out to be empty later on)				
				(o) => o is DirectoryInfo
			);

			// Determines how to represent objects as strings on the screen
			treeViewFiles.AspectGetter = FileSystemAspectGetter;

			treeViewFiles.AddObjects (DriveInfo.GetDrives ().Select (d => d.RootDirectory));
		}

		private void TreeViewFiles_ObjectActivated (ObjectActivatedEventArgs<FileSystemInfo> obj)
		{
			ShowPropertiesOf (obj.ActivatedObject);
		}

		private void ShowLines ()
		{
			miShowLines.Checked = !miShowLines.Checked;

			treeViewFiles.Style.ShowBranchLines = miShowLines.Checked;
			treeViewFiles.SetNeedsDisplay ();
		}

		private void SetExpandableSymbols (Rune? expand, Rune? collapse)
		{
			miPlusMinus.Checked = expand == '+';
			miArrowSymbols.Checked = expand == '>';
			miNoSymbols.Checked = expand == null;
			miUnicodeSymbols.Checked = expand == 'ஹ';

			treeViewFiles.Style.ExpandableSymbol = expand;
			treeViewFiles.Style.CollapseableSymbol = collapse;
			treeViewFiles.SetNeedsDisplay ();
		}
		private void ShowColoredExpandableSymbols ()
		{
			miColoredSymbols.Checked = !miColoredSymbols.Checked;

			treeViewFiles.Style.ColorExpandSymbol = miColoredSymbols.Checked;
			treeViewFiles.SetNeedsDisplay ();
		}
		private void InvertExpandableSymbols ()
		{
			miInvertSymbols.Checked = !miInvertSymbols.Checked;

			treeViewFiles.Style.InvertExpandSymbolColors = miInvertSymbols.Checked;
			treeViewFiles.SetNeedsDisplay ();
		}

		private void SetFullName ()
		{
			miFullPaths.Checked = !miFullPaths.Checked;

			if (miFullPaths.Checked) {
				treeViewFiles.AspectGetter = (f) => f.FullName;
			} else {
				treeViewFiles.AspectGetter = (f) => f.Name;
			}
		}

		private void SetLeaveLastRow ()
		{
			miLeaveLastRow.Checked = !miLeaveLastRow.Checked;
			treeViewFiles.Style.LeaveLastRow = miLeaveLastRow.Checked;
		}
		private void SetCustomColors()
		{
			var yellow = new ColorScheme
			{
				Focus = new Terminal.Gui.Attribute(Color.BrightYellow,treeViewFiles.ColorScheme.Focus.Background),
				Normal = new Terminal.Gui.Attribute (Color.BrightYellow,treeViewFiles.ColorScheme.Normal.Background),
			};

			miCustomColors.Checked = !miCustomColors.Checked;

			if(miCustomColors.Checked)
			{
				treeViewFiles.ColorGetter = (m)=>
				{
					return m is DirectoryInfo ? yellow : null;
				};
			}
			else
			{
				treeViewFiles.ColorGetter = null;
			}
		}


		private IEnumerable<FileSystemInfo> GetChildren (FileSystemInfo model)
		{
			// If it is a directory it's children are all contained files and dirs
			if (model is DirectoryInfo d) {
				try {
					return d.GetFileSystemInfos ()
						//show directories first
						.OrderBy (a => a is DirectoryInfo ? 0 : 1)
						.ThenBy (b => b.Name);
				} catch (SystemException) {

					// Access violation or other error getting the file list for directory
					return Enumerable.Empty<FileSystemInfo> ();
				}
			}

			return Enumerable.Empty<FileSystemInfo> (); ;
		}
		private string FileSystemAspectGetter (FileSystemInfo model)
		{
			if (model is DirectoryInfo d) {
				return d.Name;
			}
			if (model is FileInfo f) {
				return f.Name;
			}

			return model.ToString ();
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}
	}
}
