using System.Text;
using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class SubviewTests
{
    private readonly ITestOutputHelper _output;
    public SubviewTests (ITestOutputHelper output) { _output = output; }

    [Fact]
    [AutoInitShutdown]
    public void GetTopSuperView_Test ()
    {
        var v1 = new View ();
        var fv1 = new FrameView ();
        fv1.Add (v1);
        var tf1 = new TextField ();
        var w1 = new Window ();
        w1.Add (fv1, tf1);
        var top1 = new Toplevel ();
        top1.Add (w1);

        var v2 = new View ();
        var fv2 = new FrameView ();
        fv2.Add (v2);
        var tf2 = new TextField ();
        var w2 = new Window ();
        w2.Add (fv2, tf2);
        var top2 = new Toplevel ();
        top2.Add (w2);

        Assert.Equal (top1, v1.GetTopSuperView ());
        Assert.Equal (top2, v2.GetTopSuperView ());

        v1.Dispose ();
        fv1.Dispose ();
        tf1.Dispose ();
        w1.Dispose ();
        top1.Dispose ();
        v2.Dispose ();
        fv2.Dispose ();
        tf2.Dispose ();
        w2.Dispose ();
        top2.Dispose ();
    }

    [Fact]
    [TestRespondersDisposed]
    public void Initialized_Event_Comparing_With_Added_Event ()
    {
        Application.Init (new FakeDriver ());

        var top = new Toplevel { Id = "0" }; // Frame: 0, 0, 80, 25; Viewport: 0, 0, 80, 25

        var winAddedToTop = new Window
        {
            Id = "t", Width = Dim.Fill (), Height = Dim.Fill ()
        }; // Frame: 0, 0, 80, 25; Viewport: 0, 0, 78, 23

        var v1AddedToWin = new View
        {
            Id = "v1", Width = Dim.Fill (), Height = Dim.Fill ()
        }; // Frame: 1, 1, 78, 23 (because Windows has a border)

        var v2AddedToWin = new View
        {
            Id = "v2", Width = Dim.Fill (), Height = Dim.Fill ()
        }; // Frame: 1, 1, 78, 23 (because Windows has a border)

        var svAddedTov1 = new View
        {
            Id = "sv1", Width = Dim.Fill (), Height = Dim.Fill ()
        }; // Frame: 1, 1, 78, 23 (same as it's superview v1AddedToWin)

        int tc = 0, wc = 0, v1c = 0, v2c = 0, sv1c = 0;

        winAddedToTop.Added += (s, e) =>
                               {
                                   Assert.Equal (e.SuperView.Frame.Width, winAddedToTop.Frame.Width);
                                   Assert.Equal (e.SuperView.Frame.Height, winAddedToTop.Frame.Height);
                               };

        v1AddedToWin.Added += (s, e) =>
                              {
                                  Assert.Equal (e.SuperView.Frame.Width, v1AddedToWin.Frame.Width);
                                  Assert.Equal (e.SuperView.Frame.Height, v1AddedToWin.Frame.Height);
                              };

        v2AddedToWin.Added += (s, e) =>
                              {
                                  Assert.Equal (e.SuperView.Frame.Width, v2AddedToWin.Frame.Width);
                                  Assert.Equal (e.SuperView.Frame.Height, v2AddedToWin.Frame.Height);
                              };

        svAddedTov1.Added += (s, e) =>
                             {
                                 Assert.Equal (e.SuperView.Frame.Width, svAddedTov1.Frame.Width);
                                 Assert.Equal (e.SuperView.Frame.Height, svAddedTov1.Frame.Height);
                             };

        top.Initialized += (s, e) =>
                           {
                               tc++;
                               Assert.Equal (1, tc);
                               Assert.Equal (1, wc);
                               Assert.Equal (1, v1c);
                               Assert.Equal (1, v2c);
                               Assert.Equal (1, sv1c);

                               Assert.True (top.CanFocus);
                               Assert.True (winAddedToTop.CanFocus);
                               Assert.False (v1AddedToWin.CanFocus);
                               Assert.False (v2AddedToWin.CanFocus);
                               Assert.False (svAddedTov1.CanFocus);

                               Application.LayoutAndDraw ();
                           };

        winAddedToTop.Initialized += (s, e) =>
                                     {
                                         wc++;
                                         Assert.Equal (top.Viewport.Width, winAddedToTop.Frame.Width);
                                         Assert.Equal (top.Viewport.Height, winAddedToTop.Frame.Height);
                                     };

        v1AddedToWin.Initialized += (s, e) =>
                                    {
                                        v1c++;

                                        // Top.Frame: 0, 0, 80, 25; Top.Viewport: 0, 0, 80, 25
                                        // BUGBUG: This is wrong, it should be 78, 23. This test has always been broken.
                                        // in no way should the v1AddedToWin.Frame be the same as the Top.Frame/Viewport
                                        // as it is a subview of winAddedToTop, which has a border!
                                        //Assert.Equal (top.Viewport.Width,  v1AddedToWin.Frame.Width);
                                        //Assert.Equal (top.Viewport.Height, v1AddedToWin.Frame.Height);
                                    };

        v2AddedToWin.Initialized += (s, e) =>
                                    {
                                        v2c++;

                                        // Top.Frame: 0, 0, 80, 25; Top.Viewport: 0, 0, 80, 25
                                        // BUGBUG: This is wrong, it should be 78, 23. This test has always been broken.
                                        // in no way should the v2AddedToWin.Frame be the same as the Top.Frame/Viewport
                                        // as it is a subview of winAddedToTop, which has a border!
                                        //Assert.Equal (top.Viewport.Width,  v2AddedToWin.Frame.Width);
                                        //Assert.Equal (top.Viewport.Height, v2AddedToWin.Frame.Height);
                                    };

        svAddedTov1.Initialized += (s, e) =>
                                   {
                                       sv1c++;

                                       // Top.Frame: 0, 0, 80, 25; Top.Viewport: 0, 0, 80, 25
                                       // BUGBUG: This is wrong, it should be 78, 23. This test has always been broken.
                                       // in no way should the svAddedTov1.Frame be the same as the Top.Frame/Viewport
                                       // because sv1AddedTov1 is a subview of v1AddedToWin, which is a subview of
                                       // winAddedToTop, which has a border!
                                       //Assert.Equal (top.Viewport.Width,  svAddedTov1.Frame.Width);
                                       //Assert.Equal (top.Viewport.Height, svAddedTov1.Frame.Height);
                                       Assert.False (svAddedTov1.CanFocus);
                                       //Assert.Throws<InvalidOperationException> (() => svAddedTov1.CanFocus = true);
                                       Assert.False (svAddedTov1.CanFocus);
                                   };

        v1AddedToWin.Add (svAddedTov1);
        winAddedToTop.Add (v1AddedToWin, v2AddedToWin);
        top.Add (winAddedToTop);

        Application.Iteration += (s, a) =>
                                 {
                                     Application.LayoutAndDraw ();
                                     top.Running = false;
                                 };

        Application.Run (top);
        top.Dispose ();
        Application.Shutdown ();

        Assert.Equal (1, tc);
        Assert.Equal (1, wc);
        Assert.Equal (1, v1c);
        Assert.Equal (1, v2c);
        Assert.Equal (1, sv1c);

        Assert.True (top.CanFocus);
        Assert.True (winAddedToTop.CanFocus);
        Assert.False (v1AddedToWin.CanFocus);
        Assert.False (v2AddedToWin.CanFocus);
        Assert.False (svAddedTov1.CanFocus);

        v1AddedToWin.CanFocus = true;
        Assert.False (svAddedTov1.CanFocus); // False because sv1 was disposed and it isn't a subview of v1.
    }

    [Fact]
    [TestRespondersDisposed]
    public void Initialized_Event_Will_Be_Invoked_When_Added_Dynamically ()
    {
        Application.Init (new FakeDriver ());

        var t = new Toplevel { Id = "0" };

        var w = new Window { Id = "t", Width = Dim.Fill (), Height = Dim.Fill () };
        var v1 = new View { Id = "v1", Width = Dim.Fill (), Height = Dim.Fill () };
        var v2 = new View { Id = "v2", Width = Dim.Fill (), Height = Dim.Fill () };

        int tc = 0, wc = 0, v1c = 0, v2c = 0, sv1c = 0;

        t.Initialized += (s, e) =>
                         {
                             tc++;
                             Assert.Equal (1, tc);
                             Assert.Equal (1, wc);
                             Assert.Equal (1, v1c);
                             Assert.Equal (1, v2c);
                             Assert.Equal (0, sv1c); // Added after t in the Application.Iteration.

                             Assert.True (t.CanFocus);
                             Assert.True (w.CanFocus);
                             Assert.False (v1.CanFocus);
                             Assert.False (v2.CanFocus);

                             Application.LayoutAndDraw ();
                         };

        w.Initialized += (s, e) =>
                         {
                             wc++;
                             Assert.Equal (t.Viewport.Width, w.Frame.Width);
                             Assert.Equal (t.Viewport.Height, w.Frame.Height);
                         };

        v1.Initialized += (s, e) =>
                          {
                              v1c++;

                              //Assert.Equal (t.Viewport.Width, v1.Frame.Width);
                              //Assert.Equal (t.Viewport.Height, v1.Frame.Height);
                          };

        v2.Initialized += (s, e) =>
                          {
                              v2c++;

                              //Assert.Equal (t.Viewport.Width,  v2.Frame.Width);
                              //Assert.Equal (t.Viewport.Height, v2.Frame.Height);
                          };
        w.Add (v1, v2);
        t.Add (w);

        Application.Iteration += (s, a) =>
                                 {
                                     var sv1 = new View { Id = "sv1", Width = Dim.Fill (), Height = Dim.Fill () };

                                     sv1.Initialized += (s, e) =>
                                                        {
                                                            sv1c++;
                                                            Assert.NotEqual (t.Frame.Width, sv1.Frame.Width);
                                                            Assert.NotEqual (t.Frame.Height, sv1.Frame.Height);
                                                            Assert.False (sv1.CanFocus);
                                                            //Assert.Throws<InvalidOperationException> (() => sv1.CanFocus = true);
                                                            Assert.False (sv1.CanFocus);
                                                        };

                                     v1.Add (sv1);

                                     Application.LayoutAndDraw ();
                                     t.Running = false;
                                 };

        Application.Run (t);
        t.Dispose ();
        Application.Shutdown ();

        Assert.Equal (1, tc);
        Assert.Equal (1, wc);
        Assert.Equal (1, v1c);
        Assert.Equal (1, v2c);
        Assert.Equal (1, sv1c);

        Assert.True (t.CanFocus);
        Assert.True (w.CanFocus);
        Assert.False (v1.CanFocus);
        Assert.False (v2.CanFocus);
    }}
