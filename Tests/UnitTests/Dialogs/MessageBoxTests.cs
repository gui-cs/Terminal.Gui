using System.Text;
using UICatalog;
using UnitTests;
using Xunit.Abstractions;

namespace UnitTests.DialogTests;

public class MessageBoxTests (ITestOutputHelper output)
{
    [Fact]
    [AutoInitShutdown]
    public void KeyBindings_Enter_Causes_Focused_Button_Click_No_Accept ()
    {
        int result = -1;

        var iteration = 0;

        var btnAcceptCount = 0;

        Application.Iteration += OnApplicationOnIteration;
        Application.Run ().Dispose ();
        Application.Iteration -= OnApplicationOnIteration;

        Assert.Equal (1, result);
        Assert.Equal (1, btnAcceptCount);

        return;

        void OnApplicationOnIteration (object s, IterationEventArgs a)
        {
            iteration++;

            switch (iteration)
            {
                case 1:
                    result = MessageBox.Query (string.Empty, string.Empty, 0, false, "btn0", "btn1");
                    Application.RequestStop ();

                    break;

                case 2:
                    // Tab to btn2
                    Application.RaiseKeyDownEvent (Key.Tab);

                    var btn = Application.Navigation!.GetFocused () as Button;

                    btn.Accepting += (sender, e) => { btnAcceptCount++; };

                    // Click
                    Application.RaiseKeyDownEvent (Key.Enter);

                    break;

                default:
                    Assert.Fail ();

                    break;
            }
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void KeyBindings_Esc_Closes ()
    {
        var result = 999;

        var iteration = 0;

        Application.Iteration += OnApplicationOnIteration;
        Application.Run ().Dispose ();
        Application.Iteration -= OnApplicationOnIteration;

        Assert.Equal (-1, result);

        return;

        void OnApplicationOnIteration (object s, IterationEventArgs a)
        {
            iteration++;

            switch (iteration)
            {
                case 1:
                    result = MessageBox.Query (string.Empty, string.Empty, 0, false, "btn0", "btn1");
                    Application.RequestStop ();

                    break;

                case 2:
                    Application.RaiseKeyDownEvent (Key.Esc);

                    break;

                default:
                    Assert.Fail ();

                    break;
            }
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void KeyBindings_Space_Causes_Focused_Button_Click_No_Accept ()
    {
        int result = -1;

        var iteration = 0;

        var btnAcceptCount = 0;

        Application.Iteration += OnApplicationOnIteration;
        Application.Run ().Dispose ();
        Application.Iteration -= OnApplicationOnIteration;

        Assert.Equal (1, result);
        Assert.Equal (1, btnAcceptCount);

        return;

        void OnApplicationOnIteration (object s, IterationEventArgs a)
        {
            iteration++;

            switch (iteration)
            {
                case 1:
                    result = MessageBox.Query (string.Empty, string.Empty, 0, false, "btn0", "btn1");
                    Application.RequestStop ();

                    break;

                case 2:
                    // Tab to btn2
                    Application.RaiseKeyDownEvent (Key.Tab);

                    var btn = Application.Navigation!.GetFocused () as Button;

                    btn.Accepting += (sender, e) => { btnAcceptCount++; };

                    Application.RaiseKeyDownEvent (Key.Space);

                    break;

                default:
                    Assert.Fail ();

                    break;
            }
        }
    }

    [Theory]
    [InlineData (@"", false, false, 6, 6, 2, 2)]
    [InlineData (@"", false, true, 3, 6, 9, 3)]
    [InlineData (@"01234\n-----\n01234", false, false, 1, 6, 13, 3)]
    [InlineData (@"01234\n-----\n01234", true, false, 1, 5, 13, 4)]
    [InlineData (@"0123456789", false, false, 1, 6, 12, 3)]
    [InlineData (@"0123456789", false, true, 1, 5, 12, 4)]
    [InlineData (@"01234567890123456789", false, true, 1, 5, 13, 4)]
    [InlineData (@"01234567890123456789", true, true, 1, 5, 13, 5)]
    [InlineData (@"01234567890123456789\n01234567890123456789", false, true, 1, 5, 13, 4)]
    [InlineData (@"01234567890123456789\n01234567890123456789", true, true, 1, 4, 13, 7)]
    [AutoInitShutdown]
    public void Location_And_Size_Correct (string message, bool wrapMessage, bool hasButton, int expectedX, int expectedY, int expectedW, int expectedH)
    {
        int iterations = -1;

        Application.Driver!.SetScreenSize(15, 15); // 15 x 15 gives us enough room for a button with one char (9x1)
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        var mbFrame = Rectangle.Empty;

        Application.Iteration += OnApplicationOnIteration;

        Application.Run ().Dispose ();
        Application.Iteration -= OnApplicationOnIteration;

        Assert.Equal (new (expectedX, expectedY, expectedW, expectedH), mbFrame);

        return;

        void OnApplicationOnIteration (object s, IterationEventArgs a)
        {
            iterations++;

            if (iterations == 0)
            {
                MessageBox.Query (string.Empty, message, 0, wrapMessage, hasButton ? ["0"] : []);
                Application.RequestStop ();
            }
            else if (iterations == 1)
            {
                mbFrame = Application.Current!.Frame;
                Application.RequestStop ();
            }
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void Message_With_Spaces_WrapMessage_False ()
    {
        int iterations = -1;
        var top = new Toplevel ();
        top.BorderStyle = LineStyle.None;
        Application.Driver!.SetScreenSize(20, 10);

        var btn =
            $"{Glyphs.LeftBracket}{Glyphs.LeftDefaultIndicator} btn {Glyphs.RightDefaultIndicator}{Glyphs.RightBracket}";

        // Override CM
        MessageBox.DefaultButtonAlignment = Alignment.End;
        MessageBox.DefaultBorderStyle = LineStyle.Double;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        Application.Iteration += OnApplicationOnIteration;

        Application.Run (top);
        Application.Iteration -= OnApplicationOnIteration;
        top.Dispose ();

        return;

        void OnApplicationOnIteration (object s, IterationEventArgs a)
        {
            iterations++;

            if (iterations == 0)
            {
                var sb = new StringBuilder ();

                for (var i = 0; i < 17; i++)
                {
                    sb.Append ("ff ");
                }

                MessageBox.Query (string.Empty, sb.ToString (), 0, false, "btn");

                Application.RequestStop ();
            }
            else if (iterations == 2)
            {
                DriverAssert.AssertDriverContentsWithFrameAre (
                                                               @"
 ╔════════════════╗
 ║ ff ff ff ff ff ║
 ║       ⟦► btn ◄⟧║
 ╚════════════════╝",
                                                               output);
                Application.RequestStop ();

                // Really long text
                MessageBox.Query (string.Empty, new ('f', 500), 0, false, "btn");
            }
            else if (iterations == 4)
            {
                DriverAssert.AssertDriverContentsWithFrameAre (
                                                               @"
 ╔════════════════╗
 ║ffffffffffffffff║
 ║       ⟦► btn ◄⟧║
 ╚════════════════╝",
                                                               output);
                Application.RequestStop ();
            }
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void Message_With_Spaces_WrapMessage_True ()
    {
        int iterations = -1;
        var top = new Toplevel ();
        top.BorderStyle = LineStyle.None;
        Application.Driver!.SetScreenSize (20, 10);

        var btn =
            $"{Glyphs.LeftBracket}{Glyphs.LeftDefaultIndicator} btn {Glyphs.RightDefaultIndicator}{Glyphs.RightBracket}";

        // Override CM
        MessageBox.DefaultButtonAlignment = Alignment.End;
        MessageBox.DefaultBorderStyle = LineStyle.Double;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        Application.Iteration += OnApplicationOnIteration;

        Application.Run (top);
        Application.Iteration -= OnApplicationOnIteration;
        top.Dispose ();

        return;

        void OnApplicationOnIteration (object s, IterationEventArgs a)
        {
            iterations++;

            if (iterations == 0)
            {
                var sb = new StringBuilder ();

                for (var i = 0; i < 17; i++)
                {
                    sb.Append ("ff ");
                }

                MessageBox.Query (string.Empty, sb.ToString (), 0, true, "btn");

                Application.RequestStop ();
            }
            else if (iterations == 2)
            {
                DriverAssert.AssertDriverContentsWithFrameAre (
                                                               @"
  ╔══════════════╗
  ║ff ff ff ff ff║
  ║ff ff ff ff ff║
  ║ff ff ff ff ff║
  ║    ff ff     ║
  ║     ⟦► btn ◄⟧║
  ╚══════════════╝",
                                                               output);
                Application.RequestStop ();

                // Really long text
                MessageBox.Query (string.Empty, new ('f', 500), 0, true, "btn");
            }
            else if (iterations == 4)
            {
                DriverAssert.AssertDriverContentsWithFrameAre (
                                                               @"
 ╔════════════════╗
 ║ffffffffffffffff║
 ║ffffffffffffffff║
 ║ffffffffffffffff║
 ║ffffffffffffffff║
 ║ffffffffffffffff║
 ║ffffffffffffffff║
 ║fffffff⟦► btn ◄⟧║
 ╚════════════════╝",
                                                               output);
                Application.RequestStop ();
            }
        }
    }

    [Theory (Skip = "Bogus test: Never does anything")]
    [InlineData (0, 0, "1")]
    [InlineData (1, 1, "1")]
    [InlineData (7, 5, "1")]
    [InlineData (50, 50, "1")]
    [InlineData (0, 0, "message")]
    [InlineData (1, 1, "message")]
    [InlineData (7, 5, "message")]
    [InlineData (50, 50, "message")]
    [AutoInitShutdown]
    public void Size_Not_Default_Message (int height, int width, string message)
    {
        int iterations = -1;
        Application.Driver!.SetScreenSize(100, 100);

        Application.Iteration += (s, a) =>
                                 {
                                     iterations++;

                                     if (iterations == 0)
                                     {
                                         MessageBox.Query (height, width, string.Empty, message, null);

                                         Application.RequestStop ();
                                     }
                                     else if (iterations == 1)
                                     {
                                         AutoInitShutdownAttribute.RunIteration ();

                                         Assert.IsType<Dialog> (Application.Current);
                                         Assert.Equal (new (height, width), Application.Current.Frame.Size);

                                         Application.RequestStop ();
                                     }
                                 };
    }

    [Theory (Skip = "Bogus test: Never does anything")]
    [InlineData (0, 0, "1")]
    [InlineData (1, 1, "1")]
    [InlineData (7, 5, "1")]
    [InlineData (50, 50, "1")]
    [InlineData (0, 0, "message")]
    [InlineData (1, 1, "message")]
    [InlineData (7, 5, "message")]
    [InlineData (50, 50, "message")]
    [AutoInitShutdown]
    public void Size_Not_Default_Message_Button (int height, int width, string message)
    {
        int iterations = -1;
        Application.Driver?.SetScreenSize(100, 100);

        Application.Iteration += (s, a) =>
                                 {
                                     iterations++;

                                     if (iterations == 0)
                                     {
                                         MessageBox.Query (height, width, string.Empty, message, "_Ok");

                                         Application.RequestStop ();
                                     }
                                     else if (iterations == 1)
                                     {
                                         AutoInitShutdownAttribute.RunIteration ();

                                         Assert.IsType<Dialog> (Application.Current);
                                         Assert.Equal (new (height, width), Application.Current.Frame.Size);

                                         Application.RequestStop ();
                                     }
                                 };
    }

    [Theory (Skip = "Bogus test: Never does anything")]
    [InlineData (0, 0)]
    [InlineData (1, 1)]
    [InlineData (7, 5)]
    [InlineData (50, 50)]
    [AutoInitShutdown]
    public void Size_Not_Default_No_Message (int height, int width)
    {
        int iterations = -1;
        Application.Driver?.SetScreenSize(100, 100);

        Application.Iteration += (s, a) =>
                                 {
                                     iterations++;

                                     if (iterations == 0)
                                     {
                                         MessageBox.Query (height, width, string.Empty, string.Empty, null);

                                         Application.RequestStop ();
                                     }
                                     else if (iterations == 1)
                                     {
                                         AutoInitShutdownAttribute.RunIteration ();

                                         Assert.IsType<Dialog> (Application.Current);
                                         Assert.Equal (new (height, width), Application.Current.Frame.Size);

                                         Application.RequestStop ();
                                     }
                                 };
    }

    [Fact]
    [AutoInitShutdown]
    public void UICatalog_AboutBox ()
    {
        int iterations = -1;
        Application.Driver!.SetScreenSize (70, 15);

        // Override CM
        MessageBox.DefaultButtonAlignment = Alignment.End;
        MessageBox.DefaultBorderStyle = LineStyle.Double;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        Application.Iteration += OnApplicationOnIteration;

        var top = new Toplevel ();
        top.BorderStyle = LineStyle.Single;
        Application.Run (top);
        Application.Iteration -= OnApplicationOnIteration;
        top.Dispose ();

        return;

        void OnApplicationOnIteration (object s, IterationEventArgs a)
        {
            iterations++;

            if (iterations == 0)
            {
                MessageBox.Query (
                                  "",
                                  UICatalog.UICatalogTop.GetAboutBoxMessage (),
                                  wrapMessage: false,
                                  buttons: "_Ok");

                Application.RequestStop ();
            }
            else if (iterations == 2)
            {
                var expectedText = """
                                   ┌────────────────────────────────────────────────────────────────────┐
                                   │   ╔═══════════════════════════════════════════════════════════╗    │
                                   │   ║UI Catalog: A comprehensive sample library and test app for║    │
                                   │   ║                                                           ║    │
                                   │   ║ _______                  _             _   _____       _  ║    │
                                   │   ║|__   __|                (_)           | | / ____|     (_) ║    │
                                   │   ║   | | ___ _ __ _ __ ___  _ _ __   __ _| || |  __ _   _ _  ║    │
                                   │   ║   | |/ _ \ '__| '_ ` _ \| | '_ \ / _` | || | |_ | | | | | ║    │
                                   │   ║   | |  __/ |  | | | | | | | | | | (_| | || |__| | |_| | | ║    │
                                   │   ║   |_|\___|_|  |_| |_| |_|_|_| |_|\__,_|_(_)_____|\__,_|_| ║    │
                                   │   ║                                                           ║    │
                                   │   ║                      v2 - Pre-Alpha                       ║    │
                                   │   ║                                                   ⟦► Ok ◄⟧║    │
                                   │   ╚═══════════════════════════════════════════════════════════╝    │
                                   └────────────────────────────────────────────────────────────────────┘
                                   """;

                DriverAssert.AssertDriverContentsAre (expectedText, output);

                Application.RequestStop ();
            }
        }
    }

    [Theory]
    [MemberData (nameof (AcceptingKeys))]
    [AutoInitShutdown]
    public void Button_IsDefault_True_Return_His_Index_On_Accepting (Key key)
    {
        Application.Iteration += OnApplicationOnIteration;
        int res = MessageBox.Query ("hey", "IsDefault", "Yes", "No");
        Application.Iteration -= OnApplicationOnIteration;

        Assert.Equal (0, res);

        return;

        void OnApplicationOnIteration (object o, IterationEventArgs iterationEventArgs) => Assert.True (Application.RaiseKeyDownEvent (key));
    }

    public static IEnumerable<object []> AcceptingKeys ()
    {
        yield return [Key.Enter];
        yield return [Key.Space];
    }
}
