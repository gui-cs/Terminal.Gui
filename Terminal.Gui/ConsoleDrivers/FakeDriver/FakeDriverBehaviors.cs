//
// FakeDriver.cs: A fake ConsoleDriver for unit tests. 
//
using System.Diagnostics;

// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui {
	public class FakeDriverBehaviors {

		public bool UseFakeClipboard { get; internal set; }
		public bool FakeClipboardAlwaysThrowsNotSupportedException { get; internal set; }
		public bool FakeClipboardIsSupportedAlwaysFalse { get; internal set; }

		public FakeDriverBehaviors (bool useFakeClipboard = false, bool fakeClipboardAlwaysThrowsNotSupportedException = false, bool fakeClipboardIsSupportedAlwaysTrue = false)
		{
			UseFakeClipboard = useFakeClipboard;
			FakeClipboardAlwaysThrowsNotSupportedException = fakeClipboardAlwaysThrowsNotSupportedException;
			FakeClipboardIsSupportedAlwaysFalse = fakeClipboardIsSupportedAlwaysTrue;

			// double check usage is correct
			Debug.Assert (useFakeClipboard == false && fakeClipboardAlwaysThrowsNotSupportedException == false);
			Debug.Assert (useFakeClipboard == false && fakeClipboardIsSupportedAlwaysTrue == false);
		}
	}
}