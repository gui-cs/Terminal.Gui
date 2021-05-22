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
			Assert.Equal (clipText, Clipboard.Contents);

			Application.Shutdown ();
		}

		[Fact]
		public void Contents_Gets_From_OS_Clipboard ()
		{
			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var clipText = "This is a clipboard unit test to get clipboard from OS.";

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

			Assert.Equal (clipText, Clipboard.Contents);

			Application.Shutdown ();
		}

		[Fact]
		public void Contents_Sets_The_OS_Clipboard ()
		{
			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var clipText = "This is a clipboard unit test to set the OS clipboard.";
			var clipReadText = "";

			Application.Iteration += () => {
				Clipboard.Contents = clipText;

				if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows)) {
					using (Process pwsh = new Process {
						StartInfo = new ProcessStartInfo {
							RedirectStandardOutput = true,
							FileName = "powershell.exe",
							Arguments = "-command \"Get-Clipboard\""
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

			Assert.Equal (clipText, clipReadText);

			Application.Shutdown ();
		}
	}
}
