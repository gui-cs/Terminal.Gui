using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class ViewKeyBindingTests {
	readonly ITestOutputHelper _output;

	public ViewKeyBindingTests (ITestOutputHelper output)
	{
		this._output = output;
	}

	// tests that test KeyBindingScope.Focus and KeyBindingScope.HotKey (tests for KeyBindingScope.Application are in Application/KeyboardTests.cs)

}