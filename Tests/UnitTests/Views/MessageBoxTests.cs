#nullable enable
using System.Text;
using UICatalog;
using UnitTests;
using Xunit.Abstractions;

namespace ViewsTests;

public class MessageBoxTests (ITestOutputHelper output)
{
    [Theory (Skip = "Needs to be updated")]
    [InlineData (@"", false, false, 6, 6, 2, 2)]
    [InlineData (@"", false, true, 6, 6, 2, 3)]  // Button in Padding - width no longer auto-expands for button in edge case with no content
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
        app.Init (DriverRegistry.Names.ANSI);

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
    public void UICatalog_AboutBox ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        try
        {
            int iterations = -1;
            app.Driver!.SetScreenSize (70, 18);

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
                                       │   ║                                                           ║    │
                                       │   ║          https://github.com/gui-cs/Terminal.Gui           ║    │
                                       │   ║                                                           ║    │
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

}
