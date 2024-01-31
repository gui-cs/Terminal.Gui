#region

using System.Runtime.InteropServices;
using Unix.Terminal;

#endregion

namespace Terminal.Gui;

/// <summary>
/// A clipboard implementation for Linux.
/// This implementation uses the xclip command to access the clipboard.
/// </summary>
/// <remarks>
/// If xclip is not installed, this implementation will not work.
/// </remarks>
class CursesClipboard : ClipboardBase {
    public CursesClipboard () { IsSupported = CheckSupport (); }
    string _xclipPath = string.Empty;

    public override bool IsSupported { get; }

    bool CheckSupport () {
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
        try {
            var (exitCode, result) = ClipboardProcessRunner.Bash ("which xclip", waitForOutput: true);
            if (exitCode == 0 && result.FileExists ()) {
                _xclipPath = result;

                return true;
            }
        }
        catch (Exception) {
            // Permissions issue.
        }
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
        return false;
    }

    protected override string GetClipboardDataImpl () {
        var tempFileName = Path.GetTempFileName ();
        var xclipargs = "-selection clipboard -o";

        try {
            var (exitCode, result) = ClipboardProcessRunner.Bash (
                                                                  $"{_xclipPath} {xclipargs} > {tempFileName}",
                                                                  waitForOutput: false);
            if (exitCode == 0) {
                if (Application.Driver is CursesDriver) {
                    Curses.raw ();
                    Curses.noecho ();
                }

                return File.ReadAllText (tempFileName);
            }
        }
        catch (Exception e) {
            throw new NotSupportedException ($"\"{_xclipPath} {xclipargs}\" failed.", e);
        }
        finally {
            File.Delete (tempFileName);
        }

        return string.Empty;
    }

    protected override void SetClipboardDataImpl (string text) {
        var xclipargs = "-selection clipboard -i";
        try {
            var (exitCode, _) = ClipboardProcessRunner.Bash ($"{_xclipPath} {xclipargs}", text, waitForOutput: false);
            if (exitCode == 0 && Application.Driver is CursesDriver) {
                Curses.raw ();
                Curses.noecho ();
            }
        }
        catch (Exception e) {
            throw new NotSupportedException ($"\"{_xclipPath} {xclipargs} < {text}\" failed", e);
        }
    }
}

/// <summary>
/// A clipboard implementation for MacOSX.
/// This implementation uses the Mac clipboard API (via P/Invoke) to copy/paste.
/// The existance of the Mac pbcopy and pbpaste commands
/// is used to determine if copy/paste is supported.
/// </summary>
class MacOSXClipboard : ClipboardBase {
    IntPtr _nsString = objc_getClass ("NSString");
    IntPtr _nsPasteboard = objc_getClass ("NSPasteboard");
    IntPtr _utfTextType;
    IntPtr _generalPasteboard;
    IntPtr _initWithUtf8Register = sel_registerName ("initWithUTF8String:");
    IntPtr _allocRegister = sel_registerName ("alloc");
    IntPtr _setStringRegister = sel_registerName ("setString:forType:");
    IntPtr _stringForTypeRegister = sel_registerName ("stringForType:");
    IntPtr _utf8Register = sel_registerName ("UTF8String");
    IntPtr _nsStringPboardType;
    IntPtr _generalPasteboardRegister = sel_registerName ("generalPasteboard");
    IntPtr _clearContentsRegister = sel_registerName ("clearContents");

    public MacOSXClipboard () {
        _utfTextType = objc_msgSend (
                                     objc_msgSend (_nsString, _allocRegister),
                                     _initWithUtf8Register,
                                     "public.utf8-plain-text");
        _nsStringPboardType = objc_msgSend (
                                            objc_msgSend (_nsString, _allocRegister),
                                            _initWithUtf8Register,
                                            "NSStringPboardType");
        _generalPasteboard = objc_msgSend (_nsPasteboard, _generalPasteboardRegister);
        IsSupported = CheckSupport ();
    }

    public override bool IsSupported { get; }

