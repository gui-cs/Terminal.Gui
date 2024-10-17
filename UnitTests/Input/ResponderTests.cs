// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui.InputTests;

public class ResponderTests
{
    // Generic lifetime (IDisposable) tests
    [Fact]
    [TestRespondersDisposed]
    public void Dispose_Works ()
    {
        var r = new Responder ();
#if DEBUG_IDISPOSABLE
        Assert.Single (Responder.Instances);
#endif

        r.Dispose ();
#if DEBUG_IDISPOSABLE
        Assert.Empty (Responder.Instances);
#endif
    }

    [Fact]
    public void Disposing_Event_Notify_All_Subscribers_On_The_First_Container ()
    {
    #if DEBUG_IDISPOSABLE
        // Only clear before because need to test after assert
        Responder.Instances.Clear ();
    #endif

        var container1 = new View { Id = "Container1" };
        var count = 0;

        var view = new View { Id = "View" };
        view.Disposing += View_Disposing;
        container1.Add (view);
        Assert.Equal (container1, view.SuperView);

        void View_Disposing (object sender, EventArgs e)
        {
            count++;
            Assert.Equal (view, sender);
            container1.Remove ((View)sender);
        }

        Assert.Single (container1.Subviews);

        var container2 = new View { Id = "Container2" };

        container2.Add (view);
        Assert.Equal (container2, view.SuperView);
        Assert.Equal (container1.Subviews.Count, container2.Subviews.Count);
        container2.Dispose ();

        Assert.Empty (container1.Subviews);
        Assert.Empty (container2.Subviews);
        Assert.Equal (1, count);
        Assert.Null (view.SuperView);

        container1.Dispose ();

    #if DEBUG_IDISPOSABLE
        Assert.Empty (Responder.Instances);
    #endif
    }

    [Fact]
    public void Disposing_Event_Notify_All_Subscribers_On_The_Second_Container ()
    {
    #if DEBUG_IDISPOSABLE
        // Only clear before because need to test after assert
        Responder.Instances.Clear ();
    #endif

        var container1 = new View { Id = "Container1" };

        var view = new View { Id = "View" };
        container1.Add (view);
        Assert.Equal (container1, view.SuperView);
        Assert.Single (container1.Subviews);

        var container2 = new View { Id = "Container2" };
        var count = 0;

        view.Disposing += View_Disposing;
        container2.Add (view);
        Assert.Equal (container2, view.SuperView);

        void View_Disposing (object sender, EventArgs e)
        {
            count++;
            Assert.Equal (view, sender);
            container2.Remove ((View)sender);
        }

        Assert.Equal (container1.Subviews.Count, container2.Subviews.Count);
        container1.Dispose ();

        Assert.Empty (container1.Subviews);
        Assert.Empty (container2.Subviews);
        Assert.Equal (1, count);
        Assert.Null (view.SuperView);

        container2.Dispose ();

    #if DEBUG_IDISPOSABLE
        Assert.Empty (Responder.Instances);
    #endif
    }

    [Fact]
    [TestRespondersDisposed]
    public void IsOverridden_False_IfNotOverridden ()
    {
        // MouseEvent IS defined on Responder but NOT overridden
        Assert.False (Responder.IsOverridden (new Responder (), "OnMouseEvent"));

        // MouseEvent is defined on Responder and NOT overrident on View
        Assert.False (
                      Responder.IsOverridden (
                                              new View { Text = "View does not override OnMouseEvent" },
                                              "OnMouseEvent"
                                             )
                     );

        Assert.False (
                      Responder.IsOverridden (
                                              new DerivedView { Text = "DerivedView does not override OnMouseEvent" },
                                              "OnMouseEvent"
                                             )
                     );

        // MouseEvent is NOT defined on DerivedView 
        Assert.False (
                      Responder.IsOverridden (
                                              new DerivedView { Text = "DerivedView does not override OnMouseEvent" },
                                              "OnMouseEvent"
                                             )
                     );

        // OnKeyDown is defined on View and NOT overrident on Button
        Assert.False (
                      Responder.IsOverridden (
                                              new Button { Text = "Button does not override OnKeyDown" },
                                              "OnKeyDown"
                                             )
                     );

#if DEBUG_IDISPOSABLE

        // HACK: Force clean up of Responders to avoid having to Dispose all the Views created above.
        Responder.Instances.Clear ();
        Assert.Empty (Responder.Instances);
#endif
    }

