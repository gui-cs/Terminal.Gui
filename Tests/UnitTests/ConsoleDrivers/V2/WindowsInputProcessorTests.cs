using System.Collections.Concurrent;
using Terminal.Gui.ConsoleDrivers;
using InputRecord = Terminal.Gui.WindowsConsole.InputRecord;
using ButtonState = Terminal.Gui.WindowsConsole.ButtonState;
using MouseEventRecord = Terminal.Gui.WindowsConsole.MouseEventRecord;

namespace UnitTests.ConsoleDrivers.V2;

public class WindowsInputProcessorTests
{
    [Fact]
    public void Test_ProcessQueue_CapitalHLowerE ()
    {
        ConcurrentQueue<InputRecord> queue = new ();

        queue.Enqueue (
                       new()
                       {
                           EventType = WindowsConsole.EventType.Key,
                           KeyEvent = new()
                           {
                               bKeyDown = true,
                               UnicodeChar = 'H',
                               dwControlKeyState = WindowsConsole.ControlKeyState.CapslockOn,
                               wVirtualKeyCode = (ConsoleKeyMapping.VK)72,
                               wVirtualScanCode = 35
                           }
                       });

        queue.Enqueue (
                       new()
                       {
                           EventType = WindowsConsole.EventType.Key,
                           KeyEvent = new()
                           {
                               bKeyDown = false,
                               UnicodeChar = 'H',
                               dwControlKeyState = WindowsConsole.ControlKeyState.CapslockOn,
                               wVirtualKeyCode = (ConsoleKeyMapping.VK)72,
                               wVirtualScanCode = 35
                           }
                       });

        queue.Enqueue (
                       new()
                       {
                           EventType = WindowsConsole.EventType.Key,
                           KeyEvent = new()
                           {
                               bKeyDown = true,
                               UnicodeChar = 'i',
                               dwControlKeyState = WindowsConsole.ControlKeyState.NoControlKeyPressed,
                               wVirtualKeyCode = (ConsoleKeyMapping.VK)73,
                               wVirtualScanCode = 23
                           }
                       });

        queue.Enqueue (
                       new()
                       {
                           EventType = WindowsConsole.EventType.Key,
                           KeyEvent = new()
                           {
                               bKeyDown = false,
                               UnicodeChar = 'i',
                               dwControlKeyState = WindowsConsole.ControlKeyState.NoControlKeyPressed,
                               wVirtualKeyCode = (ConsoleKeyMapping.VK)73,
                               wVirtualScanCode = 23
                           }
                       });

        var processor = new WindowsInputProcessor (queue);

        List<Key> ups = new ();
        List<Key> downs = new ();

        processor.KeyUp += (s, e) => { ups.Add (e); };
        processor.KeyDown += (s, e) => { downs.Add (e); };

        Assert.Empty (ups);
        Assert.Empty (downs);

        processor.ProcessQueue ();

        Assert.Equal (Key.H.WithShift, ups [0]);
        Assert.Equal (Key.H.WithShift, downs [0]);
        Assert.Equal (Key.I, ups [1]);
        Assert.Equal (Key.I, downs [1]);
    }

    [Fact]
    public void Test_ProcessQueue_Mouse_Move ()
    {
        ConcurrentQueue<InputRecord> queue = new ();

        queue.Enqueue (
                       new()
                       {
                           EventType = WindowsConsole.EventType.Mouse,
                           MouseEvent = new()
                           {
                               MousePosition = new (32, 31),
                               ButtonState = ButtonState.NoButtonPressed,
                               ControlKeyState = WindowsConsole.ControlKeyState.NoControlKeyPressed,
                               EventFlags = WindowsConsole.EventFlags.MouseMoved
                           }
                       });

        var processor = new WindowsInputProcessor (queue);

        List<MouseEventArgs> mouseEvents = new ();

        processor.MouseEvent += (s, e) => { mouseEvents.Add (e); };

        Assert.Empty (mouseEvents);

        processor.ProcessQueue ();

        MouseEventArgs s = Assert.Single (mouseEvents);
        Assert.Equal (MouseFlags.ReportMousePosition, s.Flags);
        Assert.Equal (s.ScreenPosition, new (32, 31));
    }

    [Theory]
    [InlineData (ButtonState.Button1Pressed, MouseFlags.Button1Pressed)]
    [InlineData (ButtonState.Button2Pressed, MouseFlags.Button2Pressed)]
    [InlineData (ButtonState.Button3Pressed, MouseFlags.Button3Pressed)]
    [InlineData (ButtonState.Button4Pressed, MouseFlags.Button4Pressed)]
    internal void Test_ProcessQueue_Mouse_Pressed (ButtonState state, MouseFlags expectedFlag)
    {
        ConcurrentQueue<InputRecord> queue = new ();

        queue.Enqueue (
                       new()
                       {
                           EventType = WindowsConsole.EventType.Mouse,
                           MouseEvent = new()
                           {
                               MousePosition = new (32, 31),
                               ButtonState = state,
                               ControlKeyState = WindowsConsole.ControlKeyState.NoControlKeyPressed,
                               EventFlags = WindowsConsole.EventFlags.MouseMoved
                           }
                       });

        var processor = new WindowsInputProcessor (queue);

        List<MouseEventArgs> mouseEvents = new ();

        processor.MouseEvent += (s, e) => { mouseEvents.Add (e); };

        Assert.Empty (mouseEvents);

        processor.ProcessQueue ();

        MouseEventArgs s = Assert.Single (mouseEvents);
        Assert.Equal (s.Flags, MouseFlags.ReportMousePosition | expectedFlag);
        Assert.Equal (s.ScreenPosition, new (32, 31));
    }

