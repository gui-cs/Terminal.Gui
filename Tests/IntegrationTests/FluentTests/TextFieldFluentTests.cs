using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerminalGuiFluentTesting;
using TerminalGuiFluentTestingXunit;
using Xunit.Abstractions;

namespace IntegrationTests.FluentTests;
public class TextFieldFluentTests
{
    private readonly TextWriter _out;

    public TextFieldFluentTests (ITestOutputHelper outputHelper)
    {
        _out = new TestOutputWriter (outputHelper);
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void TextField_Cursor_AtEnd_WhenTyping (V2TestDriver d)
    {
        // Simulates typing abcd into a TextField with width 3 (wide enough to render 2 characters only)
        using var c = With.A<Window> (100, 20, d)
                          .Add (new TextField () { Width = 3 })
                          .Focus<TextField> ()
                          .WaitIteration ()
                          .AssertCursorPosition (new Point (1, 1)) // Initial cursor position (because Window has border)
                          .RaiseKeyDownEvent (Key.A)
                          .WaitIteration ()
                          .ScreenShot ("After typing first letter", _out)
                          .AssertCursorPosition (new Point (2, 1)) // Cursor moves along as letter is pressed
                          .RaiseKeyDownEvent (Key.B)
                          .WaitIteration ()
                          .AssertCursorPosition (new Point (3, 1)) // Cursor moves along as letter is pressed
                          .RaiseKeyDownEvent (Key.C)
                          .WaitIteration ()
                          .ScreenShot ("After typing all letters",_out)
                          .AssertCursorPosition (new Point (3, 1)) // Cursor stays where it is because we are at end of TextField
                          .RaiseKeyDownEvent (Key.D)
                          .WaitIteration ()
                          .ScreenShot ("Typing one more letter", _out)
                          .AssertCursorPosition (new Point (3, 1)) // Cursor still stays at end of TextField
                          .WriteOutLogs (_out)
                          .Stop ();
    }
}
