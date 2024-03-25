using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.DialogTests;

public class MessageBoxTests
{
    private readonly ITestOutputHelper _output;
    public MessageBoxTests (ITestOutputHelper output) { _output = output; }

    [Fact]
    [AutoInitShutdown]
    public void KeyBindings_Enter_Causes_Focused_Button_Click ()
    {
        int result = -1;

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
                                             // Tab to btn2
                                             Application.OnKeyDown (Key.Tab);
                                             Application.OnKeyDown (Key.Enter);

                                             break;

                                         default:
                                             Assert.Fail ();

                                             break;
                                     }
                                 };
        Application.Run ();

        Assert.Equal (1, result);
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
                                             Application.OnKeyDown (Key.Esc);

                                             break;

                                         default:
                                             Assert.Fail ();

                                             break;
                                     }
                                 };
        Application.Run ();

        Assert.Equal (-1, result);
    }

    [Fact]
    [AutoInitShutdown]
    public void KeyBindings_Space_Causes_Focused_Button_Click ()
    {
        int result = -1;

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
                                             // Tab to btn2
                                             Application.OnKeyDown (Key.Tab);
                                             Application.OnKeyDown (Key.Space);

                                             break;

                                         default:
                                             Assert.Fail ();

                                             break;
                                     }
                                 };
        Application.Run ();

        Assert.Equal (1, result);
    }

    [Fact]
    [AutoInitShutdown]
    public void Location_Default ()
    {
        int iterations = -1;
        ((FakeDriver)Application.Driver).SetBufferSize (100, 100);

        Application.Iteration += (s, a) =>
                                 {
                                     iterations++;

                                     if (iterations == 0)
                                     {
                                         MessageBox.Query (string.Empty, string.Empty, null);

                                         Application.RequestStop ();
                                     }
                                     else if (iterations == 1)
                                     {
                                         Application.Refresh ();

                                         Assert.IsType<Dialog> (Application.Current);

                                         // Default location is centered, so
                                         // X = (100 / 2) - (60 / 2) = 20
                                         // Y = (100 / 2) - (5 / 2) = 47
                                         Assert.Equal (new Point (20, 47), (Point)Application.Current.Frame.Location);

                                         Application.RequestStop ();
                                     }
                                 };

        Application.Run ();
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (" ", true, 1)]
    [InlineData (" ", false, 1)]
    [InlineData ("", true, 1)]
    [InlineData ("", false, 1)]
    [InlineData ("\n", true, 1)]
    [InlineData ("\n", false, 1)]
    [InlineData (" \n", true, 1)]
    [InlineData (" \n", false, 2)]
    public void Message_Empty_Or_A_NewLline_WrapMessagge_True_Or_False (
        string message,
        bool wrapMessage,
        int linesLength
    )
    {
        int iterations = -1;

        Application.Iteration += (s, a) =>
                                 {
                                     iterations++;

                                     if (iterations == 0)
                                     {
                                         MessageBox.Query (string.Empty, message, 0, wrapMessage, "ok");

                                         Application.RequestStop ();
                                     }
                                     else if (iterations == 1)
                                     {
                                         Application.Refresh ();

                                         if (linesLength == 1)
                                         {
                                             TestHelpers.AssertDriverContentsWithFrameAre (
                                                                                           @$"
                ┌──────────────────────────────────────────────┐
                │                                              │
                │                                              │
                │                   {
                    CM.Glyphs.LeftBracket
                }{
                    CM.Glyphs.LeftDefaultIndicator
                } ok {
                    CM.Glyphs.RightDefaultIndicator
                }{
                    CM.Glyphs.RightBracket
                }                   │
                └──────────────────────────────────────────────┘",
                                                                                           _output
                                                                                          );
                                         }
                                         else
                                         {
                                             TestHelpers.AssertDriverContentsWithFrameAre (
                                                                                           @$"
                ┌──────────────────────────────────────────────┐
                │                                              │
                │                                              │
                │                                              │
                │                   {
                    CM.Glyphs.LeftBracket
                }{
                    CM.Glyphs.LeftDefaultIndicator
                } ok {
                    CM.Glyphs.RightDefaultIndicator
                }{
                    CM.Glyphs.RightBracket
                }                   │
                └──────────────────────────────────────────────┘",
                                                                                           _output
                                                                                          );
                                         }

                                         Application.RequestStop ();
                                     }
                                 };

        Application.Run ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Message_Long_Without_Spaces_WrapMessage_True ()
    {
        int iterations = -1;
        var top = new Toplevel ();
        top.BorderStyle = LineStyle.Double;
        ((FakeDriver)Application.Driver).SetBufferSize (20, 10);

        var btn =
            $"{
                CM.Glyphs.LeftBracket
            }{
                CM.Glyphs.LeftDefaultIndicator
            } btn {
                CM.Glyphs.RightDefaultIndicator
            }{
                CM.Glyphs.RightBracket
            }";

        Application.Iteration += (s, a) =>
                                 {
                                     iterations++;

                                     if (iterations == 0)
                                     {
                                         // 50 characters should make the height of the wrapped text 7
                                         MessageBox.Query (string.Empty, new string ('f', 50), 0, true, "btn");

                                         Application.RequestStop ();
                                     }
                                     else if (iterations == 1)
                                     {
                                         Application.Refresh ();

                                         TestHelpers.AssertDriverContentsWithFrameAre (
                                                                                       @$"
╔══════════════════╗
║┌────────────────┐║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│       ff       │║
║│                │║
║│    {
    btn
}   │║
║└────────────────┘║
╚══════════════════╝",
                                                                                       _output
                                                                                      );
                                         Assert.Equal (new (20 - 2, 10 - 2), Application.Current.Frame.Size);
                                         Application.RequestStop ();

                                         // Really long text
                                         MessageBox.Query (string.Empty, new string ('f', 500), 0, true, "btn");
                                     }
                                     else if (iterations == 2)
                                     {
                                         Application.Refresh ();

                                         TestHelpers.AssertDriverContentsWithFrameAre (
                                                                                       @$"
╔┌────────────────┐╗
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│    {
    btn
}   │║
╚└────────────────┘╝",
                                                                                       _output
                                                                                      );
                                         Application.RequestStop ();
                                     }
                                 };

        Application.Run (top);
    }

    [Fact]
    [AutoInitShutdown]
    public void Message_With_Spaces_WrapMessage_False ()
    {
        int iterations = -1;
        var top = new Toplevel ();
        top.BorderStyle = LineStyle.Double;
        ((FakeDriver)Application.Driver).SetBufferSize (20, 10);

        var btn =
            $"{
                CM.Glyphs.LeftBracket
            }{
                CM.Glyphs.LeftDefaultIndicator
            } btn {
                CM.Glyphs.RightDefaultIndicator
            }{
                CM.Glyphs.RightBracket
            }";

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
╔══════════════════╗
║                  ║
────────────────────
ff ff ff ff ff ff ff
                    
      ⟦► btn ◄⟧     
