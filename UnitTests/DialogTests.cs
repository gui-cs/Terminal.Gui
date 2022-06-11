using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Terminal.Gui;
using Xunit;
using System.Globalization;
using Xunit.Abstractions;
using NStack;

namespace Terminal.Gui.Views {

	public class DialogTests {
		readonly ITestOutputHelper output;

		public DialogTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		private void RunButtonTestDialog (string title, int width, Dialog.ButtonAlignments align, params Button [] btns)
		{
			var dlg = new Dialog (title, width, 3, btns) { ButtonAlignment = align };
			Application.End (Application.Begin (dlg));
		}

		[Fact]
		[AutoInitShutdown]
		public void ButtonAlignment_One ()
		{
			var d = ((FakeDriver)Application.Driver);

			var title = "1234";
			// E.g "|[ ok ]|"
			var btnText = "ok";
			var buttonRow = $"{d.VLine}   {d.LeftBracket} {btnText} {d.RightBracket}   {d.VLine}";
			var width = buttonRow.Length;
			var topRow = $"┌ {title} {new String (d.HLine.ToString () [0], width - title.Length - 4)}┐";
			var bottomRow = $"└{new String (d.HLine.ToString () [0], width - 2)}┘";

			d.SetBufferSize (width, 3);

			RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btnText));
			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);

			// Justify
			buttonRow = $"{d.VLine}   {d.LeftBracket} {btnText} {d.RightBracket}   {d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btnText));
			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);

			// Right
			buttonRow = $"{d.VLine}      {d.LeftBracket} {btnText} {d.RightBracket}{d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btnText));
			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);

			// Left
			buttonRow = $"{d.VLine}{d.LeftBracket} {btnText} {d.RightBracket}      {d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btnText));
			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
		}

		[Fact]
		[AutoInitShutdown]
		public void ButtonAlignment_Two ()
		{
			var d = ((FakeDriver)Application.Driver);

			var title = "1234";
			// E.g "|[ yes ][ no ]|"
			var btn1Text = "yes";
			var btn1 = $"{d.LeftBracket} {btn1Text} {d.RightBracket}";
			var btn2Text = "no";
			var btn2 = $"{d.LeftBracket} {btn2Text} {d.RightBracket}";

			var buttonRow = $@"{d.VLine} {btn1} {btn2} {d.VLine}";
			var width = buttonRow.Length;
			var topRow = $"┌ {title} {new String (d.HLine.ToString () [0], width - title.Length - 4)}┐";
			var bottomRow = $"└{new String (d.HLine.ToString () [0], width - 2)}┘";

			d.SetBufferSize (buttonRow.Length, 3);

			RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btn1Text), new Button (btn2Text));
			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);

			// Justify
			buttonRow = $@"{d.VLine}{btn1}   {btn2}{d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btn1Text), new Button (btn2Text));
			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);

			// Right
			buttonRow = $@"{d.VLine}  {btn1} {btn2}{d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btn1Text), new Button (btn2Text));
			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);

			// Left
			buttonRow = $@"{d.VLine}{btn1} {btn2}  {d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btn1Text), new Button (btn2Text));
			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
		}

		[Fact]
		[AutoInitShutdown]
		public void ButtonAlignment_Three ()
		{
			var d = ((FakeDriver)Application.Driver);

			var title = "1234";
			// E.g "|[ yes ][ no ][ maybe ]|"
			var btn1Text = "yes";
			var btn1 = $"{d.LeftBracket} {btn1Text} {d.RightBracket}";
			var btn2Text = "no";
			var btn2 = $"{d.LeftBracket} {btn2Text} {d.RightBracket}";
			var btn3Text = "maybe";
			var btn3 = $"{d.LeftBracket} {btn3Text} {d.RightBracket}";

			var buttonRow = $@"{d.VLine} {btn1} {btn2} {btn3} {d.VLine}";
			var width = buttonRow.Length;
			var topRow = $"┌ {title} {new String (d.HLine.ToString () [0], width - title.Length - 4)}┐";
			var bottomRow = $"└{new String (d.HLine.ToString () [0], width - 2)}┘";

			d.SetBufferSize (buttonRow.Length, 3);

			RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text));
			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);

			// Justify
			buttonRow = $@"{d.VLine}{btn1}  {btn2}  {btn3}{d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text));
			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);

			// Right
			buttonRow = $@"{d.VLine}  {btn1} {btn2} {btn3}{d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text));
			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);

			// Left
			buttonRow = $@"{d.VLine}{btn1} {btn2} {btn3}  {d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text));
			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
		}

		[Fact]
		[AutoInitShutdown]
		public void ButtonAlignment_Four ()
		{
			var d = ((FakeDriver)Application.Driver);

			var title = "1234";

			// E.g "|[ yes ][ no ][ maybe ]|"
			var btn1Text = "yes";
			var btn1 = $"{d.LeftBracket} {btn1Text} {d.RightBracket}";
			var btn2Text = "no";
			var btn2 = $"{d.LeftBracket} {btn2Text} {d.RightBracket}";
			var btn3Text = "maybe";
			var btn3 = $"{d.LeftBracket} {btn3Text} {d.RightBracket}";
			var btn4Text = "never";
			var btn4 = $"{d.LeftBracket} {btn4Text} {d.RightBracket}";

			var buttonRow = $"{d.VLine} {btn1} {btn2} {btn3} {btn4} {d.VLine}";
			var width = buttonRow.Length;
			var topRow = $"┌ {title} {new String (d.HLine.ToString () [0], width - title.Length - 4)}┐";
			var bottomRow = $"└{new String (d.HLine.ToString () [0], width - 2)}┘";
			d.SetBufferSize (buttonRow.Length, 3);

			// Default - Center
			RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);

			// Justify
			buttonRow = $"{d.VLine}{btn1}  {btn2}  {btn3} {btn4}{d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);

			// Right
			buttonRow = $"{d.VLine}  {btn1} {btn2} {btn3} {btn4}{d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);

			// Left
			buttonRow = $"{d.VLine}{btn1} {btn2} {btn3} {btn4}  {d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
		}

		[Fact]
		[AutoInitShutdown]
		public void ButtonAlignment_Four_Wider ()
		{
			var d = ((FakeDriver)Application.Driver);

			var title = "1234";

			// E.g "|[ yes ][ no ][ maybe ]|"
			var btn1Text = "yes";
			var btn1 = $"{d.LeftBracket} {btn1Text} {d.RightBracket}";
			var btn2Text = "no";
			var btn2 = $"{d.LeftBracket} {btn2Text} {d.RightBracket}";
			var btn3Text = "你你你你你"; // This is a wide char
			var btn3 = $"{d.LeftBracket} {btn3Text} {d.RightBracket}";
			// Requires a Nerd Font
			var btn4Text = "\uE36E\uE36F\uE370\uE371\uE372\uE373";
			var btn4 = $"{d.LeftBracket} {btn4Text} {d.RightBracket}";

			// Note extra spaces to make dialog even wider
			//                         123456                           12345
			var buttonRow = $"{d.VLine}      {btn1} {btn2} {btn3} {btn4}     {d.VLine}";
			var width = ustring.Make (buttonRow).ConsoleWidth;
			var topRow = $"┌ {title} {new String (d.HLine.ToString () [0], width - title.Length - 4)}┐";
			var bottomRow = $"└{new String (d.HLine.ToString () [0], width - 2)}┘";
			d.SetBufferSize (width, 3);

			// Default - Center
			RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);

			// Justify
			buttonRow = $"{d.VLine}{btn1}     {btn2}     {btn3}    {btn4}{d.VLine}";
			Assert.Equal (width, ustring.Make (buttonRow).ConsoleWidth);
			RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);

			// Right
			buttonRow = $"{d.VLine}           {btn1} {btn2} {btn3} {btn4}{d.VLine}";
			Assert.Equal (width, ustring.Make (buttonRow).ConsoleWidth);
			RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);

			// Left
			buttonRow = $"{d.VLine}{btn1} {btn2} {btn3} {btn4}           {d.VLine}";
			Assert.Equal (width, ustring.Make (buttonRow).ConsoleWidth);
			RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
		}

		[Fact]
		[AutoInitShutdown]
		public void ButtonAlignment_Four_WideOdd ()
		{
			var d = ((FakeDriver)Application.Driver);

			var title = "1234";

			// E.g "|[ yes ][ no ][ maybe ]|"
			var btn1Text = "really long button 1";
			var btn1 = $"{d.LeftBracket} {btn1Text} {d.RightBracket}";
			var btn2Text = "really long button 2";
			var btn2 = $"{d.LeftBracket} {btn2Text} {d.RightBracket}";
			var btn3Text = "really long button 3";
			var btn3 = $"{d.LeftBracket} {btn3Text} {d.RightBracket}";
			var btn4Text = "really long button 44"; // 44 is intentional to make length different than rest
			var btn4 = $"{d.LeftBracket} {btn4Text} {d.RightBracket}";

			// Note extra spaces to make dialog even wider
			//                         123456                           12345
			var buttonRow = $"{d.VLine}      {btn1} {btn2} {btn3} {btn4}     {d.VLine}";
			var width = buttonRow.Length;
			var topRow = $"┌ {title} {new String (d.HLine.ToString () [0], width - title.Length - 4)}┐";
			var bottomRow = $"└{new String (d.HLine.ToString () [0], width - 2)}┘";
			d.SetBufferSize (buttonRow.Length, 3);

			// Default - Center
			RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);

			// Justify
			buttonRow = $"{d.VLine}{btn1}     {btn2}     {btn3}    {btn4}{d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);

			// Right
			buttonRow = $"{d.VLine}           {btn1} {btn2} {btn3} {btn4}{d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);

			// Left
			buttonRow = $"{d.VLine}{btn1} {btn2} {btn3} {btn4}           {d.VLine}";
			Assert.Equal (width, buttonRow.Length);
			RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
			GraphViewTests.AssertDriverContentsWithFrameAre ($"{topRow}\n{buttonRow}\n{bottomRow}", output);
		}
	}
}