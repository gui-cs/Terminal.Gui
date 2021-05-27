using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit;

namespace Terminal.Gui.Core {
	public class ClipboardTests {
		[Fact]
		public void Contents_Gets_Sets ()
		{
			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var clipText = "This is a clipboard unit test.";
			Clipboard.Contents = clipText;

			Application.Iteration += () => Application.RequestStop ();

			Application.Run ();

			Assert.Equal (clipText, Clipboard.Contents);

			Application.Shutdown ();
		}

		[Fact]
		public void IsSupported_Get ()
		{
			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			if (Clipboard.IsSupported) {
				Assert.True (Clipboard.IsSupported);
			} else {
				Assert.False (Clipboard.IsSupported);
			}

			Application.Shutdown ();
		}

		[Fact]
		public void TryGetClipboardData_Gets_From_OS_Clipboard ()
		{
			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

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

			Application.Shutdown ();
		}

		[Fact]
		public void TrySetClipboardData_Sets_The_OS_Clipboard ()
		{
			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

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

			Application.Shutdown ();
		}

		[Fact]
		public void Contents_Gets_From_OS_Clipboard ()
		{
			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var clipText = "This is a clipboard unit test to get clipboard from OS.";
			var exit = false;

			Application.Iteration += () => {
				if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows)) {
					// using (Process clipExe = new Process {
					// 	StartInfo = new ProcessStartInfo {
					// 		RedirectStandardInput = true,
					// 		FileName = "clip"
					// 	}
					// }) {
					// 	clipExe.Start ();
					// 	clipExe.StandardInput.Write (clipText);
					// 	clipExe.StandardInput.Close ();
					// 	var result = clipExe.WaitForExit (500);
					// 	if (result) {
					// 		clipExe.WaitForExit ();
					// 	}
					// }

					using (Process pwsh = new Process {
						StartInfo = new ProcessStartInfo {
							FileName = "powershell",
							Arguments = $"-command \"Set-Clipboard -Value \\\"{clipText}\\\"\""
						}
					}) {
						pwsh.Start ();
						pwsh.WaitForExit ();
					}
				} else if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX)) {
					using (Process copy = new Process {
						StartInfo = new ProcessStartInfo {
							RedirectStandardInput = true,
							FileName = "pbcopy"
						}
					}) {
						copy.Start ();
						copy.StandardInput.Write (clipText);
						copy.StandardInput.Close ();
						copy.WaitForExit ();
					}
				} else if (RuntimeInformation.IsOSPlatform (OSPlatform.Linux)) {
					if (Is_WSL_Platform ()) {
						try {
							using (Process bash = new Process {
								StartInfo = new ProcessStartInfo {
									FileName = "powershell.exe",
									Arguments = $"-noprofile -command \"Set-Clipboard -Value \\\"{clipText}\\\"\""
								}
							}) {
								bash.Start ();
								bash.WaitForExit ();
							}

							//using (Process clipExe = new Process {
							//	StartInfo = new ProcessStartInfo {
							//		RedirectStandardInput = true,
							//		FileName = "clip.exe"
							//	}
							//}) {
							//	clipExe.Start ();
							//	clipExe.StandardInput.Write (clipText);
							//	clipExe.StandardInput.Close ();
							//	clipExe.WaitForExit ();
							//	//var result = clipExe.WaitForExit (500);
							//	//if (result) {
							//	//	clipExe.WaitForExit ();
							//	//}
							//}
						} catch {
							exit = true;
						}
						Application.RequestStop ();
						return;
					}
					if (exit = xclipExists () == false) {
						// xclip doesn't exist then exit.
						Application.RequestStop ();
						return;
					}

					using (Process bash = new Process {
						StartInfo = new ProcessStartInfo {
							FileName = "bash",
							Arguments = $"-c \"xclip -sel clip -i\"",
							RedirectStandardInput = true,
						}
					}) {
						bash.Start ();
						bash.StandardInput.Write (clipText);
						bash.StandardInput.Close ();
						bash.WaitForExit ();
					}
				}

				Application.RequestStop ();
			};

			Application.Run ();

			if (!exit) {
				Assert.Equal (clipText, Clipboard.Contents);
			}

			Application.Shutdown ();
		}

		[Fact]
		public void Contents_Sets_The_OS_Clipboard ()
		{
			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var clipText = "This is a clipboard unit test to set the OS clipboard.";
			var clipReadText = "";
			var exit = false;

			Application.Iteration += () => {
				Clipboard.Contents = clipText;

				if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows)) {
					using (Process pwsh = new Process {
						StartInfo = new ProcessStartInfo {
							RedirectStandardOutput = true,
							FileName = "powershell.exe",
							Arguments = "-noprofile -command \"Get-Clipboard\""
						}
					}) {
						pwsh.Start ();
						clipReadText = pwsh.StandardOutput.ReadToEnd ().TrimEnd ();
						pwsh.StandardOutput.Close ();
						pwsh.WaitForExit ();
					}
				} else if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX)) {
					using (Process paste = new Process {
						StartInfo = new ProcessStartInfo {
							RedirectStandardOutput = true,
							FileName = "pbpaste"
						}
					}) {
						paste.Start ();
						clipReadText = paste.StandardOutput.ReadToEnd ();
						paste.StandardOutput.Close ();
						paste.WaitForExit ();
					}
				} else if (RuntimeInformation.IsOSPlatform (OSPlatform.Linux)) {
					if (Is_WSL_Platform ()) {
						try {
							using (Process bash = new Process {
								StartInfo = new ProcessStartInfo {
									RedirectStandardOutput = true,
									FileName = "powershell.exe",
									Arguments = "-noprofile -command \"Get-Clipboard\""
								}
							}) {
								bash.Start ();
								clipReadText = bash.StandardOutput.ReadToEnd ();
								bash.StandardOutput.Close ();
								bash.WaitForExit ();
							}
						} catch {
							exit = true;
						}
						Application.RequestStop ();
					}
					if (exit = xclipExists () == false) {
						// xclip doesn't exist then exit.
						Application.RequestStop ();
					}

					using (Process bash = new Process {
						StartInfo = new ProcessStartInfo {
							RedirectStandardOutput = true,
							FileName = "bash",
							Arguments = $"-c \"xclip -sel clip -o\""
						}
					}) {
						bash.Start ();
						clipReadText = bash.StandardOutput.ReadToEnd ();
						bash.StandardOutput.Close ();
						bash.WaitForExit ();
					}
				}

				Application.RequestStop ();
			};

			Application.Run ();

			if (!exit) {
				Assert.Equal (clipText, clipReadText);
			}

			Application.Shutdown ();
		}

		bool Is_WSL_Platform ()
		{
			using (Process bash = new Process {
				StartInfo = new ProcessStartInfo {
					FileName = "bash",
					Arguments = $"-c \"uname -a\"",
					RedirectStandardOutput = true,
				}
			}) {
				bash.Start ();
				var result = bash.StandardOutput.ReadToEnd ();
				var isWSL = false;
				if (result.Contains ("microsoft") && result.Contains ("WSL")) {
					isWSL = true;
				}
				bash.StandardOutput.Close ();
				bash.WaitForExit ();
				return isWSL;
			}
		}

		bool xclipExists ()
		{
			try {
				using (Process bash = new Process {
					StartInfo = new ProcessStartInfo {
						FileName = "bash",
						Arguments = $"-c \"which xclip\"",
						RedirectStandardOutput = true,
					}
				}) {
					bash.Start ();
					bool exist = bash.StandardOutput.ReadToEnd ().TrimEnd () != "";
					bash.StandardOutput.Close ();
					bash.WaitForExit ();
					if (exist) {
						return true;
					}
				}
				return false;
			} catch (System.Exception) {
				return false;
			}
		}
	}
}
