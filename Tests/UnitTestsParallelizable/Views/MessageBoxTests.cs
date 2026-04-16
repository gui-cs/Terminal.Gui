namespace ViewsTests;

public class MessageBoxTests
{
    [Fact]
    public void KeyBindings_Esc_Closes ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

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

    [Theory]
    [MemberData (nameof (AcceptingKeys))]
    public void Enter_Or_Space_Returns_Default_Button_Index (Key key)
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        try
        {
            app.Iteration += OnApplicationOnIteration;
            int? res = MessageBox.Query (app, "hey", "IsDefault", Strings.btnNo, Strings.btnYes);
            app.Iteration -= OnApplicationOnIteration;

            Assert.Equal (1, res);

            void OnApplicationOnIteration (object? o, EventArgs<IApplication?> iterationEventArgs) => Assert.True (app.Keyboard.RaiseKeyDownEvent (key));
        }
        finally
        {
            app.Dispose ();
        }
    }

    [Fact]
    public void Esc_Returns_Null ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        try
        {
            app.Iteration += OnApplicationOnIteration;
            int? res = MessageBox.Query (app, "hey", "IsDefault", "_No", "_Yes");
            app.Iteration -= OnApplicationOnIteration;

            Assert.Null (res);

            void OnApplicationOnIteration (object? o, EventArgs<IApplication?> iterationEventArgs) => Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Esc));
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

    // Copilot
    [Fact]
    public void Query_With_Default_Button_Index_Sets_Default_Button_And_Focus ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        try
        {
            int? result = null;
            var iteration = 0;

            app.Iteration += OnApplicationOnIteration;
            result = MessageBox.Query (app, "hey", "IsDefault", 0, Strings.btnNo, Strings.btnYes);
            app.Iteration -= OnApplicationOnIteration;

            Assert.Null (result);

            void OnApplicationOnIteration (object? sender, EventArgs<IApplication?> args)
            {
                iteration++;

                if (iteration != 1)
                {
                    return;
                }

                Dialog dialog = (Dialog)app.TopRunnableView!;
                Button [] buttons = dialog.Buttons;

                Assert.True (buttons [0].IsDefault);
                Assert.False (buttons [1].IsDefault);
                Assert.Same (buttons [0], dialog.DefaultAcceptView);
                Assert.True (buttons [0].HasFocus);

                app.RequestStop ();
            }
        }
        finally
        {
            app.Dispose ();
        }
    }

    // Copilot
    [Fact]
    public void ErrorQuery_With_Default_Button_Index_Sets_Default_Button_And_Focus ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        try
        {
            int? result = null;
            var iteration = 0;

            app.Iteration += OnApplicationOnIteration;
            result = MessageBox.ErrorQuery (app, "Error", "Message", 0, true, Strings.btnNo, Strings.btnYes);
            app.Iteration -= OnApplicationOnIteration;

            Assert.Null (result);

            void OnApplicationOnIteration (object? sender, EventArgs<IApplication?> args)
            {
                iteration++;

                if (iteration != 1)
                {
                    return;
                }

                Dialog dialog = (Dialog)app.TopRunnableView!;
                Button [] buttons = dialog.Buttons;

                Assert.True (buttons [0].IsDefault);
                Assert.False (buttons [1].IsDefault);
                Assert.Same (buttons [0], dialog.DefaultAcceptView);
                Assert.True (buttons [0].HasFocus);

                app.RequestStop ();
            }
        }
        finally
        {
            app.Dispose ();
        }
    }

    // Copilot
    [Theory]
    [MemberData (nameof (AcceptingKeys))]
    public void Enter_Or_Space_Returns_Explicit_Default_Button_Index (Key key)
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        try
        {
            app.Iteration += OnApplicationOnIteration;
            int? result = MessageBox.Query (app, "hey", "IsDefault", 0, Strings.btnNo, Strings.btnYes);
            app.Iteration -= OnApplicationOnIteration;

            Assert.Equal (0, result);

            void OnApplicationOnIteration (object? sender, EventArgs<IApplication?> args) => Assert.True (app.Keyboard.RaiseKeyDownEvent (key));
        }
        finally
        {
            app.Dispose ();
        }
    }

    // Copilot
    [Fact]
    public void Query_With_Out_Of_Range_Default_Button_Throws ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        try
        {
            Assert.Throws<ArgumentOutOfRangeException> (() => MessageBox.Query (app, "hey", "IsDefault", 2, Strings.btnNo, Strings.btnYes));
        }
        finally
        {
            app.Dispose ();
        }
    }

    // Claude - Opus 4.5
    [Fact]
    public void Query_Sets_Dialog_SchemeName ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        try
        {
            string? schemeName = null;
            var iteration = 0;

            app.Iteration += OnApplicationOnIteration;
            int? result = MessageBox.Query (app, "Test", "Message", "OK");
            app.Iteration -= OnApplicationOnIteration;

            Assert.Equal ("Dialog", schemeName);

            void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a)
            {
                iteration++;

                if (iteration == 1)
                {
                    // Capture the SchemeName from the running dialog
                    var dialog = app.TopRunnableView as Dialog;
                    Assert.NotNull (dialog);
                    schemeName = dialog.SchemeName;
                    app.RequestStop ();
                }
            }
        }
        finally
        {
            app.Dispose ();
        }
    }

    // Claude - Opus 4.5
    [Fact]
    public void ErrorQuery_Sets_Error_SchemeName ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        try
        {
            string? schemeName = null;
            var iteration = 0;

            app.Iteration += OnApplicationOnIteration;
            int? result = MessageBox.ErrorQuery (app, "Error", "Error Message", "OK");
            app.Iteration -= OnApplicationOnIteration;

            Assert.Equal ("Error", schemeName);

            void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a)
            {
                iteration++;

                if (iteration == 1)
                {
                    // Capture the SchemeName from the running dialog
                    var dialog = app.TopRunnableView as Dialog;
                    Assert.NotNull (dialog);
                    schemeName = dialog.SchemeName;
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
