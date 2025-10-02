using System.Collections.Concurrent;
using InputRecord = Terminal.Gui.Drivers.WindowsConsole.InputRecord;
using ButtonState = Terminal.Gui.Drivers.WindowsConsole.ButtonState;
using EventFlags = Terminal.Gui.Drivers.WindowsConsole.EventFlags;
using ControlKeyState = Terminal.Gui.Drivers.WindowsConsole.ControlKeyState;
using MouseEventRecord = Terminal.Gui.Drivers.WindowsConsole.MouseEventRecord;

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
                               EventFlags = EventFlags.MouseMoved
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
                               EventFlags = EventFlags.MouseMoved
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
                Tuple.Create (ButtonState.Button1Pressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.Button1Pressed),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.Button1Released),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.None)
            }
        };

        yield return new object []
        {
            new []
            {
                Tuple.Create (ButtonState.Button2Pressed, EventFlags.MouseMoved, ControlKeyState.NoControlKeyPressed, MouseFlags.Button2Pressed | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.Button2Released),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.None)
            }
        };

        yield return new object []
        {
            new []
            {
                Tuple.Create (ButtonState.Button3Pressed, EventFlags.MouseMoved, ControlKeyState.NoControlKeyPressed, MouseFlags.Button3Pressed | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.Button3Released),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.None)
            }
        };

        yield return new object []
        {
            new []
            {
                Tuple.Create (ButtonState.Button4Pressed, EventFlags.MouseMoved, ControlKeyState.NoControlKeyPressed, MouseFlags.Button4Pressed | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.Button4Released),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.MouseMoved, ControlKeyState.NoControlKeyPressed, MouseFlags.ReportMousePosition)
            }
        };

        yield return new object []
        {
            new []
            {
                Tuple.Create (ButtonState.RightmostButtonPressed, EventFlags.MouseMoved, ControlKeyState.NoControlKeyPressed, MouseFlags.Button3Pressed | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.Button3Released),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.None)
            }
        };

        // Tests for holding down 2 buttons at once and releasing them one after the other
        yield return new object []
        {
            new []
            {
                Tuple.Create (
                              ButtonState.Button1Pressed | ButtonState.Button2Pressed, EventFlags.MouseMoved, ControlKeyState.NoControlKeyPressed,
                              MouseFlags.Button1Pressed | MouseFlags.Button2Pressed | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.Button1Pressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.Button1Pressed | MouseFlags.Button2Released),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.Button1Released),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.None)
            }
        };

        yield return new object []
        {
            new []
            {
                Tuple.Create (
                              ButtonState.Button3Pressed | ButtonState.Button4Pressed, EventFlags.MouseMoved, ControlKeyState.NoControlKeyPressed,
                              MouseFlags.Button3Pressed | MouseFlags.Button4Pressed | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.Button3Pressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.Button3Pressed | MouseFlags.Button4Released),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.Button3Released),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.None)
            }
        };

        // Test for holding down 2 buttons at once and releasing them simultaneously
        yield return new object []
        {
            new []
            {
                Tuple.Create (
                              ButtonState.Button1Pressed | ButtonState.Button2Pressed, EventFlags.MouseMoved, ControlKeyState.NoControlKeyPressed,
                              MouseFlags.Button1Pressed | MouseFlags.Button2Pressed | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.Button1Released | MouseFlags.Button2Released),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.None)
            }
        };

        // Test that rightmost and button 3 are the same button so 2 states is still only 1 flag
        yield return new object []
        {
            new []
            {
                Tuple.Create (ButtonState.Button3Pressed | ButtonState.RightmostButtonPressed, EventFlags.MouseMoved, ControlKeyState.NoControlKeyPressed,
                              MouseFlags.Button3Pressed | MouseFlags.ReportMousePosition),

                // Can swap between without raising the released
                Tuple.Create (ButtonState.Button3Pressed, EventFlags.MouseMoved, ControlKeyState.NoControlKeyPressed, MouseFlags.Button3Pressed | MouseFlags.ReportMousePosition),
                Tuple.Create (ButtonState.RightmostButtonPressed, EventFlags.MouseMoved, ControlKeyState.NoControlKeyPressed, MouseFlags.Button3Pressed | MouseFlags.ReportMousePosition),

                // Now with neither we get released
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.Button3Released),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.None)
            }
        };

        // Test for ControlKeyState buttons pressed and handled
        yield return new object []
        {
            new []
            {
                Tuple.Create (ButtonState.Button1Pressed, EventFlags.NoEvent, ControlKeyState.LeftAltPressed, MouseFlags.Button1Pressed | MouseFlags.ButtonAlt),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.LeftAltPressed, MouseFlags.Button1Released | MouseFlags.ButtonAlt),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.LeftAltPressed, MouseFlags.None | MouseFlags.ButtonAlt)
            }
        };

        yield return new object []
        {
            new []
            {
                Tuple.Create (ButtonState.Button1Pressed, EventFlags.NoEvent, ControlKeyState.RightAltPressed, MouseFlags.Button1Pressed | MouseFlags.ButtonAlt),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.RightAltPressed, MouseFlags.Button1Released | MouseFlags.ButtonAlt),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.RightAltPressed, MouseFlags.None | MouseFlags.ButtonAlt)
            }
        };

        yield return new object []
        {
            new []
            {
                Tuple.Create (ButtonState.Button1Pressed, EventFlags.NoEvent, ControlKeyState.LeftControlPressed, MouseFlags.Button1Pressed | MouseFlags.ButtonCtrl),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.LeftControlPressed, MouseFlags.Button1Released | MouseFlags.ButtonCtrl),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.LeftControlPressed, MouseFlags.None | MouseFlags.ButtonCtrl)
            }
        };

        yield return new object []
        {
            new []
            {
                Tuple.Create (ButtonState.Button1Pressed, EventFlags.NoEvent, ControlKeyState.RightControlPressed, MouseFlags.Button1Pressed | MouseFlags.ButtonCtrl),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.RightControlPressed, MouseFlags.Button1Released | MouseFlags.ButtonCtrl),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.RightControlPressed, MouseFlags.None | MouseFlags.ButtonCtrl)
            }
        };

        yield return new object []
        {
            new []
            {
                Tuple.Create (ButtonState.Button1Pressed, EventFlags.NoEvent, ControlKeyState.ShiftPressed, MouseFlags.Button1Pressed | MouseFlags.ButtonShift),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.ShiftPressed, MouseFlags.Button1Released | MouseFlags.ButtonShift),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.ShiftPressed, MouseFlags.None | MouseFlags.ButtonShift)
            }
        };

        // Test for ControlKeyState buttons pressed and not handled
        yield return new object []
        {
            new []
            {
                Tuple.Create (ButtonState.Button1Pressed, EventFlags.NoEvent, ControlKeyState.CapslockOn, MouseFlags.Button1Pressed),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.CapslockOn, MouseFlags.Button1Released),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.CapslockOn, MouseFlags.None)
            }
        };

        yield return new object []
        {
            new []
            {
                Tuple.Create (ButtonState.Button1Pressed, EventFlags.NoEvent, ControlKeyState.EnhancedKey, MouseFlags.Button1Pressed),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.EnhancedKey, MouseFlags.Button1Released),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.EnhancedKey, MouseFlags.None)
            }
        };

        yield return new object []
        {
            new []
            {
                Tuple.Create (ButtonState.Button1Pressed, EventFlags.NoEvent, ControlKeyState.NumlockOn, MouseFlags.Button1Pressed),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NumlockOn, MouseFlags.Button1Released),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NumlockOn, MouseFlags.None)
            }
        };

        yield return new object []
        {
            new []
            {
                Tuple.Create (ButtonState.Button1Pressed, EventFlags.NoEvent, ControlKeyState.ScrolllockOn, MouseFlags.Button1Pressed),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.ScrolllockOn, MouseFlags.Button1Released),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.ScrolllockOn, MouseFlags.None)
            }
        };
    }

    [Theory]
    [MemberData (nameof (MouseFlagTestData))]
    internal void MouseFlags_Should_Map_Correctly (Tuple<ButtonState, EventFlags, ControlKeyState, MouseFlags> [] inputOutputPairs)
    {
        var processor = new WindowsInputProcessor (new ());

        foreach (Tuple<ButtonState, EventFlags, ControlKeyState, MouseFlags> pair in inputOutputPairs)
        {
            var mockEvent = new MouseEventRecord { ButtonState = pair.Item1, EventFlags = pair.Item2, ControlKeyState = pair.Item3};
            MouseEventArgs result = processor.ToDriverMouse (mockEvent);

            Assert.Equal (pair.Item4, result.Flags);
        }
    }
}
