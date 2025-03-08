using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class SubViewTests
{
    private readonly ITestOutputHelper _output;
    public SubViewTests (ITestOutputHelper output) { _output = output; }

    // TODO: This is a poor unit tests. Not clear what it's testing. Refactor.
    [Fact]
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
    }
}
