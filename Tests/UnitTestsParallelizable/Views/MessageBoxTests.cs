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
    public void Button_Enter_Or_Space_Returns_Default_Index (Key key)
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        try
        {
            app.Iteration += OnApplicationOnIteration;
            int? res = MessageBox.Query (app, "hey", "IsDefault", "_No", "_Yes");
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
