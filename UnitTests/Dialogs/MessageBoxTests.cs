using System.Text;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Xunit.Abstractions;

namespace Terminal.Gui.DialogTests;

public class MessageBoxTests
{
    private readonly ITestOutputHelper _output;
    public MessageBoxTests (ITestOutputHelper output) { _output = output; }

    [Fact]
    [AutoInitShutdown]
    public void KeyBindings_Enter_Causes_Focused_Button_Click_No_Accept ()
    {
        int result = -1;

        var iteration = 0;

        int btnAcceptCount = 0;

        Application.Iteration += (s, a) =>
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

                                             Button btn = Application.Navigation!.GetFocused () as Button;

                                             btn.Accepting += (sender, e) => { btnAcceptCount++; };

                                             // Click
                                             Application.RaiseKeyDownEvent (Key.Enter);

                                             break;

                                         default:
                                             Assert.Fail ();

                                             break;
                                     }
                                 };
        Application.Run ().Dispose ();

        Assert.Equal (1, result);
        Assert.Equal (1, btnAcceptCount);
    }

    [Fact]
    [AutoInitShutdown]
    public void KeyBindings_Esc_Closes ()
    {
        var result = 999;

        var iteration = 0;

        Application.Iteration += (s, a) =>
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
                                 };
        Application.Run ().Dispose ();

        Assert.Equal (-1, result);
    }

    [Fact]
    [AutoInitShutdown]
    public void KeyBindings_Space_Causes_Focused_Button_Click_No_Accept ()
    {
        int result = -1;

        var iteration = 0;

        int btnAcceptCount = 0;

        Application.Iteration += (s, a) =>
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

                                             Button btn = Application.Navigation!.GetFocused () as Button;

                                             btn.Accepting += (sender, e) => { btnAcceptCount++; };

                                             Application.RaiseKeyDownEvent (Key.Space);

                                             break;

                                         default:
                                             Assert.Fail ();

                                             break;
                                     }
                                 };
        Application.Run ().Dispose ();

        Assert.Equal (1, result);
        Assert.Equal (1, btnAcceptCount);

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

        ((FakeDriver)Application.Driver!).SetBufferSize (15, 15); // 15 x 15 gives us enough room for a button with one char (9x1)
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        Rectangle mbFrame = Rectangle.Empty;

        Application.Iteration += (s, a) =>
                                 {
                                     iterations++;

                                     if (iterations == 0)
                                     {
                                         MessageBox.Query (string.Empty, message, 0, wrapMessage, hasButton ? ["0"] : []);
                                         Application.RequestStop ();
                                     }
                                     else if (iterations == 1)
                                     {
                                         mbFrame = Application.Top!.Frame;
                                         Application.RequestStop ();
                                     }
                                 };

        Application.Run ().Dispose ();

        Assert.Equal (new (expectedX, expectedY, expectedW, expectedH), mbFrame);
    }

    [Fact]
    [AutoInitShutdown]
    public void Message_With_Spaces_WrapMessage_False ()
    {
        int iterations = -1;
        var top = new Toplevel ();
        top.BorderStyle = LineStyle.None;
        ((FakeDriver)Application.Driver!).SetBufferSize (20, 10);

        var btn =
            $"{CM.Glyphs.LeftBracket}{CM.Glyphs.LeftDefaultIndicator} btn {CM.Glyphs.RightDefaultIndicator}{CM.Glyphs.RightBracket}";

        // Override CM
        MessageBox.DefaultButtonAlignment = Alignment.End;
        MessageBox.DefaultBorderStyle = LineStyle.Double;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        Application.Iteration += (s, a) =>
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
                                     else if (iterations == 1)
                                     {
                                         Application.Refresh ();

                                         TestHelpers.AssertDriverContentsWithFrameAre (
                                                                                       @"
 ╔════════════════╗
 ║ ff ff ff ff ff ║
 ║       ⟦► btn ◄⟧║
 ╚════════════════╝",
                                                                                       _output
                                                                                      );
                                         Application.RequestStop ();

                                         // Really long text
                                         MessageBox.Query (string.Empty, new string ('f', 500), 0, false, "btn");
                                     }
                                     else if (iterations == 2)
                                     {
                                         Application.Refresh ();

                                         TestHelpers.AssertDriverContentsWithFrameAre (
                                                                                       @"
 ╔════════════════╗
 ║ffffffffffffffff║
 ║       ⟦► btn ◄⟧║
 ╚════════════════╝",
                                                                                       _output
                                                                                      );
                                         Application.RequestStop ();
                                     }
                                 };

        Application.Run (top);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Message_With_Spaces_WrapMessage_True ()
    {
        int iterations = -1;
        var top = new Toplevel ();
        top.BorderStyle = LineStyle.None;
        ((FakeDriver)Application.Driver!).SetBufferSize (20, 10);

        var btn =
            $"{CM.Glyphs.LeftBracket}{CM.Glyphs.LeftDefaultIndicator} btn {CM.Glyphs.RightDefaultIndicator}{CM.Glyphs.RightBracket}";

        // Override CM
        MessageBox.DefaultButtonAlignment = Alignment.End;
        MessageBox.DefaultBorderStyle = LineStyle.Double;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        Application.Iteration += (s, a) =>
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
                                     else if (iterations == 1)
                                     {
                                         Application.Refresh ();

                                         TestHelpers.AssertDriverContentsWithFrameAre (
                                                                                       @"
  ╔══════════════╗
  ║ff ff ff ff ff║
  ║ff ff ff ff ff║
  ║ff ff ff ff ff║
  ║    ff ff     ║
  ║     ⟦► btn ◄⟧║
  ╚══════════════╝",
                                                                                       _output
                                                                                      );
                                         Application.RequestStop ();

                                         // Really long text
                                         MessageBox.Query (string.Empty, new string ('f', 500), 0, true, "btn");
                                     }
                                     else if (iterations == 2)
                                     {
                                         Application.Refresh ();

                                         TestHelpers.AssertDriverContentsWithFrameAre (
                                                                                       @$"
 ╔════════════════╗
 ║ffffffffffffffff║
 ║ffffffffffffffff║
 ║ffffffffffffffff║
 ║ffffffffffffffff║
 ║ffffffffffffffff║
 ║ffffffffffffffff║
 ║fffffff⟦► btn ◄⟧║
 ╚════════════════╝",
                                                                                       _output
                                                                                      );
                                         Application.RequestStop ();
                                     }
                                 };

        Application.Run (top);
        top.Dispose ();
    }

    [Theory]
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
        ((FakeDriver)Application.Driver!).SetBufferSize (100, 100);

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
                                         Application.Refresh ();

                                         Assert.IsType<Dialog> (Application.Top);
                                         Assert.Equal (new (height, width), Application.Top.Frame.Size);

                                         Application.RequestStop ();
                                     }
                                 };
    }

    [Theory]
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
        ((FakeDriver)Application.Driver!).SetBufferSize (100, 100);

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
                                         Application.Refresh ();

                                         Assert.IsType<Dialog> (Application.Top);
                                         Assert.Equal (new (height, width), Application.Top.Frame.Size);

                                         Application.RequestStop ();
                                     }
                                 };
    }

    [Theory]
    [InlineData (0, 0)]
    [InlineData (1, 1)]
    [InlineData (7, 5)]
    [InlineData (50, 50)]
    [AutoInitShutdown]
    public void Size_Not_Default_No_Message (int height, int width)
    {
        int iterations = -1;
        ((FakeDriver)Application.Driver!).SetBufferSize (100, 100);

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
                                         Application.Refresh ();

                                         Assert.IsType<Dialog> (Application.Top);
                                         Assert.Equal (new (height, width), Application.Top.Frame.Size);

                                         Application.RequestStop ();
                                     }
                                 };
    }

    [Fact]
    [AutoInitShutdown]
    public void UICatalog_AboutBox ()
    {
        int iterations = -1;
        ((FakeDriver)Application.Driver).SetBufferSize (70, 15);

        // Override CM
        MessageBox.DefaultButtonAlignment = Alignment.End;
        MessageBox.DefaultBorderStyle = LineStyle.Double;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        Application.Iteration += (s, a) =>
                                 {
                                     iterations++;

                                     if (iterations == 0)
                                     {
                                         MessageBox.Query (
                                                           title: "",
                                                           message: UICatalog.UICatalogApp.GetAboutBoxMessage (),
                                                           wrapMessage: false,
                                                           buttons: "_Ok"
                                                          );

                                         Application.RequestStop ();
                                     }
                                     else if (iterations == 1)
                                     {
                                         Application.Refresh ();

                                         string expectedText = """
                                                               ┌────────────────────────────────────────────────────────────────────┐
                                                               │    ╔══════════════════════════════════════════════════════════╗    │
                                                               │    ║      UI Catalog: A comprehensive sample library for      ║    │
                                                               │    ║                                                          ║    │
                                                               │    ║ _______                  _             _   _____       _ ║    │
                                                               │    ║|__   __|                (_)           | | / ____|     (_)║    │
                                                               │    ║   | | ___ _ __ _ __ ___  _ _ __   __ _| || |  __ _   _ _ ║    │
                                                               │    ║   | |/ _ \ '__| '_ ` _ \| | '_ \ / _` | || | |_ | | | | |║    │
                                                               │    ║   | |  __/ |  | | | | | | | | | | (_| | || |__| | |_| | |║    │
                                                               │    ║   |_|\___|_|  |_| |_| |_|_|_| |_|\__,_|_(_)_____|\__,_|_|║    │
                                                               │    ║                                                          ║    │
                                                               │    ║                      v2 - Pre-Alpha                      ║    │
                                                               │    ║                                                  ⟦► Ok ◄⟧║    │
                                                               │    ╚══════════════════════════════════════════════════════════╝    │
                                                               └────────────────────────────────────────────────────────────────────┘
                                                               """;

                                         TestHelpers.AssertDriverContentsAre (expectedText, _output);

                                         Application.RequestStop ();
                                     }
                                 };

        var top = new Toplevel ();
        top.BorderStyle = LineStyle.Single;
        Application.Run (top);
        top.Dispose ();
    }
}

