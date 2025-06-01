using System.Text;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.Drivers.FakeConsole;

namespace Terminal.Gui.DriverTests;

public class ConsoleDriverTests
{
    private readonly ITestOutputHelper _output;

    public ConsoleDriverTests (ITestOutputHelper output)
    {
        ConsoleDriver.RunningUnitTests = true;
        _output = output;
    }

    [Theory]
    [InlineData (typeof (FakeDriver))]
    [InlineData (typeof (NetDriver))]

    //[InlineData (typeof (ANSIDriver))]
    [InlineData (typeof (WindowsDriver))]
    [InlineData (typeof (CursesDriver))]
    public void End_Cleans_Up (Type driverType)
    {
        var driver = (IConsoleDriver)Activator.CreateInstance (driverType);
        driver.Init ();
        driver.End ();
    }

    [Theory]
    [InlineData (typeof (FakeDriver))]
    public void FakeDriver_MockKeyPresses (Type driverType)
    {
        var driver = (IConsoleDriver)Activator.CreateInstance (driverType);
        Application.Init (driver);

        var text = "MockKeyPresses";
        Stack<ConsoleKeyInfo> mKeys = new ();

        foreach (char r in text.Reverse ())
        {
            ConsoleKey ck = char.IsLetter (r) ? (ConsoleKey)char.ToUpper (r) : (ConsoleKey)r;
            var cki = new ConsoleKeyInfo (r, ck, char.IsUpper(r), false, false);
            mKeys.Push (cki);
        }

        Console.MockKeyPresses = mKeys;

        Toplevel top = new ();
        var view = new View { CanFocus = true };
        var rText = "";
        var idx = 0;

        view.KeyDown += (s, e) =>
                        {
                            Assert.Equal (new Rune(text [idx]), e.AsRune);
                            rText += e.AsRune;
                            Assert.Equal (rText, text.Substring (0, idx + 1));
                            e.Handled = true;
                            idx++;
                        };
        top.Add (view);

        Application.Iteration += (s, a) =>
                                 {
                                     if (mKeys.Count == 0)
                                     {
                                         Application.RequestStop ();
                                     }
                                 };

        Application.Run (top);

        Assert.Equal ("MockKeyPresses", rText);

        top.Dispose ();
        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();
    }

    [Theory]
    [InlineData (typeof (FakeDriver))]
    public void FakeDriver_Only_Sends_Keystrokes_Through_MockKeyPresses (Type driverType)
    {
        var driver = (IConsoleDriver)Activator.CreateInstance (driverType);
        Application.Init (driver);

        Toplevel top = new ();
        var view = new View { CanFocus = true };
        var count = 0;
        var wasKeyPressed = false;

        view.KeyDown += (s, e) => { wasKeyPressed = true; };
        top.Add (view);

        Application.Iteration += (s, a) =>
                                 {
                                     count++;

                                     if (count == 10)
                                     {
                                         Application.RequestStop ();
                                     }
                                 };

        Application.Run (top);

        Assert.False (wasKeyPressed);

        top.Dispose ();
        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();
    }

    [Theory]
    [InlineData (typeof (FakeDriver))]
    [InlineData (typeof (NetDriver))]

    //[InlineData (typeof (ANSIDriver))]
    [InlineData (typeof (WindowsDriver))]
    [InlineData (typeof (CursesDriver))]
    public void Init_Inits (Type driverType)
    {
        var driver = (IConsoleDriver)Activator.CreateInstance (driverType);
        MainLoop ml = driver.Init ();
        Assert.NotNull (ml);
        Assert.NotNull (driver.Clipboard);
        Console.ForegroundColor = ConsoleColor.Red;
        Assert.Equal (ConsoleColor.Red, Console.ForegroundColor);
        Console.BackgroundColor = ConsoleColor.Green;
        Assert.Equal (ConsoleColor.Green, Console.BackgroundColor);

        driver.End ();
    }

    //[Theory]
    //[InlineData (typeof (FakeDriver))]
    //public void FakeDriver_MockKeyPresses_Press_AfterTimeOut (Type driverType)
    //{
    //	var driver = (IConsoleDriver)Activator.CreateInstance (driverType);
    //	Application.Init (driver);

