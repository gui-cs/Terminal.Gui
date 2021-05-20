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
	}
}