    [Fact]
    [TestRespondersDisposed]
    public void IsOverridden_True_IfOverridden ()
    {
        // MouseEvent is defined on Responder IS overriden on ScrollBarView (but not View)
        Assert.True (
                     Responder.IsOverridden (
                                             new ScrollBarView { Text = "ScrollBarView overrides OnMouseEvent" },
                                             "OnMouseEvent"
                                            )
                    );

        // OnKeyDown is defined on View
        Assert.False (Responder.IsOverridden (new View { Text = "View overrides OnKeyDown" }, "OnKeyDown"));

        // OnKeyDown is defined on DerivedView
        Assert.True (
                     Responder.IsOverridden (
                                             new DerivedView { Text = "DerivedView overrides OnKeyDown" },
                                             "OnKeyDown"
                                            )
                    );

        // ScrollBarView overrides both MouseEvent (from Responder) and Redraw (from View)
        Assert.True (
                     Responder.IsOverridden (
                                             new ScrollBarView { Text = "ScrollBarView overrides OnMouseEvent" },
                                             "OnMouseEvent"
                                            )
                    );

        Assert.True (
                     Responder.IsOverridden (
                                             new ScrollBarView { Text = "ScrollBarView overrides OnDrawContent" },
                                             "OnDrawContent"
                                            )
                    );
#if DEBUG_IDISPOSABLE

        // HACK: Force clean up of Responders to avoid having to Dispose all the Views created above.
        Responder.Instances.Clear ();
        Assert.Empty (Responder.Instances);
#endif
    }

    [Fact]
    public void KeyPressed_Handled_True_Cancels_KeyPress ()
    {
        var r = new View ();
        var args = new Key { KeyCode = KeyCode.Null };

        Assert.False (r.NewKeyDownEvent (args));
        Assert.False (args.Handled);

        r.KeyDown += (s, a) => a.Handled = true;
        Assert.True (r.NewKeyDownEvent (args));
        Assert.True (args.Handled);

        r.Dispose ();
    }

    [Fact]
    [TestRespondersDisposed]
    public void New_Initializes ()
    {
        var r = new Responder ();
        Assert.NotNull (r);
        Assert.Equal ("Terminal.Gui.Responder", r.ToString ());
        r.Dispose ();
    }

    [Fact]
    [TestRespondersDisposed]
    public void New_Methods_Return_False ()
    {
        var r = new View ();

        //Assert.False (r.OnKeyDown (new KeyEventArgs () { Key = Key.Unknown }));
        Assert.False (r.NewKeyDownEvent (new Key { KeyCode = KeyCode.Null }));
        Assert.False (r.NewKeyDownEvent (new Key { KeyCode = KeyCode.Null }));
        Assert.False (r.NewMouseEvent (new MouseEventArgs { Flags = MouseFlags.AllEvents }));

        var v = new View ();
        //Assert.False (r.OnEnter (v));
        v.Dispose ();

        v = new View ();
        //Assert.False (r.OnLeave (v));
        v.Dispose ();

        r.Dispose ();
    }

    [Fact]
    public void Responder_Not_Notifying_Dispose ()
    {
        // Only clear before because need to test after assert
    #if DEBUG_IDISPOSABLE
        Responder.Instances.Clear ();
    #endif
        var container1 = new View { Id = "Container1" };

        var view = new View { Id = "View" };
        container1.Add (view);
        Assert.Equal (container1, view.SuperView);

        Assert.Single (container1.Subviews);

        var container2 = new View { Id = "Container2" };

        container2.Add (view);
        Assert.Equal (container2, view.SuperView);
        Assert.Equal (container1.Subviews.Count, container2.Subviews.Count);
        container1.Dispose ();

        Assert.Empty (container1.Subviews);
        Assert.NotEmpty (container2.Subviews);
        Assert.Single (container2.Subviews);
        Assert.Null (view.SuperView);

        // Trying access disposed properties
    #if DEBUG_IDISPOSABLE
        Assert.True (container2.Subviews [0].WasDisposed);
    #endif
        Assert.False (container2.Subviews [0].CanFocus);
        Assert.Null (container2.Subviews [0].Margin);
        Assert.Null (container2.Subviews [0].Border);
        Assert.Null (container2.Subviews [0].Padding);
        Assert.Null (view.SuperView);

        container2.Dispose ();

    #if DEBUG_IDISPOSABLE
        Assert.Empty (Responder.Instances);
    #endif
    }

    public class DerivedView : View
    {
        protected override bool OnKeyDown (Key keyEvent) { return true; }
    }
}
