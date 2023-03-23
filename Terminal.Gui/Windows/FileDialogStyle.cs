using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Terminal.Gui.Resources;
using Terminal.Gui.Trees;
using static System.Environment;

namespace Terminal.Gui {

	/// <summary>
	/// Stores style settings for <see cref="FileDialog"/>.
	/// </summary>
	public class FileDialogStyle {

		/// <summary>
		/// Gets or sets the header text displayed in the Filename column of the files table.
		/// </summary>
		public string FilenameColumnName { get; set; } = Strings.fdFilename;

		/// <summary>
		/// Gets or sets the header text displayed in the Size column of the files table.
		/// </summary>
		public string SizeColumnName { get; set; } = Strings.fdSize;

		/// <summary>
		/// Gets or sets the header text displayed in the Modified column of the files table.
		/// </summary>
		public string ModifiedColumnName { get; set; } = Strings.fdModified;

		/// <summary>
		/// Gets or sets the header text displayed in the Type column of the files table.
		/// </summary>
		public string TypeColumnName { get; set; } = Strings.fdType;

		/// <summary>
		/// Gets or sets the text displayed in the 'Search' text box when user has not supplied any input yet.
		/// </summary>
		public string SearchCaption { get; internal set; } = Strings.fdSearchCaption;

		/// <summary>
		/// Gets or sets the text displayed in the 'Path' text box when user has not supplied any input yet.
		/// </summary>
		public string PathCaption { get; internal set; } = Strings.fdPathCaption;

		/// <summary>
		/// Gets or sets the text on the 'Ok' button.  Typically you may want to change this to
		/// "Open" or "Save" etc.
		/// </summary>
		public string OkButtonText { get; set; } = "Ok";

		/// <summary>
		/// Gets or sets error message when user attempts to select a file type that is not one of <see cref="AllowedTypes"/>
		/// </summary>
		public string WrongFileTypeFeedback { get; internal set; } = Strings.fdWrongFileTypeFeedback;

		/// <summary>
		/// Gets or sets error message when user selects a directory that does not exist and
		/// <see cref="OpenMode"/> is <see cref="OpenMode.Directory"/> and <see cref="MustExist"/> is <see langword="true"/>.
		/// </summary>
		public string DirectoryMustExistFeedback { get; internal set; } = Strings.fdDirectoryMustExistFeedback;

		/// <summary>
		/// Gets or sets error message when user <see cref="OpenMode"/> is <see cref="OpenMode.Directory"/>
		/// and user enters the name of an existing file (File system cannot have a folder with the same name as a file).
		/// </summary>
		public string FileAlreadyExistsFeedback { get; internal set; } = Strings.fdFileAlreadyExistsFeedback;

		/// <summary>
		/// Gets or sets error message when user selects a file that does not exist and
		/// <see cref="OpenMode"/> is <see cref="OpenMode.File"/> and <see cref="MustExist"/> is <see langword="true"/>.
		/// </summary>
		public string FileMustExistFeedback { get; internal set; } = Strings.fdFileMustExistFeedback;

		/// <summary>
		/// Gets or sets error message when user <see cref="OpenMode"/> is <see cref="OpenMode.File"/>
		/// and user enters the name of an existing directory (File system cannot have a folder with the same name as a file).
		/// </summary>
		public string DirectoryAlreadyExistsFeedback { get; internal set; } = Strings.fdDirectoryAlreadyExistsFeedback;

		/// <summary>
		/// Gets or sets error message when user selects a file/dir that does not exist and
		/// <see cref="OpenMode"/> is <see cref="OpenMode.Mixed"/> and <see cref="MustExist"/> is <see langword="true"/>.
		/// </summary>
		public string FileOrDirectoryMustExistFeedback { get; internal set; } = Strings.fdFileOrDirectoryMustExistFeedback;

		/// <summary>
		/// Gets the style settings for the table of files (in currently selected directory).
		/// </summary>
		public TableView.TableStyle TableStyle { get; internal set; }

		/// <summary>
		/// Gets the style settings for the collapse-able directory/places tree
		/// </summary>
		public TreeStyle TreeStyle { get; internal set; }

		/// <summary>
		/// Gets or Sets the method for getting the root tree objects that are displayed in
		/// the collapse-able tree in the <see cref="FileDialog"/>.  Defaults to all accessible
		/// <see cref="System.Environment.GetLogicalDrives"/> and unique
		/// <see cref="Environment.SpecialFolder"/>.
		/// </summary>
		/// <remarks>Must be configured before showing the dialog.</remarks>
		public FileDialogTreeRootGetter TreeRootGetter { get; set; } = DefaultTreeRootGetter;

		private static IEnumerable<FileDialogRootTreeNode> DefaultTreeRootGetter ()
		{
			var roots = new List<FileDialogRootTreeNode> ();
			try {
				foreach (var d in Environment.GetLogicalDrives ()) {
					roots.Add (new FileDialogRootTreeNode (d, new DirectoryInfo (d)));
				}

			} catch (Exception) {
				// Cannot get the system disks thats fine
			}


			try {
				foreach (var special in Enum.GetValues (typeof (Environment.SpecialFolder)).Cast<SpecialFolder> ()) {
					try {
						var path = Environment.GetFolderPath (special);
						if (
							!string.IsNullOrWhiteSpace (path)
							&& Directory.Exists (path)
							&& !roots.Any (r => string.Equals (r.Path.FullName, path))) {

							roots.Add (new FileDialogRootTreeNode (
							special.ToString (),
							new DirectoryInfo (Environment.GetFolderPath (special))));
						}
					} catch (Exception) {
						// Special file exists but contents are unreadable (permissions?)
						// skip it anyway
					}
				}
			} catch (Exception) {
				// Cannot get the special files for this OS oh well
			}

			return roots;
		}
	}

}