using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Terminal.Gui {
	/// <summary>
	/// Default file operation handlers using modal dialogs.
	/// </summary>
	public class DefaultFileOperations : IFileOperations {
		
		/// <inheritdoc/>
		public bool Delete (IEnumerable<FileSystemInfo> toDelete)
		{
			// Default implementation does not allow deleting multiple files
			if (toDelete.Count () != 1) {
				return false;
			}
			var d = toDelete.Single ();
			var adjective =  d is FileInfo ? "File" : "Directory";

			int result = MessageBox.Query (
				string.Format ("Delete {0}", adjective),
				string.Format ("Are you sure you want to delete the selected {0}? This operation is permanent", adjective),
				"Yes", "No");

			try {
				if (result == 0) {
					if (d is FileInfo) {
						d.Delete ();
					} else {
						((DirectoryInfo)d).Delete (true);
					}

                    return true;
				}
			} catch (Exception ex) {
				MessageBox.ErrorQuery ("Delete Failed", ex.Message, "Ok");
			}

            return false;
		}

		/// <inheritdoc/>
		public bool Rename (FileSystemInfo toRename)
		{
			return false;
		}

		/// <inheritdoc/>
		public bool New (DirectoryInfo inDirectory)
		{
			return false;
		}
	}
}