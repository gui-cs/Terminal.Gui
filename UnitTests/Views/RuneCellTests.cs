using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests {
	public class RuneCellTests {
		readonly ITestOutputHelper _output;

		public RuneCellTests (ITestOutputHelper output)
		{
			_output = output;
		}

		[Fact]
		public void Constructor_Defaults ()
		{
			var rc = new RuneCell ();
			Assert.NotNull (rc);
			Assert.Equal (0, rc.Rune.Value);
			Assert.Null (rc.ColorScheme);
		}

		[Fact]
		public void Equals_True ()
		{
			var rc1 = new RuneCell ();
			var rc2 = new RuneCell ();
			Assert.True (rc1.Equals (rc2));
			Assert.True (rc2.Equals (rc1));

			rc1.Rune = new Rune ('a');
			rc1.ColorScheme = new ColorScheme ();
			rc2.Rune = new Rune ('a');
			rc2.ColorScheme = new ColorScheme ();
			Assert.True (rc1.Equals (rc2));
			Assert.True (rc2.Equals (rc1));
		}

		[Fact]
		public void Equals_False ()
		{
			var rc1 = new RuneCell ();
			var rc2 = new RuneCell () {
				Rune = new Rune ('a'),
				ColorScheme = new ColorScheme () { Normal = new Attribute (Color.Red) }
			};
			Assert.False (rc1.Equals (rc2));
			Assert.False (rc2.Equals (rc1));

			rc1.Rune = new Rune ('a');
			rc1.ColorScheme = new ColorScheme ();
			Assert.Equal (rc1.Rune, rc2.Rune);
			Assert.False (rc1.Equals (rc2));
			Assert.False (rc2.Equals (rc1));
		}

		[Fact]
		public void ToString_Override ()
		{
			var rc1 = new RuneCell ();
			var rc2 = new RuneCell () {
				Rune = new Rune ('a'),
				ColorScheme = new ColorScheme () { Normal = new Attribute (Color.Red) }
			};
			Assert.Equal ("U+0000 '\0'; null", rc1.ToString ());
			Assert.Equal ("U+0061 'a'; Normal: Red,Red; Focus: White,Black; HotNormal: White,Black; HotFocus: White,Black; Disabled: White,Black", rc2.ToString ());
		}


		// TODO: Move the tests below to View or Color - they test ColorScheme, not RuneCell primitives.

		private TextView CreateTextView ()
		{
			return new TextView () { Width = 30, Height = 10 };
		}

		[Fact, AutoInitShutdown]
		public void RuneCell_LoadRuneCells_InheritsPreviousColorScheme ()
		{
			List<RuneCell> runeCells = new List<RuneCell> ();
			foreach (var color in Colors.ColorSchemes) {
				string csName = color.Key;
				foreach (var rune in csName.EnumerateRunes ()) {
					runeCells.Add (new RuneCell { Rune = rune, ColorScheme = color.Value });
				}
				runeCells.Add (new RuneCell { Rune = (Rune)'\n', ColorScheme = color.Value });
			}

			var tv = CreateTextView ();
			tv.Load (runeCells);
			Application.Top.Add (tv);
			var rs = Application.Begin (Application.Top);
			Assert.True (tv.InheritsPreviousColorScheme);
			var expectedText = @"
TopLevel
Base    
Dialog  
Menu    
Error   ";
			TestHelpers.AssertDriverContentsWithFrameAre (expectedText, _output);

			var attributes = new Attribute [] {
				// 0
				Colors.ColorSchemes ["TopLevel"].Focus,
				// 1
				Colors.ColorSchemes ["Base"].Focus,
				// 2
				Colors.ColorSchemes ["Dialog"].Focus,
				// 3
				Colors.ColorSchemes ["Menu"].Focus,
				// 4
				Colors.ColorSchemes ["Error"].Focus
			};
			var expectedColor = @"
0000000000
1111000000
2222220000
3333000000
4444400000";
			TestHelpers.AssertDriverAttributesAre (expectedColor, driver: Application.Driver, attributes);

			tv.WordWrap = true;
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (expectedText, _output);
			TestHelpers.AssertDriverAttributesAre (expectedColor, driver: Application.Driver, attributes);

			tv.CursorPosition = new Point (6, 2);
			tv.SelectionStartColumn = 0;
			tv.SelectionStartRow = 0;
			Assert.Equal ($"TopLevel{Environment.NewLine}Base{Environment.NewLine}Dialog", tv.SelectedText);
			tv.Copy ();
			tv.Selecting = false;
			tv.CursorPosition = new Point (2, 4);
			tv.Paste ();
			Application.Refresh ();
			expectedText = @"
TopLevel  
Base      
Dialog    
Menu      
ErTopLevel
Base      
Dialogror ";
			TestHelpers.AssertDriverContentsWithFrameAre (expectedText, _output);
			expectedColor = @"
0000000000
1111000000
2222220000
3333000000
4444444444
4444000000
4444444440";
			TestHelpers.AssertDriverAttributesAre (expectedColor, driver: Application.Driver, attributes);

			tv.Undo ();
			tv.CursorPosition = new Point (0, 3);
			tv.SelectionStartColumn = 0;
			tv.SelectionStartRow = 0;
			Assert.Equal ($"TopLevel{Environment.NewLine}Base{Environment.NewLine}Dialog{Environment.NewLine}", tv.SelectedText);
			tv.Copy ();
			tv.Selecting = false;
			tv.CursorPosition = new Point (2, 4);
			tv.Paste ();
			Application.Refresh ();
			expectedText = @"
TopLevel  
Base      
Dialog    
Menu      
ErTopLevel
Base      
Dialog    
ror       ";
			TestHelpers.AssertDriverContentsWithFrameAre (expectedText, _output);
			expectedColor = @"
0000000000
1111000000
2222220000
3333000000
4444444444
4444000000
4444440000
4440000000";
			TestHelpers.AssertDriverAttributesAre (expectedColor, driver: Application.Driver, attributes);

			Application.End (rs);
		}

		[Fact, AutoInitShutdown]
		public void RuneCell_LoadRuneCells_Without_ColorScheme_Is_Never_Null ()
		{
			var cells = new List<RuneCell> {
				new RuneCell{Rune = new Rune ('T')},
				new RuneCell{Rune = new Rune ('e')},
				new RuneCell{Rune = new Rune ('s')},
				new RuneCell{Rune = new Rune ('t')}
			};
			var tv = CreateTextView ();
			Application.Top.Add (tv);
			tv.Load (cells);

			for (int i = 0; i < tv.Lines; i++) {
				var line = tv.GetLine (i);
				foreach (var rc in line) {
					Assert.NotNull (rc.ColorScheme);
				}
			}
		}

		[Fact, AutoInitShutdown]
		public void RuneCellEventArgs_WordWrap_True ()
		{
			var eventCount = 0;
			var text = new List<List<RuneCell>> () { TextModel.ToRuneCells ("This is the first line.".ToRunes ()), TextModel.ToRuneCells ("This is the second line.".ToRunes ()) };
			var tv = CreateTextView ();
			tv.DrawNormalColor += _textView_DrawColor;
			tv.DrawReadOnlyColor += _textView_DrawColor;
			tv.DrawSelectionColor += _textView_DrawColor;
			tv.DrawUsedColor += _textView_DrawColor;
			void _textView_DrawColor (object sender, RuneCellEventArgs e)
			{
				Assert.Equal (e.Line [e.Col], text [e.UnwrappedPosition.Row] [e.UnwrappedPosition.Col]);
				eventCount++;
			}
			tv.Text = $"{TextModel.ToString (text [0])}\n{TextModel.ToString (text [1])}\n";
			Assert.False (tv.WordWrap);
			Application.Top.Add (tv);
			Application.Begin (Application.Top);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This is the first line. 
This is the second line.", _output);

			tv.Width = 10;
			tv.Height = 25;
			tv.WordWrap = true;
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This is
the    
first  
line.  
This is
the    
second 
line.  ", _output);

			Assert.Equal (eventCount, (text [0].Count + text [1].Count) * 2);
		}
	}
}
