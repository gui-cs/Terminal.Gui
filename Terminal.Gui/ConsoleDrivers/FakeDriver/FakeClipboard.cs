//
// FakeDriver.cs: A fake ConsoleDriver for unit tests. 
//
using System;

// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui {

	public class FakeClipboard : ClipboardBase {
		public Exception FakeException = null;

		string contents = string.Empty;

		bool isSupportedAlwaysFalse = false;

		public override bool IsSupported => !isSupportedAlwaysFalse;

		public FakeClipboard (bool fakeClipboardThrowsNotSupportedException = false, bool isSupportedAlwaysFalse = false)
		{
			this.isSupportedAlwaysFalse = isSupportedAlwaysFalse;
			if (fakeClipboardThrowsNotSupportedException) {
				FakeException = new NotSupportedException ("Fake clipboard exception");
			}
		}

		protected override string GetClipboardDataImpl ()
		{
			if (FakeException != null) {
				throw FakeException;
			}
			return contents;
		}

		protected override void SetClipboardDataImpl (string text)
		{
			if (FakeException != null) {
				throw FakeException;
			}
			contents = text;
		}
	}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

}