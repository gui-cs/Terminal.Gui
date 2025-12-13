#nullable enable
using System.Text;
using UICatalog;
using UnitTests;
using Xunit.Abstractions;

namespace ViewsTests;

public class MessageBoxTests (ITestOutputHelper output)
{
    [Fact]
    public void KeyBindings_Enter_Causes_Focused_Button_Click_No_Accept ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.FAKE);

        try
        {
            int? result = null;
            var iteration = 0;
            var btnAcceptCount = 0;

            app.Iteration += OnApplicationOnIteration;
            app.Run<Runnable<bool>> ();
            app.Iteration -= OnApplicationOnIteration;

            Assert.Equal (1, result);
            Assert.Equal (1, btnAcceptCount);

            void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a)
            {
                iteration++;

                switch (iteration)
                {
                    case 1:
                        result = MessageBox.Query (app, string.Empty, string.Empty, 0, false, "btn0", "btn1");
                        app.RequestStop ();

                        break;

                    case 2:
                        // Tab to btn2
                        app.Keyboard.RaiseKeyDownEvent (Key.Tab);

                        var btn = app.Navigation!.GetFocused () as Button;
                        btn!.Accepting += (sender, e) => { btnAcceptCount++; };

                        // Click
                        app.Keyboard.RaiseKeyDownEvent (Key.Enter);

                        break;

                    default:
                        Assert.Fail ();

                        break;
                }
            }
        }
        finally
        {
            app.Dispose ();
        }
    }

    [Fact]
    public void KeyBindings_Esc_Closes ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.FAKE);

        try
        {
            int? result = 999;
            var iteration = 0;

            app.Iteration += OnApplicationOnIteration;
            app.Run<Runnable<bool>> ();
            app.Iteration -= OnApplicationOnIteration;

            Assert.Null (result);

            void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a)
            {
                iteration++;

                switch (iteration)
                {
                    case 1:
                        result = MessageBox.Query (app, string.Empty, string.Empty, 0, false, "btn0", "btn1");
                        app.RequestStop ();

                        break;

                    case 2:
                        app.Keyboard.RaiseKeyDownEvent (Key.Esc);

                        break;

                    default:
                        Assert.Fail ();

                        break;
                }
            }
        }
        finally
        {
            app.Dispose ();
        }
    }

    [Fact]
    public void KeyBindings_Space_Causes_Focused_Button_Click_No_Accept ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.FAKE);

        try
        {
            int? result = null;
            var iteration = 0;
            var btnAcceptCount = 0;

            app.Iteration += OnApplicationOnIteration;
            app.Run<Runnable<bool>> ();
            app.Iteration -= OnApplicationOnIteration;

            Assert.Equal (1, result);
            Assert.Equal (1, btnAcceptCount);

            void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a)
            {
                iteration++;

                switch (iteration)
                {
                    case 1:
                        result = MessageBox.Query (app, string.Empty, string.Empty, 0, false, "btn0", "btn1");
                        app.RequestStop ();

                        break;

                    case 2:
                        // Tab to btn2
                        app.Keyboard.RaiseKeyDownEvent (Key.Tab);

                        var btn = app.Navigation!.GetFocused () as Button;
                        btn!.Accepting += (sender, e) => { btnAcceptCount++; };

                        app.Keyboard.RaiseKeyDownEvent (Key.Space);

                        break;

                    default:
                        Assert.Fail ();

                        break;
                }
            }
        }
        finally
        {
            app.Dispose ();
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
    public void Location_And_Size_Correct (string message, bool wrapMessage, bool hasButton, int expectedX, int expectedY, int expectedW, int expectedH)
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.FAKE);

        try
        {
            int iterations = -1;

            app.Driver!.SetScreenSize (15, 15); // 15 x 15 gives us enough room for a button with one char (9x1)
            Dialog.DefaultShadow = ShadowStyle.None;
            Button.DefaultShadow = ShadowStyle.None;

            var mbFrame = Rectangle.Empty;

            app.Iteration += OnApplicationOnIteration;
            app.Run<Runnable<bool>> ();
            app.Iteration -= OnApplicationOnIteration;

            Assert.Equal (new (expectedX, expectedY, expectedW, expectedH), mbFrame);

            void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a)
            {
                iterations++;

                if (iterations == 0)
                {
                    MessageBox.Query (app, string.Empty, message, 0, wrapMessage, hasButton ? ["0"] : []);
                    app.RequestStop ();
                }
                else if (iterations == 1)
                {
                    mbFrame = app.TopRunnableView!.Frame;
                    app.RequestStop ();
                }
            }
        }
        finally
        {
            app.Dispose ();
        }
    }

    [Fact]
    public void Message_With_Spaces_WrapMessage_False ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.FAKE);

        try
        {
            int iterations = -1;
            var top = new Runnable ();
            top.BorderStyle = LineStyle.None;
            app.Driver!.SetScreenSize (20, 10);

            var btn =
                $"{Glyphs.LeftBracket}{Glyphs.LeftDefaultIndicator} btn {Glyphs.RightDefaultIndicator}{Glyphs.RightBracket}";

            // Override CM
            MessageBox.DefaultButtonAlignment = Alignment.End;
            MessageBox.DefaultBorderStyle = LineStyle.Double;
            Dialog.DefaultShadow = ShadowStyle.None;
            Button.DefaultShadow = ShadowStyle.None;

            app.Iteration += OnApplicationOnIteration;
            try
            {
                app.Run (top);
            }
            finally
            {
                app.Iteration -= OnApplicationOnIteration;
                top.Dispose ();
            }

            void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a)
            {
                iterations++;

                if (iterations == 0)
                {
                    var sb = new StringBuilder ();

                    for (var i = 0; i < 17; i++)
                    {
                        sb.Append ("ff ");
                    }

                    MessageBox.Query (app, string.Empty, sb.ToString (), 0, false, "btn");
                    app.RequestStop ();
                }
                else if (iterations == 2)
                {
                    DriverAssert.AssertDriverContentsWithFrameAre (
                                                                   @"
 ╔════════════════╗
 ║ ff ff ff ff ff ║
 ║       ⟦► btn ◄⟧║
 ╚════════════════╝",
                                                                   output,
                                                                   app.Driver);
                    app.RequestStop ();

                    // Really long text
                    MessageBox.Query (app, string.Empty, new ('f', 500), 0, false, "btn");
                }
                else if (iterations == 4)
                {
                    DriverAssert.AssertDriverContentsWithFrameAre (
                                                                   @"
 ╔════════════════╗
 ║ffffffffffffffff║
 ║       ⟦► btn ◄⟧║
 ╚════════════════╝",
                                                                   output,
                                                                   app.Driver);
                    app.RequestStop ();
                }
            }
        }
        finally
        {
            app.Dispose ();
        }
    }

    [Fact]
    public void Message_With_Spaces_WrapMessage_True ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.FAKE);

        try
        {
            int iterations = -1;
            var top = new Runnable ();
            top.BorderStyle = LineStyle.None;
            app.Driver!.SetScreenSize (20, 10);

            var btn =
                $"{Glyphs.LeftBracket}{Glyphs.LeftDefaultIndicator} btn {Glyphs.RightDefaultIndicator}{Glyphs.RightBracket}";

            // Override CM
            MessageBox.DefaultButtonAlignment = Alignment.End;
            MessageBox.DefaultBorderStyle = LineStyle.Double;
            Dialog.DefaultShadow = ShadowStyle.None;
            Button.DefaultShadow = ShadowStyle.None;

            app.Iteration += OnApplicationOnIteration;
            try
            {
                app.Run (top);
            }
            finally
            {
                app.Iteration -= OnApplicationOnIteration;
                top.Dispose ();
            }

            void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a)
            {
                iterations++;

                if (iterations == 0)
                {
                    var sb = new StringBuilder ();

                    for (var i = 0; i < 17; i++)
                    {
                        sb.Append ("ff ");
                    }

                    MessageBox.Query (app, string.Empty, sb.ToString (), 0, true, "btn");
                    app.RequestStop ();
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
                                                                   output,
                                                                   app.Driver);
                    app.RequestStop ();

                    // Really long text
                    MessageBox.Query (app, string.Empty, new ('f', 500), 0, true, "btn");
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
                                                                   output,
                                                                   app.Driver);
                    app.RequestStop ();
                }
            }
        }
        finally
        {
            app.Dispose ();
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
    public void Size_Not_Default_Message (int height, int width, string message)
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.FAKE);

        try
        {
            int iterations = -1;
            app.Driver!.SetScreenSize (100, 100);

            app.Iteration += (s, a) =>
                             {
                                 iterations++;

                                 if (iterations == 0)
                                 {
                                     MessageBox.Query (app, height, width, string.Empty, message);
                                     app.RequestStop ();
                                 }
                                 else if (iterations == 1)
                                 {
                                     Assert.IsType<Dialog> (app.TopRunnableView);
                                     Assert.Equal (new (height, width), app.TopRunnableView.Frame.Size);
                                     app.RequestStop ();
                                 }
                             };
        }
        finally
        {
            app.Dispose ();
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
    public void Size_Not_Default_Message_Button (int height, int width, string message)
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.FAKE);

        try
        {
            int iterations = -1;

            app.Iteration += (s, a) =>
                             {
                                 iterations++;

                                 if (iterations == 0)
                                 {
                                     MessageBox.Query (app, height, width, string.Empty, message, "_Ok");
                                     app.RequestStop ();
                                 }
                                 else if (iterations == 1)
                                 {
                                     Assert.IsType<Dialog> (app.TopRunnableView);
                                     Assert.Equal (new (height, width), app.TopRunnableView.Frame.Size);
                                     app.RequestStop ();
                                 }
                             };
        }
        finally
        {
            app.Dispose ();
        }
    }

    [Theory (Skip = "Bogus test: Never does anything")]
    [InlineData (0, 0)]
    [InlineData (1, 1)]
    [InlineData (7, 5)]
    [InlineData (50, 50)]
    public void Size_Not_Default_No_Message (int height, int width)
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.FAKE);

        try
        {
            int iterations = -1;

            app.Iteration += (s, a) =>
                             {
                                 iterations++;

                                 if (iterations == 0)
                                 {
                                     MessageBox.Query (app, height, width, string.Empty, string.Empty);
                                     app.RequestStop ();
                                 }
                                 else if (iterations == 1)
                                 {
                                     Assert.IsType<Dialog> (app.TopRunnableView);
                                     Assert.Equal (new (height, width), app.TopRunnableView.Frame.Size);
                                     app.RequestStop ();
                                 }
                             };
        }
        finally
        {
            app.Dispose ();
        }
    }

    [Fact]
    public void UICatalog_AboutBox ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.FAKE);

        try
        {
            int iterations = -1;
            app.Driver!.SetScreenSize (70, 15);

            // Override CM
            MessageBox.DefaultButtonAlignment = Alignment.End;
            MessageBox.DefaultBorderStyle = LineStyle.Double;
            Dialog.DefaultShadow = ShadowStyle.None;
            Button.DefaultShadow = ShadowStyle.None;

            app.Iteration += OnApplicationOnIteration;

            var top = new Runnable ();
            top.BorderStyle = LineStyle.Single;
            try
            {
                app.Run (top);
            }
            finally
            {
                app.Iteration -= OnApplicationOnIteration;
                top.Dispose ();
            }

            void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a)
            {
                iterations++;

                if (iterations == 0)
                {
                    MessageBox.Query (
                                      app,
                                      "",
                                      UICatalogRunnable.GetAboutBoxMessage (),
                                      wrapMessage: false,
                                      buttons: "_Ok");

                    app.RequestStop ();
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

                    DriverAssert.AssertDriverContentsAre (expectedText, output, app.Driver);

                    app.RequestStop ();
                }
            }
        }
        finally
        {
            app.Dispose ();
        }
    }

    [Theory]
    [MemberData (nameof (AcceptingKeys))]
    public void Button_IsDefault_True_Return_His_Index_On_Accepting (Key key)
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.FAKE);

        try
        {
            app.Iteration += OnApplicationOnIteration;
            int? res = MessageBox.Query (app, "hey", "IsDefault", "Yes", "No");
            app.Iteration -= OnApplicationOnIteration;

            Assert.Equal (0, res);

            void OnApplicationOnIteration (object? o, EventArgs<IApplication?> iterationEventArgs) { Assert.True (app.Keyboard.RaiseKeyDownEvent (key)); }
        }
        finally
        {
            app.Dispose ();
        }
    }

    public static IEnumerable<object []> AcceptingKeys ()
    {
        yield return [Key.Enter];
        yield return [Key.Space];
    }
}
