#nullable enable
using UICatalog;
using UnitTests;
using Xunit.Abstractions;

namespace ViewsTests;

public class MessageBoxTests (ITestOutputHelper output)
{
    [Fact]
    public void UICatalog_AboutBox ()
    {
        // Do not use modern Create here (non-parallel tests)
        IApplication app = Application.Instance;
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
                                      buttons: Strings.btnOk);

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
                                       │   ║                                                   ⟦► OK ◄⟧║    │
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
