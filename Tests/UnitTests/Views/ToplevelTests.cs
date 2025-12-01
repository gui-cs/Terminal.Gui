namespace UnitTests.ViewsTests;

public class ToplevelTests
{
    [Fact]
    [AutoInitShutdown]
    public void Mouse_Drag_On_Top_With_Superview_Null ()
    {
        var win = new Window ();
        Runnable top = new ();
        top.Add (win);
        int iterations = -1;
        Window testWindow;

        Application.Iteration += OnApplicationOnIteration;

        Application.Run (top);
        Application.Iteration -= OnApplicationOnIteration;
        top.Dispose ();

        return;

        void OnApplicationOnIteration (object s, EventArgs<IApplication> a)
        {
            iterations++;

            if (iterations == 0)
            {
                Application.Driver?.SetScreenSize (15, 7);

                // Don't use MessageBox here; it's too complicated for this unit test; just use Window
                testWindow = new ()
                {
                    Text = "Hello",
                    X = 2,
                    Y = 2,
                    Width = 10,
                    Height = 3,
                    Arrangement = ViewArrangement.Movable
                };
                Application.Run (testWindow);
            }
            else if (iterations == 1)
            {
                Assert.Equal (new (2, 2), Application.TopRunnableView!.Frame.Location);
            }
            else if (iterations == 2)
            {
                Assert.Null (Application.Mouse.MouseGrabView);

                // Grab the mouse
                Application.RaiseMouseEvent (new () { ScreenPosition = new (3, 2), Flags = MouseFlags.Button1Pressed });

                Assert.Equal (Application.TopRunnableView!.Border, Application.Mouse.MouseGrabView);
                Assert.Equal (new (2, 2, 10, 3), Application.TopRunnableView.Frame);
            }
            else if (iterations == 3)
            {
                Assert.Equal (Application.TopRunnableView!.Border, Application.Mouse.MouseGrabView);

                // Drag to left
                Application.RaiseMouseEvent (
                                             new ()
                                             {
                                                 ScreenPosition = new (2, 2),
                                                 Flags = MouseFlags.Button1Pressed
                                                         | MouseFlags.ReportMousePosition
                                             });
                AutoInitShutdownAttribute.RunIteration ();

                Assert.Equal (Application.TopRunnableView.Border, Application.Mouse.MouseGrabView);
                Assert.Equal (new (1, 2, 10, 3), Application.TopRunnableView.Frame);
            }
            else if (iterations == 4)
            {
                Assert.Equal (Application.TopRunnableView!.Border, Application.Mouse.MouseGrabView);
                Assert.Equal (new (1, 2), Application.TopRunnableView.Frame.Location);

                Assert.Equal (Application.TopRunnableView.Border, Application.Mouse.MouseGrabView);
            }
            else if (iterations == 5)
            {
                Assert.Equal (Application.TopRunnableView!.Border, Application.Mouse.MouseGrabView);

                // Drag up
                Application.RaiseMouseEvent (new () { ScreenPosition = new (2, 1), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition });
                AutoInitShutdownAttribute.RunIteration ();

                Assert.Equal (Application.TopRunnableView!.Border, Application.Mouse.MouseGrabView);
                Assert.Equal (new (1, 1, 10, 3), Application.TopRunnableView.Frame);
            }
            else if (iterations == 6)
            {
                Assert.Equal (Application.TopRunnableView!.Border, Application.Mouse.MouseGrabView);
                Assert.Equal (new (1, 1), Application.TopRunnableView.Frame.Location);

                Assert.Equal (Application.TopRunnableView.Border, Application.Mouse.MouseGrabView);
                Assert.Equal (new (1, 1, 10, 3), Application.TopRunnableView.Frame);
            }
            else if (iterations == 7)
            {
                Assert.Equal (Application.TopRunnableView!.Border, Application.Mouse.MouseGrabView);

                // Ungrab the mouse
                Application.RaiseMouseEvent (new () { ScreenPosition = new (2, 1), Flags = MouseFlags.Button1Released });
                AutoInitShutdownAttribute.RunIteration ();

                Assert.Null (Application.Mouse.MouseGrabView);
            }
            else if (iterations == 8)
            {
                Application.RequestStop ();
            }
            else if (iterations == 9)
            {
                Application.RequestStop ();
            }
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void Mouse_Drag_On_Top_With_Superview_Not_Null ()
    {
        var win = new Window { X = 3, Y = 2, Width = 10, Height = 5, Arrangement = ViewArrangement.Movable };
        Runnable top = new ();
        top.Add (win);

        int iterations = -1;

        var movex = 0;
        var movey = 0;

        var location = new Rectangle (win.Frame.X, win.Frame.Y, 7, 3);

        Application.Iteration += OnApplicationOnIteration;

        Application.Run (top);
        Application.Iteration -= OnApplicationOnIteration;
        top.Dispose ();

        return;

        void OnApplicationOnIteration (object s, EventArgs<IApplication> a)
        {
            iterations++;

            if (iterations == 0)
            {
                Application.Driver?.SetScreenSize (30, 10);
            }
            else if (iterations == 1)
            {
                location = win.Frame;

                Assert.Null (Application.Mouse.MouseGrabView);

                // Grab the mouse
                Application.RaiseMouseEvent (new () { ScreenPosition = new (win.Frame.X, win.Frame.Y), Flags = MouseFlags.Button1Pressed });

                Assert.Equal (win.Border, Application.Mouse.MouseGrabView);
            }
            else if (iterations == 2)
            {
                Assert.Equal (win.Border, Application.Mouse.MouseGrabView);

                // Drag to left
                movex = 1;
                movey = 0;

                Application.RaiseMouseEvent (
                                             new ()
                                             {
                                                 ScreenPosition = new (win.Frame.X + movex, win.Frame.Y + movey),
                                                 Flags = MouseFlags.Button1Pressed
                                                         | MouseFlags.ReportMousePosition
                                             });

                Assert.Equal (win.Border, Application.Mouse.MouseGrabView);
            }
            else if (iterations == 3)
            {
                // we should have moved +1, +0
                Assert.Equal (win.Border, Application.Mouse.MouseGrabView);
                Assert.Equal (win.Border, Application.Mouse.MouseGrabView);
                location.Offset (movex, movey);
            }
            else if (iterations == 4)
            {
                Assert.Equal (win.Border, Application.Mouse.MouseGrabView);

                // Drag up
                movex = 0;
                movey = -1;

                Application.RaiseMouseEvent (
                                             new ()
                                             {
                                                 ScreenPosition = new (win.Frame.X + movex, win.Frame.Y + movey),
                                                 Flags = MouseFlags.Button1Pressed
                                                         | MouseFlags.ReportMousePosition
                                             });

                Assert.Equal (win.Border, Application.Mouse.MouseGrabView);
            }
            else if (iterations == 5)
            {
                // we should have moved +0, -1
                Assert.Equal (win.Border, Application.Mouse.MouseGrabView);
                location.Offset (movex, movey);
                Assert.Equal (location, win.Frame);
            }
            else if (iterations == 6)
            {
                Assert.Equal (win.Border, Application.Mouse.MouseGrabView);

                // Ungrab the mouse
                movex = 0;
                movey = 0;

                Application.RaiseMouseEvent (new () { ScreenPosition = new (win.Frame.X + movex, win.Frame.Y + movey), Flags = MouseFlags.Button1Released });

                Assert.Null (Application.Mouse.MouseGrabView);
            }
            else if (iterations == 7)
            {
                Application.RequestStop ();
            }
        }
    }


    [Fact]
    [AutoInitShutdown]
    public void Modal_As_Top_Will_Drag_Cleanly ()
    {
        // Don't use Dialog as a Top, use a Window instead - dialog has complex layout behavior that is not needed here.
        var window = new Window { Width = 10, Height = 3, Arrangement = ViewArrangement.Movable };

        window.Add (
                    new Label
                    {
                        X = Pos.Center (),
                        Y = Pos.Center (),
                        Width = Dim.Fill (),
                        Height = Dim.Fill (),
                        TextAlignment = Alignment.Center,
                        VerticalTextAlignment = Alignment.Center,
                        Text = "Test"
                    }
                   );

        SessionToken rs = Application.Begin (window);

        Assert.Null (Application.Mouse.MouseGrabView);
        Assert.Equal (new (0, 0, 10, 3), window.Frame);

        Application.RaiseMouseEvent (new () { ScreenPosition = new (0, 0), Flags = MouseFlags.Button1Pressed });

        AutoInitShutdownAttribute.RunIteration ();
        Assert.Equal (window.Border, Application.Mouse.MouseGrabView);

        Assert.Equal (new (0, 0, 10, 3), window.Frame);

        Application.RaiseMouseEvent (
                                     new ()
                                     {
                                         ScreenPosition = new (1, 1), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                     });

        AutoInitShutdownAttribute.RunIteration ();
        Assert.Equal (window.Border, Application.Mouse.MouseGrabView);
        Assert.Equal (new (1, 1, 10, 3), window.Frame);

        Application.End (rs);
        window.Dispose ();
    }
}
