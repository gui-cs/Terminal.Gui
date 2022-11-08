using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ConsoleDrivers {
	public class ClipboardTests {

		[Fact, AutoInitShutdown (useFakeClipboard: false)]
		public void Contents_Gets_Sets ()
		{
			var clipText = "This is a clipboard unit test.";
			Clipboard.Contents = clipText;

			Application.Iteration += () => Application.RequestStop ();
			Application.Run ();

			Assert.Equal (clipText, Clipboard.Contents);
		}

		[Fact, AutoInitShutdown (useFakeClipboard: false)]
		public void IsSupported_Get ()
		{
			if (Clipboard.IsSupported) {
				Assert.True (Clipboard.IsSupported);
			} else {
				Assert.False (Clipboard.IsSupported);
			}
		}

		[Fact, AutoInitShutdown (useFakeClipboard: false)]
		public void TryGetClipboardData_Gets_From_OS_Clipboard ()
		{
			var clipText = "Trying to get from the OS clipboard.";
			Clipboard.Contents = clipText;

			Application.Iteration += () => Application.RequestStop ();

			Application.Run ();

			if (Clipboard.IsSupported) {
				Assert.True (Clipboard.TryGetClipboardData (out string result));
				Assert.Equal (clipText, result);
			} else {
				Assert.False (Clipboard.TryGetClipboardData (out string result));
				Assert.NotEqual (clipText, result);
			}
		}

		[Fact, AutoInitShutdown (useFakeClipboard: false)]
		public void TrySetClipboardData_Sets_The_OS_Clipboard ()
		{
			var clipText = "Trying to set the OS clipboard.";
			if (Clipboard.IsSupported) {
				Assert.True (Clipboard.TrySetClipboardData (clipText));
			} else {
				Assert.False (Clipboard.TrySetClipboardData (clipText));
			}

			Application.Iteration += () => Application.RequestStop ();

			Application.Run ();

			if (Clipboard.IsSupported) {
				Assert.Equal (clipText, Clipboard.Contents);
			} else {
				Assert.NotEqual (clipText, Clipboard.Contents);
			}
		}

		private static string RunClipboardProcess (string cmd, string args, string writeText = null)
		{
			string output = string.Empty;

			using (Process process = new Process {
				StartInfo = new ProcessStartInfo {
					FileName = cmd,
					Arguments = args,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					RedirectStandardInput = true
				}
			}) {
				process.Start ();
				if (string.IsNullOrEmpty (writeText)) {
					process.StandardInput.Write (writeText);
					process.StandardInput.Close ();
				}
				process.WaitForExit ();

				if (process.ExitCode > 0) {
					var error = $@"RunClipboardProcess failed. Command line: {cmd} {args}.
										Output: {process.StandardOutput.ReadToEnd ()}
										Error: {process.StandardError.ReadToEnd ()}";
					throw new InvalidOperationException (error);
				}
				output = process.StandardOutput.ReadToEnd ().TrimEnd ();
				process.StandardOutput.Close ();
			}
			return output;
		}

		[Fact, AutoInitShutdown (useFakeClipboard: false)]
		public void Contents_Gets_From_OS_Clipboard ()
		{
			var clipText = "This is a clipboard unit test to get clipboard from OS.";
			var failed = false;
			var getClipText = "";

			Application.Iteration += () => {
				if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows)) {
					RunClipboardProcess ("pwsh", $"-command \"Set-Clipboard -Value \\\"{clipText}\\\"\"");
					getClipText = Clipboard.Contents.ToString ();

				} else if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX)) {
					RunClipboardProcess ("pbcopy", string.Empty, clipText);
					getClipText = Clipboard.Contents.ToString ();

				} else if (RuntimeInformation.IsOSPlatform (OSPlatform.Linux)) {
					if (Is_WSL_Platform ()) {
						try {
							RunClipboardProcess ("pwsh", $"-noprofile -command \"Set-Clipboard -Value \\\"{clipText}\\\"\"");
						} catch {
							failed = true;
						}
						if (!failed) {
							getClipText = Clipboard.Contents.ToString ();
						}
						Application.RequestStop ();
						return;
					}

					if (failed = xclipExists () == false) {
						// xclip doesn't exist then exit.
						Application.RequestStop ();
						return;
					}

					// If we get here, powershell didn't work and xclip exists...
					RunClipboardProcess ("bash", $"-c \"xclip -sel clip -i\"", clipText);

					if (!failed) {
						getClipText = Clipboard.Contents.ToString ();
					}
				}

				Application.RequestStop ();
			};

			Application.Run ();

			if (!failed) {
				Assert.Equal (clipText, getClipText);
			}
		}


		[Fact, AutoInitShutdown (useFakeClipboard: false)]
		public void Contents_Sets_The_OS_Clipboard ()
		{
			var clipText = "This is a clipboard unit test to set the OS clipboard.";
			var clipReadText = "";
			var failed = false;

			Application.Iteration += () => {
				Clipboard.Contents = clipText;

				if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows)) {
					clipReadText = RunClipboardProcess ("pwsh", "-noprofile -command \"Get-Clipboard\"");

				} else if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX)) {
					clipReadText = RunClipboardProcess ("pbpaste", "");

				} else if (RuntimeInformation.IsOSPlatform (OSPlatform.Linux)) {
					if (Is_WSL_Platform ()) {
						try {
							clipReadText = RunClipboardProcess ("/opt/microsoft/powershell/7/pwsh", "-noprofile -command \"Get-Clipboard\"");
						} catch {
							failed = true;
						}
						Application.RequestStop ();
					}
					if (failed = xclipExists () == false) {
						// xclip doesn't exist then exit.
						Application.RequestStop ();
						return;
					}

					clipReadText = RunClipboardProcess ("bash", $"-c \"xclip -sel clip -o\"");
				}

				Application.RequestStop ();
			};

			Application.Run ();

			if (!failed) {
				Assert.Equal (clipText, clipReadText);
			}

		}

		bool Is_WSL_Platform ()
		{
			var result = RunClipboardProcess ("bash", $"-c \"uname -a\"");
			return result.Contains ("microsoft") && result.Contains ("WSL");
		}

		bool xclipExists ()
		{
			try {
				var result = RunClipboardProcess ("bash", $"-c \"which xclip\"");
				return result.TrimEnd () != "";
			} catch (System.Exception) {
				return false;
			}
		}
	}
}
