using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Terminal.Gui.FileServicesTests {
	public class FileSystemIconProviderTests {
		[Fact]
		public void FlagsShouldBeMutuallyExclusive()
		{
			var p = new FileSystemIconProvider {
				UseUnicodeCharacters = false,
				UseNerdIcons = false
			};

			Assert.False (p.UseUnicodeCharacters);
			Assert.False (p.UseNerdIcons);

			p.UseUnicodeCharacters = true;

			Assert.True (p.UseUnicodeCharacters);
			Assert.False (p.UseNerdIcons);

			// Cannot use both nerd and unicode so unicode should have switched off
			p.UseNerdIcons = true;

			Assert.True (p.UseNerdIcons);
			Assert.False (p.UseUnicodeCharacters);

			// Cannot use both unicode and nerd so now nerd should have switched off
			p.UseUnicodeCharacters = true;

			Assert.True (p.UseUnicodeCharacters);
			Assert.False (p.UseNerdIcons);
		}

		[Fact]
		public void TestBasicIcons ()
		{
			var p = new FileSystemIconProvider ();
			var fs = GetMockFileSystem ();
			
			Assert.Equal(IsWindows() ? new Rune('\\') : new Rune('/'), p.GetIcon(fs.DirectoryInfo.New(@"c:\")));

			Assert.Equal (new Rune (' '), p.GetIcon (fs.FileInfo.New (@"c:\myfile.txt")));
		}
		private bool IsWindows ()
		{
			return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform (System.Runtime.InteropServices.OSPlatform.Windows);
		}

		private IFileSystem GetMockFileSystem()
		{
			var fileSystem = new MockFileSystem (new Dictionary<string, MockFileData> (), @"c:\");
			
			fileSystem.AddFile (@"c:\myfile.txt", new MockFileData ("Testing is meh."));
			fileSystem.AddFile (@"c:\demo\jQuery.js", new MockFileData ("some js"));
			fileSystem.AddFile (@"c:\demo\mybinary.exe", new MockFileData ("some js"));
			fileSystem.AddFile (@"c:\demo\image.gif", new MockFileData (new byte [] { 0x12, 0x34, 0x56, 0xd2 }));

			var m = (MockDirectoryInfo)fileSystem.DirectoryInfo.New (@"c:\demo\subfolder");
			m.Create ();

			return fileSystem;
		}
	}
}
