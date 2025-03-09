using System.IO.Abstractions;
using Terminal.Gui.Resources;

namespace Terminal.Gui;

/// <summary>Default file operation handlers using modal dialogs.</summary>
public class DefaultFileOperations : IFileOperations
{
    /// <inheritdoc/>
    public bool Delete (IEnumerable<IFileSystemInfo> toDelete)
    {
        // Default implementation does not allow deleting multiple files
        if (toDelete.Count () != 1)
        {
            return false;
        }

        IFileSystemInfo d = toDelete.Single ();
        string adjective = d.Name;

        int result = MessageBox.Query (
                                       string.Format (Strings.fdDeleteTitle, adjective),
                                       string.Format (Strings.fdDeleteBody, adjective),
                                       Strings.btnYes,
                                       Strings.btnNo
                                      );

        try
        {
            if (result == 0)
            {
                if (d is IFileInfo)
                {
                    d.Delete ();
                }
                else
                {
                    ((IDirectoryInfo)d).Delete (true);
                }

                return true;
            }
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery (Strings.fdDeleteFailedTitle, ex.Message, Strings.btnOk);
        }

        return false;
    }

    /// <inheritdoc/>
    public IFileSystemInfo Rename (IFileSystem fileSystem, IFileSystemInfo toRename)
    {
        // Don't allow renaming C: or D: or / (on linux) etc
        if (toRename is IDirectoryInfo dir && dir.Parent is null)
        {
            return null;
        }

        if (Prompt (Strings.fdRenameTitle, toRename.Name, out string newName))
        {
            if (!string.IsNullOrWhiteSpace (newName))
            {
                try
                {
                    if (toRename is IFileInfo f)
                    {
                        IFileInfo newLocation =
                            fileSystem.FileInfo.New (
                                                     Path.Combine (
                                                                   f.Directory.FullName,
                                                                   newName
                                                                  )
                                                    );
                        f.MoveTo (newLocation.FullName);

                        return newLocation;
                    }
                    else
                    {
                        var d = (IDirectoryInfo)toRename;

                        IDirectoryInfo newLocation =
                            fileSystem.DirectoryInfo.New (
                                                          Path.Combine (
                                                                        d.Parent.FullName,
                                                                        newName
                                                                       )
                                                         );
                        d.MoveTo (newLocation.FullName);

                        return newLocation;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.ErrorQuery (Strings.fdRenameFailedTitle, ex.Message, "Ok");
                }
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public IFileSystemInfo New (IFileSystem fileSystem, IDirectoryInfo inDirectory)
    {
        if (Prompt (Strings.fdNewTitle, "", out string named))
        {
            if (!string.IsNullOrWhiteSpace (named))
            {
                try
                {
                    IDirectoryInfo newDir =
                        fileSystem.DirectoryInfo.New (
                                                      Path.Combine (inDirectory.FullName, named)
                                                     );
                    newDir.Create ();

                    return newDir;
                }
                catch (Exception ex)
                {
                    MessageBox.ErrorQuery (Strings.fdNewFailed, ex.Message, "Ok");
                }
            }
        }

        return null;
    }

    private bool Prompt (string title, string defaultText, out string result)
    {
        var confirm = false;
        var btnOk = new Button { IsDefault = true, Text = Strings.btnOk };

        btnOk.Accepting += (s, e) =>
                         {
                             confirm = true;
                             Application.RequestStop ();
                             // Anytime Accepting is handled, make sure to set e.Cancel to false.
                             e.Cancel = false;
                         };
        var btnCancel = new Button { Text = Strings.btnCancel };

        btnCancel.Accepting += (s, e) =>
                             {
                                 confirm = false;
                                 Application.RequestStop ();
                                 // Anytime Accepting is handled, make sure to set e.Cancel to false.
                                 e.Cancel = false;
                             };

        var lbl = new Label { Text = Strings.fdRenamePrompt };
        var tf = new TextField { X = Pos.Right (lbl), Width = Dim.Fill (), Text = defaultText };
        tf.SelectAll ();

        var dlg = new Dialog { Title = title, Width = Dim.Percent (50), Height = 4 };
        dlg.Add (lbl);
        dlg.Add (tf);

        // Add buttons last so tab order is friendly
        // and TextField gets focus
        dlg.AddButton (btnOk);
        dlg.AddButton (btnCancel);

        Application.Run (dlg);
        dlg.Dispose ();

        result = tf.Text;

        return confirm;
    }
}