    //	// Simulating pressing of QuitKey after a short period of time
    //	uint quitTime = 100;
    //	Func<MainLoop, bool> closeCallback = (MainLoop loop) => {
    //		// Prove the scenario is using Application.QuitKey correctly
    //		output.WriteLine ($"  {quitTime}ms elapsed; Simulating keypresses...");
    //		FakeConsole.PushMockKeyPress (Key.F);
    //		FakeConsole.PushMockKeyPress (Key.U);
    //		FakeConsole.PushMockKeyPress (Key.C);
    //		FakeConsole.PushMockKeyPress (Key.K);
    //		return false;
    //	};
    //	output.WriteLine ($"Add timeout to simulate key presses after {quitTime}ms");
    //	_ = Application.AddTimeout (TimeSpan.FromMilliseconds (quitTime), closeCallback);

    //	// If Top doesn't quit within abortTime * 5 (500ms), this will force it
    //	uint abortTime = quitTime * 5;
    //	Func<MainLoop, bool> forceCloseCallback = (MainLoop loop) => {
    //		Application.RequestStop ();
    //		Assert.Fail ($"  failed to Quit after {abortTime}ms. Force quit.");
    //		return false;
    //	};
    //	output.WriteLine ($"Add timeout to force quit after {abortTime}ms");
    //	_ = Application.AddTimeout (TimeSpan.FromMilliseconds (abortTime), forceCloseCallback);

    //	Key key = Key.Unknown;

    //	Application.Top.KeyPress += (e) => {
    //		key = e.Key;
    //		output.WriteLine ($"  Application.Top.KeyPress: {key}");
    //		e.Handled = true;

    //	};

    //	int iterations = 0;
    //	Application.Iteration += (s, a) => {
    //		output.WriteLine ($"  iteration {++iterations}");

    //		if (Console.MockKeyPresses.Count == 0) {
    //			output.WriteLine ($"    No more MockKeyPresses; RequestStop");
    //			Application.RequestStop ();
    //		}
    //	};

    //	Application.Run ();

    //	// Shutdown must be called to safely clean up Application if Init has been called
    //	Application.Shutdown ();
    //}

    [Theory]
    [InlineData (typeof (FakeDriver))]
    [InlineData (typeof (NetDriver))]

    //[InlineData (typeof (ANSIDriver))]
    [InlineData (typeof (WindowsDriver))]
    [InlineData (typeof (CursesDriver))]
    public void TerminalResized_Simulation (Type driverType)
    {
        var driver = (IConsoleDriver)Activator.CreateInstance (driverType);
        driver?.Init ();
        driver.Cols = 80;
        driver.Rows = 25;

        var wasTerminalResized = false;

        driver.SizeChanged += (s, e) =>
                              {
                                  wasTerminalResized = true;
                                  Assert.Equal (120, e.Size.GetValueOrDefault ().Width);
                                  Assert.Equal (40, e.Size.GetValueOrDefault ().Height);
                              };

        Assert.Equal (80, driver.Cols);
        Assert.Equal (25, driver.Rows);
        Assert.False (wasTerminalResized);

        driver.Cols = 120;
        driver.Rows = 40;

        ((ConsoleDriver)driver).OnSizeChanged (new SizeChangedEventArgs (new (driver.Cols, driver.Rows)));
        Assert.Equal (120, driver.Cols);
        Assert.Equal (40, driver.Rows);
        Assert.True (wasTerminalResized);
        driver.End ();
    }

    // Disabled due to test error - Change Task.Delay to an await
    //		[Fact, AutoInitShutdown]
    //		public void Write_Do_Not_Change_On_ProcessKey ()
    //		{
    //			var win = new Window ();
    //			Application.Begin (win);
    //			((FakeDriver)Application.Driver!).SetBufferSize (20, 8);

    //			System.Threading.Tasks.Task.Run (() => {
    //				System.Threading.Tasks.Task.Delay (500).Wait ();
    //				Application.Invoke (() => {
    //					var lbl = new Label ("Hello World") { X = Pos.Center () };
    //					var dlg = new Dialog ();
    //					dlg.Add (lbl);
    //					Application.Begin (dlg);

