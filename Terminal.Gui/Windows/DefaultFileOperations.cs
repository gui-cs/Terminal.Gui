using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terminal.Gui.Resources;

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
			var adjective = d.Name;

			int result = MessageBox.Query (
				string.Format (Strings.fdDeleteTitle, adjective),
				string.Format (Strings.fdDeleteBody, adjective),
				Strings.fdYes, Strings.fdNo);

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
				MessageBox.ErrorQuery (Strings.fdDeleteFailedTitle, ex.Message, "Ok");
			}

			return false;
		}

		private bool Prompt (string title, string defaultText, out string result)
		{

			bool confirm = false;
			var btnOk = new Button ("Ok") {
				IsDefault = true,
			};
			btnOk.Clicked += (s, e) => {
				confirm = true;
				Application.RequestStop ();
			};
			var btnCancel = new Button ("Cancel");
			btnCancel.Clicked += (s, e) => {
				confirm = false;
				Application.RequestStop ();
			};

			var lbl = new Label (Strings.fdRenamePrompt);
			var tf = new TextField (defaultText) {
				X = Pos.Right (lbl),
				Width = Dim.Fill (),
			};
			tf.SelectAll ();

			var dlg = new Dialog (title) {
				Width = Dim.Percent (50),
				Height = 4
			};
			dlg.Add (lbl);
			dlg.Add (tf);

			// Add buttons last so tab order is friendly
			// and TextField gets focus
			dlg.AddButton (btnOk);
			dlg.AddButton (btnCancel);

			Application.Run (dlg);

			result = tf.Text?.ToString ();

			return confirm;
		}

		/// <inheritdoc/>
		public FileSystemInfo Rename (FileSystemInfo toRename)
		{
			// Dont allow renaming C: or D: or / (on linux) etc
			if (toRename is DirectoryInfo dir && dir.Parent == null) {
				return null;
			}

			if (Prompt (Strings.fdRenameTitle, toRename.Name, out var newName)) {
				if (!string.IsNullOrWhiteSpace (newName)) {
					try {

						if (toRename is FileInfo f) {

							var newLocation = new FileInfo (Path.Combine (f.Directory.FullName, newName));
							f.MoveTo (newLocation.FullName);
							return newLocation;

						} else {
							var d = ((DirectoryInfo)toRename);

							var newLocation = new DirectoryInfo (Path.Combine (d.Parent.FullName, newName));
							d.MoveTo (newLocation.FullName);
							return newLocation;
						}
					} catch (Exception ex) {
						MessageBox.ErrorQuery (Strings.fdRenameFailedTitle, ex.Message, "Ok");
					}

				}
			}
			return null;
		}

		/// <inheritdoc/>
		public FileSystemInfo New (DirectoryInfo inDirectory)
		{
			if (Prompt (Strings.fdNewTitle, "", out var named)) {
				if (!string.IsNullOrWhiteSpace (named)) {
					try {
						var newDir = new DirectoryInfo (Path.Combine (inDirectory.FullName, named));
						newDir.Create ();
						return newDir;
					} catch (Exception ex) {
						MessageBox.ErrorQuery (Strings.fdNewFailed, ex.Message, "Ok");
					}
				}
			}
			return null;
		}
	}
}