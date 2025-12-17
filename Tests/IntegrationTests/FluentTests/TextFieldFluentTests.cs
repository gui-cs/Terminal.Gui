using System.Drawing;
using IntegrationTests.FluentTests;
using TerminalGuiFluentTesting;
using TerminalGuiFluentTestingXunit;
using Xunit.Abstractions;

namespace IntegrationTests.TextFieldTests;

public class TextFieldFluentTests (ITestOutputHelper outputHelper)
{
    private readonly TextWriter _out = new TestOutputWriter (outputHelper);

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void TextField_Cursor_AtEnd_WhenTyping (TestDriver d)
    {
        // Simulates typing abcd into a TextField with width 3 (wide enough to render 2 characters only)
        using GuiTestContext c = With.A<Window> (100, 20, d, _out)
                                     .Add (new TextField { Width = 3 })
                                     .Focus<TextField> ()
                                     .WaitIteration ()
                                     .AssertCursorPosition (new (1, 1)) // Initial cursor position (because Window has border)
                                     .KeyDown (Key.A)
                                     .WaitIteration ()
                                     .ScreenShot ("After typing first letter", _out)
                                     .AssertCursorPosition (new Point (2, 1)) // Cursor moves along as letter is pressed
                                     .KeyDown (Key.B)
                                     .WaitIteration ()
                                     .AssertCursorPosition (new Point (3, 1)) // Cursor moves along as letter is pressed
                                     .KeyDown (Key.C)
                                     .WaitIteration ()
                                     .ScreenShot ("After typing all letters", _out)
                                     .AssertCursorPosition (new Point (3, 1)) // Cursor stays where it is because we are at end of TextField
                                     .KeyDown (Key.D)
                                     .WaitIteration ()
                                     .ScreenShot ("Typing one more letter", _out)
                                     .AssertCursorPosition (new Point (3, 1)) // Cursor still stays at end of TextField
                                     .WriteOutLogs (_out)
                                     .Stop ()
            ;
    }
}