────────────────────
║                  ║
║                  ║
╚══════════════════╝",
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
╔══════════════════╗
║                  ║
────────────────────
ffffffffffffffffffff
                    
      ⟦► btn ◄⟧     
────────────────────
║                  ║
║                  ║
╚══════════════════╝",
                                                                                       _output
                                                                                      );
                                         Application.RequestStop ();
                                     }
                                 };

        Application.Run (top);
    }

    [Fact]
    [AutoInitShutdown]
    public void Message_With_Spaces_WrapMessage_True ()
    {
        int iterations = -1;
        var top = new Toplevel();
        top.BorderStyle = LineStyle.Double;
        ((FakeDriver)Application.Driver).SetBufferSize (20, 10);

        var btn =
            $"{
                CM.Glyphs.LeftBracket
            }{
                CM.Glyphs.LeftDefaultIndicator
            } btn {
                CM.Glyphs.RightDefaultIndicator
            }{
                CM.Glyphs.RightBracket
            }";

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
                                                                                       @$"
╔══════════════════╗
║ ┌──────────────┐ ║
║ │ff ff ff ff ff│ ║
║ │ff ff ff ff ff│ ║
║ │ff ff ff ff ff│ ║
║ │    ff ff     │ ║
║ │              │ ║
║ │   {
    btn
}  │ ║
║ └──────────────┘ ║
╚══════════════════╝",
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
╔┌────────────────┐╗
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│    {
    btn
}   │║
╚└────────────────┘╝",
                                                                                       _output
                                                                                      );
                                         Application.RequestStop ();
                                     }
                                 };

        Application.Run (top);
    }

    [Fact]
    [AutoInitShutdown]
    public void Message_Without_Spaces_WrapMessage_False ()
    {
        int iterations = -1;
        var top = new Toplevel();
        top.BorderStyle = LineStyle.Double;
        ((FakeDriver)Application.Driver).SetBufferSize (20, 10);

        var btn =
            $"{
                CM.Glyphs.LeftBracket
            }{
                CM.Glyphs.LeftDefaultIndicator
            } btn {
                CM.Glyphs.RightDefaultIndicator
            }{
                CM.Glyphs.RightBracket
            }";

        Application.Iteration += (s, a) =>
                                 {
                                     iterations++;

                                     if (iterations == 0)
                                     {
                                         MessageBox.Query (string.Empty, new string ('f', 50), 0, false, "btn");

                                         Application.RequestStop ();
                                     }
                                     else if (iterations == 1)
                                     {
                                         Application.Refresh ();

                                         TestHelpers.AssertDriverContentsWithFrameAre (
                                                                                       @"
╔══════════════════╗
║                  ║
────────────────────
ffffffffffffffffffff
                    
      ⟦► btn ◄⟧     
────────────────────
║                  ║
║                  ║
╚══════════════════╝",
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
╔══════════════════╗
║                  ║
────────────────────
ffffffffffffffffffff
                    
      ⟦► btn ◄⟧     
────────────────────
║                  ║
║                  ║
╚══════════════════╝",
                                                                                       _output
                                                                                      );

                                         Application.RequestStop ();
                                     }
                                 };

        Application.Run (top);
    }

    [Fact]
    [AutoInitShutdown]
    public void Size_Default ()
    {
        int iterations = -1;
        ((FakeDriver)Application.Driver).SetBufferSize (100, 100);

        Application.Iteration += (s, a) =>
                                 {
                                     iterations++;

                                     if (iterations == 0)
                                     {
                                         MessageBox.Query (string.Empty, string.Empty, null);

                                         Application.RequestStop ();
                                     }
                                     else if (iterations == 1)
                                     {
                                         Application.Refresh ();

                                         Assert.IsType<Dialog> (Application.Current);

                                         // Default size is Percent(60)
                                         Assert.Equal (new ((int)(100 * .60), 5), Application.Current.Frame.Size);

                                         Application.RequestStop ();
                                     }
                                 };

        Application.Run ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Size_JustBigEnough_Fixed_Size ()
    {
        int iterations = -1;

        var btn =
            $"{
                CM.Glyphs.LeftBracket
            }{
                CM.Glyphs.LeftDefaultIndicator
            } Ok {
                CM.Glyphs.RightDefaultIndicator
            }{
                CM.Glyphs.RightBracket
            }";

        Application.Iteration += (s, a) =>
                                 {
                                     iterations++;

                                     if (iterations == 0)
                                     {
                                         MessageBox.Query (11, 5, string.Empty, "Message", "_Ok");

                                         Application.RequestStop ();
                                     }
                                     else if (iterations == 1)
                                     {
                                         Application.Refresh ();

                                         TestHelpers.AssertDriverContentsWithFrameAre (
                                                                                       @$"
                                  ┌─────────┐
                                  │ Message │
                                  │         │
                                  │{
                                      btn
                                  } │
                                  └─────────┘
",
                                                                                       _output
                                                                                      );

                                         Application.RequestStop ();
                                     }
                                 };

        Application.Run ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Size_No_With_Button ()
    {
        var top = new Toplevel ();
        top.BorderStyle = LineStyle.Double;
        int iterations = -1;

        var aboutMessage = new StringBuilder ();
        aboutMessage.AppendLine (@"0123456789012345678901234567890123456789");
        aboutMessage.AppendLine (@"https://github.com/gui-cs/Terminal.Gui");
        var message = aboutMessage.ToString ();

        var btn =
            $"{
                CM.Glyphs.LeftBracket
            }{
                CM.Glyphs.LeftDefaultIndicator
            } Ok {
                CM.Glyphs.RightDefaultIndicator
            }{
                CM.Glyphs.RightBracket
            }";

        ((FakeDriver)Application.Driver).SetBufferSize (40 + 4, 8);

        Application.Iteration += (s, a) =>
                                 {
                                     iterations++;

                                     if (iterations == 0)
                                     {
                                         MessageBox.Query (string.Empty, message, "_Ok");

                                         Application.RequestStop ();
                                     }
                                     else if (iterations == 1)
                                     {
                                         Application.Refresh ();

                                         TestHelpers.AssertDriverContentsWithFrameAre (
                                                                                       @$"
╔══════════════════════════════════════════╗
║┌────────────────────────────────────────┐║
║│0123456789012345678901234567890123456789│║
║│ https://github.com/gui-cs/Terminal.Gui │║
║│                                        │║
║│                {
    btn
}                │║
║└────────────────────────────────────────┘║
╚══════════════════════════════════════════╝
",
                                                                                       _output
                                                                                      );

                                         Application.RequestStop ();
                                     }
                                 };

        Application.Run (top);
    }

    [Fact]
    [AutoInitShutdown]
    public void Size_None_No_Buttons ()
    {
        int iterations = -1;

        Application.Iteration += (s, a) =>
                                 {
                                     iterations++;

                                     if (iterations == 0)
                                     {
                                         MessageBox.Query ("Title", "Message");

                                         Application.RequestStop ();
                                     }
                                     else if (iterations == 1)
                                     {
                                         Application.Refresh ();

                                         TestHelpers.AssertDriverContentsWithFrameAre (
                                                                                       @"
                ┌┤Title├───────────────────────────────────────┐
                │                   Message                    │
                │                                              │
                │                                              │
                └──────────────────────────────────────────────┘
",
                                                                                       _output
                                                                                      );

                                         Application.RequestStop ();
                                     }
                                 };

        Application.Run ();
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
        ((FakeDriver)Application.Driver).SetBufferSize (100, 100);

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

                                         Assert.IsType<Dialog> (Application.Current);
                                         Assert.Equal (new (height, width), Application.Current.Frame.Size);

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
        ((FakeDriver)Application.Driver).SetBufferSize (100, 100);

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

                                         Assert.IsType<Dialog> (Application.Current);
                                         Assert.Equal (new (height, width), Application.Current.Frame.Size);

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
        ((FakeDriver)Application.Driver).SetBufferSize (100, 100);

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

                                         Assert.IsType<Dialog> (Application.Current);
                                         Assert.Equal (new (height, width), Application.Current.Frame.Size);

                                         Application.RequestStop ();
                                     }
                                 };
    }

    [Fact]
    [AutoInitShutdown]
    public void Size_Tiny_Fixed_Size ()
    {
        int iterations = -1;

        Application.Iteration += (s, a) =>
                                 {
                                     iterations++;

                                     if (iterations == 0)
                                     {
                                         MessageBox.Query (7, 5, string.Empty, "Message", "_Ok");

                                         Application.RequestStop ();
                                     }
                                     else if (iterations == 1)
                                     {
                                         Application.Refresh ();

                                         Assert.Equal (new (7, 5), Application.Current.Frame.Size);

                                         TestHelpers.AssertDriverContentsWithFrameAre (
                                                                                       @$"
                                    ┌─────┐
                                    │Messa│
                                    │ ge  │
                                    │ Ok {
                                        CM.Glyphs.RightDefaultIndicator
                                    }│
                                    └─────┘
",
                                                                                       _output
                                                                                      );

                                         Application.RequestStop ();
                                     }
                                 };

        Application.Run ();
    }
}
