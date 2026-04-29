using System.IO.Abstractions;

namespace Terminal.Gui.Views;

/// <summary>Default file operation handlers using modal dialogs.</summary>
public class DefaultFileOperations : IFileOperations
{
    /// <inheritdoc/>
    public bool Delete (IApplication? app, IEnumerable<IFileSystemInfo> toDelete)
    {
        // Default implementation does not allow deleting multiple files
        IEnumerable<IFileSystemInfo> fileSystemInfos = toDelete as IFileSystemInfo [] ?? toDelete.ToArray ();

        if (fileSystemInfos.Count () != 1)
        {
            return false;
        }

        IFileSystemInfo d = fileSystemInfos.Single ();
        string adjective = d.Name;

        int? result = MessageBox.Query (app ?? throw new ArgumentNullException (nameof (app)),
                                        string.Format (Strings.fdDeleteTitle, adjective),
                                        string.Format (Strings.fdDeleteBody, adjective),
                                        Strings.btnYes,
                                        Strings.btnNo);

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
            MessageBox.ErrorQuery (app, Strings.fdDeleteFailedTitle, ex.Message, Strings.btnOk);
        }

        return false;
    }

    /// <inheritdoc/>
    public IFileSystemInfo? Rename (IApplication? app, IFileSystem fileSystem, IFileSystemInfo toRename)
    {
        // Don't allow renaming C: or D: or / (on linux) etc
        if (toRename is IDirectoryInfo { Parent: null })
        {
            return null;
        }

        if (!Prompt (app ?? throw new ArgumentNullException (nameof (app)), Strings.fdRenameTitle, toRename.Name, out string newName))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace (newName))
        {
            return null;
        }

        try
        {
            if (toRename is IFileInfo f)
            {
                IFileInfo newLocation = fileSystem.FileInfo.New (Path.Combine (f.Directory?.FullName ?? throw new InvalidOperationException (), newName));
                f.MoveTo (newLocation.FullName);

                return newLocation;
            }
            else
            {
                var d = (IDirectoryInfo)toRename;

                IDirectoryInfo newLocation =
                    fileSystem.DirectoryInfo.New (Path.Combine (d.Parent?.FullName ?? throw new InvalidOperationException (), newName));
                d.MoveTo (newLocation.FullName);

                return newLocation;
            }
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery (app, Strings.fdRenameFailedTitle, ex.Message, Strings.btnOk);
        }

        return null;
    }

    /// <inheritdoc/>
    public IFileSystemInfo? New (IApplication? app, IFileSystem fileSystem, IDirectoryInfo inDirectory)
    {
        if (app is null)
        {
            ArgumentNullException.ThrowIfNull (app);
        }
        var tv = new TextField { Width = Dim.Fill (0, 50), Height = 1 };
        string? result = app.TopRunnable?.Prompt<TextField, string> (tv, beginInitHandler: prompt => { prompt.Title = Strings.fdNewTitle; });

        if (string.IsNullOrWhiteSpace (result))
        {
            return null;
        }

        try
        {
            IDirectoryInfo newDir = fileSystem.DirectoryInfo.New (Path.Combine (inDirectory.FullName, result));
            newDir.Create ();

            return newDir;
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery (app, Strings.fdNewFailed, ex.Message, Strings.btnOk);
        }

        return null;
    }

    private bool Prompt (IApplication app, string title, string defaultText, out string result)
    {
        var confirm = false;
        var btnOk = new Button { IsDefault = true, Text = Strings.btnOk };

        btnOk.Accepting += (s, e) =>
                           {
                               confirm = true;
                               (s as View)?.App?.RequestStop ();

                               // When Accepting is handled, set e.Handled to true to prevent further processing.
                               e.Handled = true;
                           };
        var btnCancel = new Button { Text = Strings.btnCancel };

        btnCancel.Accepting += (s, e) =>
                               {
                                   confirm = false;
                                   (s as View)?.App?.RequestStop ();

                                   // When Accepting is handled, set e.Handled to true to prevent further processing.
                                   e.Handled = true;
                               };

        var lbl = new Label { Text = Strings.fdRenamePrompt };
        var tf = new TextField { X = Pos.Right (lbl) + 1, Width = Dim.Fill (0, 50), Height = 1, Text = defaultText };
        tf.SelectAll ();

        var dlg = new Dialog { Title = title };
        dlg.Add (lbl);
        dlg.Add (tf);

        // Add buttons last so tab order is friendly
        // and TextField gets focus
        dlg.AddButton (btnOk);
        dlg.AddButton (btnCancel);

        app.Run (dlg);
        dlg.Dispose ();

        result = tf.Text;

        return confirm;
    }
}