    bool CheckSupport () {
        var (exitCode, result) = ClipboardProcessRunner.Bash ("which pbcopy", waitForOutput: true);
        if (exitCode != 0 || !result.FileExists ()) {
            return false;
        }

        (exitCode, result) = ClipboardProcessRunner.Bash ("which pbpaste", waitForOutput: true);

        return exitCode == 0 && result.FileExists ();
    }

    protected override string GetClipboardDataImpl () {
        var ptr = objc_msgSend (_generalPasteboard, _stringForTypeRegister, _nsStringPboardType);
        var charArray = objc_msgSend (ptr, _utf8Register);

        return Marshal.PtrToStringAnsi (charArray);
    }

    protected override void SetClipboardDataImpl (string text) {
        IntPtr str = default;
        try {
            str = objc_msgSend (objc_msgSend (_nsString, _allocRegister), _initWithUtf8Register, text);
            objc_msgSend (_generalPasteboard, _clearContentsRegister);
            objc_msgSend (_generalPasteboard, _setStringRegister, str, _utfTextType);
        }
        finally {
            if (str != default) {
                objc_msgSend (str, sel_registerName ("release"));
            }
        }
    }

    [DllImport ("/System/Library/Frameworks/AppKit.framework/AppKit")]
    static extern IntPtr objc_getClass (string className);

    [DllImport ("/System/Library/Frameworks/AppKit.framework/AppKit")]
    static extern IntPtr objc_msgSend (IntPtr receiver, IntPtr selector);

    [DllImport ("/System/Library/Frameworks/AppKit.framework/AppKit")]
    static extern IntPtr objc_msgSend (IntPtr receiver, IntPtr selector, string arg1);

    [DllImport ("/System/Library/Frameworks/AppKit.framework/AppKit")]
    static extern IntPtr objc_msgSend (IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport ("/System/Library/Frameworks/AppKit.framework/AppKit")]
    static extern IntPtr objc_msgSend (IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

    [DllImport ("/System/Library/Frameworks/AppKit.framework/AppKit")]
    static extern IntPtr sel_registerName (string selectorName);
}

/// <summary>
/// A clipboard implementation for Linux, when running under WSL.
/// This implementation uses the Windows clipboard to store the data, and uses Windows'
/// powershell.exe (launched via WSL interop services) to set/get the Windows
/// clipboard.
/// </summary>
class WSLClipboard : ClipboardBase {
    public WSLClipboard () { }
    public override bool IsSupported { get { return CheckSupport (); } }
    private static string _powershellPath = string.Empty;

    bool CheckSupport () {
        if (string.IsNullOrEmpty (_powershellPath)) {
            // Specify pwsh.exe (not pwsh) to ensure we get the Windows version (invoked via WSL)
            var (exitCode, result) = ClipboardProcessRunner.Bash ("which pwsh.exe", waitForOutput: true);
            if (exitCode > 0) {
                (exitCode, result) = ClipboardProcessRunner.Bash ("which powershell.exe", waitForOutput: true);
            }

            if (exitCode == 0) {
                _powershellPath = result;
            }
        }

        return !string.IsNullOrEmpty (_powershellPath);
    }

    protected override string GetClipboardDataImpl () {
        if (!IsSupported) {
            return string.Empty;
        }

        var (exitCode, output) =
            ClipboardProcessRunner.Process (_powershellPath, "-noprofile -command \"Get-Clipboard\"");
        if (exitCode == 0) {
            if (Application.Driver is CursesDriver) {
                Curses.raw ();
                Curses.noecho ();
            }

            if (output.EndsWith ("\r\n")) {
                output = output.Substring (0, output.Length - 2);
            }

            return output;
        }

        return string.Empty;
    }

    protected override void SetClipboardDataImpl (string text) {
        if (!IsSupported) {
            return;
        }

        var (exitCode, output) = ClipboardProcessRunner.Process (
                                                                 _powershellPath,
                                                                 $"-noprofile -command \"Set-Clipboard -Value \\\"{text}\\\"\"");
        if (exitCode == 0) {
            if (Application.Driver is CursesDriver) {
                Curses.raw ();
                Curses.noecho ();
            }
        }
    }
}