    //					var expected = @"
    //┌──────────────────┐
    //│┌───────────────┐ │
    //││  Hello World  │ │
    //││               │ │
    //││               │ │
    //││               │ │
    //│└───────────────┘ │
    //└──────────────────┘
    //";

    //					var pos = DriverAsserts.AssertDriverContentsWithFrameAre (expected, output);
    //					Assert.Equal (new (0, 0, 20, 8), pos);

    //					Assert.True (dlg.ProcessKey (new (Key.Tab)));
    //					dlg.Draw ();

    //					expected = @"
    //┌──────────────────┐
    //│┌───────────────┐ │
    //││  Hello World  │ │
    //││               │ │
    //││               │ │
    //││               │ │
    //│└───────────────┘ │
    //└──────────────────┘
    //";

    //					pos = DriverAsserts.AssertDriverContentsWithFrameAre (expected, output);
    //					Assert.Equal (new (0, 0, 20, 8), pos);

    //					win.RequestStop ();
    //				});
    //			});

    //			Application.Run (win);
    //			Application.Shutdown ();
    //		}

    [Theory]
    [InlineData ('\ud83d', '\udcc4')] // This seems right sequence but Stack is LIFO
    [InlineData ('\ud83d', '\ud83d')]
    [InlineData ('\udcc4', '\udcc4')]
    public void FakeDriver_IsValidInput_Wrong_Surrogate_Sequence (char c1, char c2)
    {
        var driver = (IConsoleDriver)Activator.CreateInstance (typeof (FakeDriver));
        Application.Init (driver);

        Stack<ConsoleKeyInfo> mKeys = new (
                                           [
                                               new ('a', ConsoleKey.A, false, false, false),
                                               new (c1, ConsoleKey.None, false, false, false),
                                               new (c2, ConsoleKey.None, false, false, false)
                                           ]);

        Console.MockKeyPresses = mKeys;

        Toplevel top = new ();
        var view = new View { CanFocus = true };
        var rText = "";
        var idx = 0;

        view.KeyDown += (s, e) =>
                        {
                            Assert.Equal (new ('a'), e.AsRune);
                            Assert.Equal ("a", e.AsRune.ToString ());
                            rText += e.AsRune;
                            e.Handled = true;
                            idx++;
                        };
        top.Add (view);

        Application.Iteration += (s, a) =>
                                 {
                                     if (mKeys.Count == 0)
                                     {
                                         Application.RequestStop ();
                                     }
                                 };

        Application.Run (top);

        Assert.Equal ("a", rText);
        Assert.Equal (1, idx);
        Assert.Equal (0, ((FakeDriver)driver)._highSurrogate);

        top.Dispose ();

        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();
    }

    [Fact]
    public void FakeDriver_IsValidInput_Correct_Surrogate_Sequence ()
    {
        var driver = (IConsoleDriver)Activator.CreateInstance (typeof (FakeDriver));
        Application.Init (driver);

        Stack<ConsoleKeyInfo> mKeys = new (
                                           [
                                               new ('a', ConsoleKey.A, false, false, false),
                                               new ('\udcc4', ConsoleKey.None, false, false, false),
                                               new ('\ud83d', ConsoleKey.None, false, false, false)
                                           ]);

        Console.MockKeyPresses = mKeys;

        Toplevel top = new ();
        var view = new View { CanFocus = true };
        var rText = "";
        var idx = 0;

        view.KeyDown += (s, e) =>
                        {
                            if (idx == 0)
                            {
                                Assert.Equal (new (0x1F4C4), e.AsRune);
                                Assert.Equal ("📄", e.AsRune.ToString ());
                            }
                            else
                            {
                                Assert.Equal (new ('a'), e.AsRune);
                                Assert.Equal ("a", e.AsRune.ToString ());
                            }

                            rText += e.AsRune;
                            e.Handled = true;
                            idx++;
                        };
        top.Add (view);

        Application.Iteration += (s, a) =>
                                 {
                                     if (mKeys.Count == 0)
                                     {
                                         Application.RequestStop ();
                                     }
                                 };

        Application.Run (top);

        Assert.Equal ("📄a", rText);
        Assert.Equal (2, idx);

        top.Dispose ();

        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();
    }
}
