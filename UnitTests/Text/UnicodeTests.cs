using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using Xunit;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.TextTests {
	public class UnicodeTests {
		readonly ITestOutputHelper output;

		public UnicodeTests (ITestOutputHelper output)
		{
			this.output = output;
		}
		
		[Fact, AutoInitShutdown]
		public void AddRune_On_Clip_Left_Or_Right_Replace_Previous_Or_Next_Wide_Rune_With_Space ()
		{
			var tv = new TextView () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Text = @"これは広いルーンラインです。
これは広いルーンラインです。
これは広いルーンラインです。
これは広いルーンラインです。
これは広いルーンラインです。
これは広いルーンラインです。
これは広いルーンラインです。
これは広いルーンラインです。"
			};
			var win = new Window ("ワイドルーン") { Width = Dim.Fill (), Height = Dim.Fill () };
			win.Add (tv);
			Application.Top.Add (win);
			var lbl = new Label ("ワイドルーン。");
			var dg = new Dialog ("テスト", 14, 4, new Button ("選ぶ"));
			dg.Add (lbl);
			Application.Begin (Application.Top);
			Application.Begin (dg);
			((FakeDriver)Application.Driver).SetBufferSize (30, 10);

			var expected = @"
┌┤ワイドルーン├──────────────┐
│これは広いルーンラインです。│
│これは広いルーンラインです。│
│これは ┌┤テスト├────┐ です。│
│これは │ワイドルーン│ です。│
│これは │  [ 選ぶ ]  │ です。│
│これは └────────────┘ です。│
│これは広いルーンラインです。│
│これは広いルーンラインです。│
└────────────────────────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 30, 10), pos);
		}
	}
}