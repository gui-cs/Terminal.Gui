using System.IO.Abstractions;

namespace Terminal.Gui;

internal class FileDialogHistory {
    public FileDialogHistory (FileDialog dlg) { this.dlg = dlg; }
    private readonly FileDialog dlg;
    private readonly Stack<FileDialogState> back = new ();
    private readonly Stack<FileDialogState> forward = new ();

    public bool Back () {
        IDirectoryInfo goTo = null;
        FileSystemInfoStats restoreSelection = null;
        string restorePath = null;

        if (CanBack ()) {
            FileDialogState backTo = back.Pop ();
            goTo = backTo.Directory;
            restoreSelection = backTo.Selected;
            restorePath = backTo.Path;
        } else if (CanUp ()) {
            goTo = dlg.State?.Directory.Parent;
        }

        // nowhere to go
        if (goTo == null) {
            return false;
        }

        forward.Push (dlg.State);
        dlg.PushState (goTo, false, true, false, restorePath);

        if (restoreSelection != null) {
            dlg.RestoreSelection (restoreSelection.FileSystemInfo);
        }

        return true;
    }

    internal bool CanBack () => back.Count > 0;
    internal bool CanForward () => forward.Count > 0;
    internal bool CanUp () => dlg.State?.Directory.Parent != null;
    internal void ClearForward () { forward.Clear (); }

    internal bool Forward () {
        if (forward.Count > 0) {
            dlg.PushState (forward.Pop ().Directory, true, true, false);

            return true;
        }

        return false;
    }

    internal void Push (FileDialogState state, bool clearForward) {
        if (state == null) {
            return;
        }

        // if changing to a new directory push onto the Back history
        if (back.Count == 0 || back.Peek ().Directory.FullName != state.Directory.FullName) {
            back.Push (state);
            if (clearForward) {
                ClearForward ();
            }
        }
    }

    internal bool Up () {
        IDirectoryInfo parent = dlg.State?.Directory.Parent;
        if (parent != null) {
            back.Push (new FileDialogState (parent, dlg));
            dlg.PushState (parent, false);

            return true;
        }

        return false;
    }
}
