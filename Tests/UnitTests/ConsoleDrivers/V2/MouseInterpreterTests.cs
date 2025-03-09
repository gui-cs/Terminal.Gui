using Moq;

namespace UnitTests.ConsoleDrivers.V2;
public class MouseInterpreterTests
{
    [Theory]
    [MemberData (nameof (SequenceTests))]
    public void TestMouseEventSequences_InterpretedOnlyAsFlag (List<MouseEventArgs> events, params MouseFlags?[] expected)
    {
        // Arrange: Mock dependencies and set up the interpreter
        var interpreter = new MouseInterpreter (null);

        // Act and Assert
        for (int i = 0; i < events.Count; i++)
        {
            var results = interpreter.Process (events [i]).ToArray();

            // Raw input event should be there
            Assert.Equal (events [i].Flags, results [0].Flags);

            // also any expected should be there
            if (expected [i] != null)
            {
                Assert.Equal (expected [i], results [1].Flags);
            }
            else
            {
                Assert.Single (results);
            }
        }
    }

    public static IEnumerable<object []> SequenceTests ()
    {
        yield return new object []
        {
            new List<MouseEventArgs>
            {
                new() { Flags = MouseFlags.Button1Pressed },
                new()
            },
            null,
            MouseFlags.Button1Clicked
        };

        yield return new object []
        {
            new List<MouseEventArgs>
            {
                new() { Flags = MouseFlags.Button1Pressed },
                new(),
                new() { Flags = MouseFlags.Button1Pressed },
                new()
            },
            null,
            MouseFlags.Button1Clicked,
            null,
            MouseFlags.Button1DoubleClicked
        };

        yield return new object []
        {
            new List<MouseEventArgs>
            {
                new() { Flags = MouseFlags.Button1Pressed },
                new(),
                new() { Flags = MouseFlags.Button1Pressed },
                new(),
                new() { Flags = MouseFlags.Button1Pressed },
                new()
            },
            null,
            MouseFlags.Button1Clicked,
            null,
            MouseFlags.Button1DoubleClicked,
            null,
            MouseFlags.Button1TripleClicked
        };

        yield return new object []
        {
            new List<MouseEventArgs>
            {
                new() { Flags = MouseFlags.Button2Pressed },
                new(),
                new() { Flags = MouseFlags.Button2Pressed },
                new(),
                new() { Flags = MouseFlags.Button2Pressed },
                new()
            },
            null,
            MouseFlags.Button2Clicked,
            null,
            MouseFlags.Button2DoubleClicked,
            null,
            MouseFlags.Button2TripleClicked
        };

        yield return new object []
        {
            new List<MouseEventArgs>
            {
                new() { Flags = MouseFlags.Button3Pressed },
                new(),
                new() { Flags = MouseFlags.Button3Pressed },
                new(),
                new() { Flags = MouseFlags.Button3Pressed },
                new()
            },
            null,
            MouseFlags.Button3Clicked,
            null,
            MouseFlags.Button3DoubleClicked,
            null,
            MouseFlags.Button3TripleClicked
        };

        yield return new object []
        {
            new List<MouseEventArgs>
            {
                new() { Flags = MouseFlags.Button4Pressed },
                new(),
                new() { Flags = MouseFlags.Button4Pressed },
                new(),
                new() { Flags = MouseFlags.Button4Pressed },
                new()
            },
            null,
            MouseFlags.Button4Clicked,
            null,
            MouseFlags.Button4DoubleClicked,
            null,
            MouseFlags.Button4TripleClicked
        };

        yield return new object []
        {
            new List<MouseEventArgs>
            {
                new() { Flags = MouseFlags.Button1Pressed ,Position = new Point (10,11)},
                new(){Position = new Point (10,11)},

                // Clicking the line below means no double click because it's a different location
                new() { Flags = MouseFlags.Button1Pressed,Position = new Point (10,12) },
                new(){Position = new Point (10,12)}
            },
            null,
            MouseFlags.Button1Clicked,
            null,
            MouseFlags.Button1Clicked //release is click because new position
        };
    }

}
