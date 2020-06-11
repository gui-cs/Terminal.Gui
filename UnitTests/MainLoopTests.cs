using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using Xunit;

// Alais Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui {
	public class MainLoopTests {
		[Fact]
		public void Init_Shutdown_Cleans_Up ()
		{
		}
	}
}