    [Theory]
    [InlineData (100, MouseFlags.WheeledUp)]
    [InlineData (-100, MouseFlags.WheeledDown)]
    internal void Test_ProcessQueue_Mouse_Wheel (int wheelValue, MouseFlags expectedFlag)
    {
        ConcurrentQueue<InputRecord> queue = new ();

        queue.Enqueue (
                       new()
                       {
                           EventType = WindowsConsole.EventType.Mouse,
                           MouseEvent = new()
                           {
                               MousePosition = new (32, 31),
                               ButtonState = (ButtonState)wheelValue,
                               ControlKeyState = WindowsConsole.ControlKeyState.NoControlKeyPressed,
                               EventFlags = WindowsConsole.EventFlags.MouseWheeled
                           }
                       });

        var processor = new WindowsInputProcessor (queue);

        List<MouseEventArgs> mouseEvents = new ();

        processor.MouseEvent += (s, e) => { mouseEvents.Add (e); };

        Assert.Empty (mouseEvents);

        processor.ProcessQueue ();

        MouseEventArgs s = Assert.Single (mouseEvents);
        Assert.Equal (s.Flags, expectedFlag);
        Assert.Equal (s.ScreenPosition, new (32, 31));
    }

    public static IEnumerable<object []> MouseFlagTestData ()
    {
        yield return new object []
        {
            new []
            {
                Tuple.Create (ButtonState.Button1Pressed, MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.NoButtonPressed, MouseFlags.Button1Released | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.NoButtonPressed, MouseFlags.ReportMousePosition | MouseFlags.ReportMousePosition)
            }
        };

        yield return new object []
        {
            new []
            {
                Tuple.Create (ButtonState.Button2Pressed, MouseFlags.Button2Pressed | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.NoButtonPressed, MouseFlags.Button2Released | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.NoButtonPressed, MouseFlags.ReportMousePosition | MouseFlags.ReportMousePosition)
            }
        };

        yield return new object []
        {
            new []
            {
                Tuple.Create (ButtonState.Button3Pressed, MouseFlags.Button3Pressed | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.NoButtonPressed, MouseFlags.Button3Released | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.NoButtonPressed, MouseFlags.ReportMousePosition | MouseFlags.ReportMousePosition)
            }
        };

        yield return new object []
        {
            new []
            {
                Tuple.Create (ButtonState.Button4Pressed, MouseFlags.Button4Pressed | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.NoButtonPressed, MouseFlags.Button4Released | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.NoButtonPressed, MouseFlags.ReportMousePosition | MouseFlags.ReportMousePosition)
            }
        };

        yield return new object []
        {
            new []
            {
                Tuple.Create (ButtonState.RightmostButtonPressed, MouseFlags.Button3Pressed | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.NoButtonPressed, MouseFlags.Button3Released | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.NoButtonPressed, MouseFlags.ReportMousePosition | MouseFlags.ReportMousePosition)
            }
        };

        // Tests for holding down 2 buttons at once and releasing them one after the other
        yield return new object []
        {
            new []
            {
                Tuple.Create (
                              ButtonState.Button1Pressed | ButtonState.Button2Pressed,
                              MouseFlags.Button1Pressed | MouseFlags.Button2Pressed | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.Button1Pressed, MouseFlags.Button1Pressed | MouseFlags.Button2Released | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.NoButtonPressed, MouseFlags.Button1Released | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.NoButtonPressed, MouseFlags.ReportMousePosition)
            }
        };

        yield return new object []
        {
            new []
            {
                Tuple.Create (
                              ButtonState.Button3Pressed | ButtonState.Button4Pressed,
                              MouseFlags.Button3Pressed | MouseFlags.Button4Pressed | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.Button3Pressed, MouseFlags.Button3Pressed | MouseFlags.Button4Released | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.NoButtonPressed, MouseFlags.Button3Released | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.NoButtonPressed, MouseFlags.ReportMousePosition)
            }
        };

        // Test for holding down 2 buttons at once and releasing them simultaneously
        yield return new object []
        {
            new []
            {
                Tuple.Create (
                              ButtonState.Button1Pressed | ButtonState.Button2Pressed,
                              MouseFlags.Button1Pressed | MouseFlags.Button2Pressed | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.NoButtonPressed, MouseFlags.Button1Released | MouseFlags.Button2Released | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.NoButtonPressed, MouseFlags.ReportMousePosition)
            }
        };

        // Test that rightmost and button 3 are the same button so 2 states is still only 1 flag
        yield return new object []
        {
            new []
            {
                Tuple.Create (ButtonState.Button3Pressed | ButtonState.RightmostButtonPressed, MouseFlags.Button3Pressed | MouseFlags.ReportMousePosition),

                // Can swap between without raising the released
                Tuple.Create (ButtonState.Button3Pressed, MouseFlags.Button3Pressed | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.RightmostButtonPressed, MouseFlags.Button3Pressed | MouseFlags.ReportMousePosition),

                // Now with neither we get released
                Tuple.Create (ButtonState.NoButtonPressed, MouseFlags.Button3Released | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.NoButtonPressed, MouseFlags.ReportMousePosition)
            }
        };
    }

    [Theory]
    [MemberData (nameof (MouseFlagTestData))]
    internal void MouseFlags_Should_Map_Correctly (Tuple<ButtonState, MouseFlags> [] inputOutputPairs)
    {
        var processor = new WindowsInputProcessor (new ());

        foreach (Tuple<ButtonState, MouseFlags> pair in inputOutputPairs)
        {
            var mockEvent = new MouseEventRecord { ButtonState = pair.Item1 };
            MouseEventArgs result = processor.ToDriverMouse (mockEvent);

            Assert.Equal (pair.Item2, result.Flags);
        }
    }
}
