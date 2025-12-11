#nullable disable
namespace DriverTests.Mouse;

public class MouseInterpreterTests
{
    [Theory]
    [MemberData (nameof (SequenceTests))]
    public void TestMouseEventSequences_InterpretedOnlyAsFlag (List<Terminal.Gui.Input.Mouse> events, params MouseFlags? [] expected)
    {
        // Arrange: Mock dependencies and set up the interpreter
        MouseInterpreter interpreter = new  ();

        // Collect all results from processing the event sequence
        List<Terminal.Gui.Input.Mouse> allResults = [];

        // Act
        foreach (Terminal.Gui.Input.Mouse mouse in events)
        {
            allResults.AddRange (interpreter.Process (mouse));
        }

        // Assert - verify all expected click events were generated
        foreach (MouseFlags? expectedClick in expected.Where (e => e != null))
        {
            Assert.Contains (allResults, e => e.Flags == expectedClick);
        }

        // Also verify all original input events were passed through
        foreach (Terminal.Gui.Input.Mouse inputEvent in events)
        {
            Assert.Contains (allResults, e => e.Flags == inputEvent.Flags);
        }
    }

    public static IEnumerable<object []> SequenceTests ()
    {
        yield return
        [
            new List<Terminal.Gui.Input.Mouse>
            {
                new () { Flags = MouseFlags.Button1Pressed },
                new ()
            },
            new MouseFlags?[] { null, MouseFlags.Button1Clicked }
        ];

        yield return
        [
            new List<Terminal.Gui.Input.Mouse>
            {
                new () { Flags = MouseFlags.Button1Pressed },
                new (),
                new () { Flags = MouseFlags.Button1Pressed },
                new ()
            },
            new MouseFlags?[] { null, MouseFlags.Button1Clicked, null, MouseFlags.Button1DoubleClicked }
        ];

        yield return
        [
            new List<Terminal.Gui.Input.Mouse>
            {
                new () { Flags = MouseFlags.Button1Pressed },
                new (),
                new () { Flags = MouseFlags.Button1Pressed },
                new (),
                new () { Flags = MouseFlags.Button1Pressed },
                new ()
            },
            new MouseFlags?[] { null, MouseFlags.Button1Clicked, null, MouseFlags.Button1DoubleClicked, null, MouseFlags.Button1TripleClicked }
        ];

        yield return
        [
            new List<Terminal.Gui.Input.Mouse>
            {
                new () { Flags = MouseFlags.Button2Pressed },
                new (),
                new () { Flags = MouseFlags.Button2Pressed },
                new (),
                new () { Flags = MouseFlags.Button2Pressed },
                new ()
            },
            new MouseFlags?[] { null, MouseFlags.Button2Clicked, null, MouseFlags.Button2DoubleClicked, null, MouseFlags.Button2TripleClicked }
        ];

        yield return
        [
            new List<Terminal.Gui.Input.Mouse>
            {
                new () { Flags = MouseFlags.Button3Pressed },
                new (),
                new () { Flags = MouseFlags.Button3Pressed },
                new (),
                new () { Flags = MouseFlags.Button3Pressed },
                new ()
            },
            new MouseFlags?[] { null, MouseFlags.Button3Clicked, null, MouseFlags.Button3DoubleClicked, null, MouseFlags.Button3TripleClicked }
        ];

        yield return
        [
            new List<Terminal.Gui.Input.Mouse>
            {
                new () { Flags = MouseFlags.Button4Pressed },
                new (),
                new () { Flags = MouseFlags.Button4Pressed },
                new (),
                new () { Flags = MouseFlags.Button4Pressed },
                new ()
            },
            new MouseFlags?[] { null, MouseFlags.Button4Clicked, null, MouseFlags.Button4DoubleClicked, null, MouseFlags.Button4TripleClicked }
        ];

        yield return
        [
            new List<Terminal.Gui.Input.Mouse>
            {
                new () { Flags = MouseFlags.Button1Pressed, Position = new (10, 11) },
                new () { Position = new (10, 11) },

                // Clicking the line below means no double click because it's a different location
                new () { Flags = MouseFlags.Button1Pressed, Position = new (10, 12) },
                new () { Position = new (10, 12) }
            },
            new MouseFlags?[] { null, MouseFlags.Button1Clicked, null, MouseFlags.Button1Clicked } //release is click because new position
        ];
    }
}
