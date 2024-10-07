using System.ComponentModel;
using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class ViewCommandTests (ITestOutputHelper output)
{
    #region OnAccept/Accept tests
    [Fact]
    public void Accept_Command_Raises_NoFocus ()
    {
        var view = new ViewEventTester ();
        Assert.False (view.HasFocus);

        Assert.False (view.InvokeCommand (Command.Accept)); // false means it was not handled

        Assert.Equal (1, view.OnAcceptedCount);

        Assert.Equal (1, view.AcceptedCount);

        Assert.False (view.HasFocus);
    }

    [Fact]
    public void Accept_Command_Handle_OnAccept_NoEvent ()
    {
        var view = new ViewEventTester ();
        Assert.False (view.HasFocus);

        view.HandleOnAccepted = true;
        Assert.True (view.InvokeCommand (Command.Accept));

        Assert.Equal (1, view.OnAcceptedCount);

        Assert.Equal (0, view.AcceptedCount);
    }

    [Fact]
    public void Accept_Handle_Event_OnAccept_Returns_True ()
    {
        var view = new View ();
        var acceptInvoked = false;

        view.Accepted += ViewOnAccept;

        bool? ret = view.InvokeCommand (Command.Accept);
        Assert.True (ret);
        Assert.True (acceptInvoked);

        return;

        void ViewOnAccept (object sender, HandledEventArgs e)
        {
            acceptInvoked = true;
            e.Handled = true;
        }
    }

    [Fact]
    public void Accept_Command_Invokes_Accept_Event ()
    {
        var view = new View ();
        var accepted = false;

        view.Accepted += ViewOnAccept;

        view.InvokeCommand (Command.Accept);
        Assert.True (accepted);

        return;

        void ViewOnAccept (object sender, HandledEventArgs e) { accepted = true; }
    }

    // Accept on subview should bubble up to parent
    [Fact]
    public void Accept_Command_Bubbles_Up_To_SuperView ()
    {
        var view = new ViewEventTester () { Id = "view" };
        var subview = new ViewEventTester () { Id = "subview" };
        view.Add (subview);

        subview.InvokeCommand (Command.Accept);
        Assert.Equal (1, subview.OnAcceptedCount);
        Assert.Equal (1, view.OnAcceptedCount);

        subview.HandleOnAccepted = true;
        subview.InvokeCommand (Command.Accept);
        Assert.Equal (2, subview.OnAcceptedCount);
        Assert.Equal (1, view.OnAcceptedCount);

        subview.HandleOnAccepted = false;
        subview.HandleAccepted = true;
        subview.InvokeCommand (Command.Accept);
        Assert.Equal (3, subview.OnAcceptedCount);
        Assert.Equal (1, view.OnAcceptedCount);

        // Add a super view to test deeper hierarchy
        var superView = new ViewEventTester () { Id = "superView" };
        superView.Add (view);

        subview.InvokeCommand (Command.Accept);
        Assert.Equal (4, subview.OnAcceptedCount);
        Assert.Equal (1, view.OnAcceptedCount);
        Assert.Equal (0, superView.OnAcceptedCount);

        subview.HandleAccepted = false;
        subview.InvokeCommand (Command.Accept);
        Assert.Equal (5, subview.OnAcceptedCount);
        Assert.Equal (2, view.OnAcceptedCount);
        Assert.Equal (1, superView.OnAcceptedCount);

        view.HandleAccepted = true;
        subview.InvokeCommand (Command.Accept);
        Assert.Equal (6, subview.OnAcceptedCount);
        Assert.Equal (3, view.OnAcceptedCount);
        Assert.Equal (1, superView.OnAcceptedCount);
    }

    [Fact]
    public void MouseClick_Does_Not_Invoke_Accept_Command ()
    {
        var view = new ViewEventTester ();
        view.NewMouseEvent (new () { Flags = MouseFlags.Button1Clicked, Position = Point.Empty, View = view });

        Assert.Equal (0, view.OnAcceptedCount);
    }

    #endregion OnAccept/Accept tests

    #region OnSelect/Select tests

    [Theory]
    [CombinatorialData]
    public void Select_Command_Raises_SetsFocus (bool canFocus)
    {
        var view = new ViewEventTester ()
        {
            CanFocus = canFocus
        };

        Assert.Equal (canFocus, view.CanFocus);
        Assert.False (view.HasFocus);

        Assert.Equal (canFocus, view.InvokeCommand (Command.Select));

        Assert.Equal (1, view.OnSelectedCount);

        Assert.Equal (1, view.SelectedCount);

        Assert.Equal (canFocus, view.HasFocus);
    }

    [Fact]
    public void Select_Command_Handle_OnSelect_NoEvent ()
    {
        var view = new ViewEventTester ();
        Assert.False (view.HasFocus);

        view.HandleOnSelected = true;
        Assert.True (view.InvokeCommand (Command.Select));

        Assert.Equal (1, view.OnSelectedCount);

        Assert.Equal (0, view.SelectedCount);
    }

    [Fact]
    public void Select_Handle_Event_OnSelected_Returns_True ()
    {
        var view = new View ();
        var SelectedInvoked = false;

        view.Selecting += ViewOnSelect;

        bool? ret = view.InvokeCommand (Command.Select);
        Assert.True (ret);
        Assert.True (SelectedInvoked);

        return;

        void ViewOnSelect (object sender, CommandEventArgs e)
        {
            SelectedInvoked = true;
            e.Cancel = true;
        }
    }

    [Fact]
    public void Select_Command_Invokes_Selected_Event ()
    {
        var view = new View ();
        var selected = false;

        view.Selecting += ViewOnSelected;

        view.InvokeCommand (Command.Select);
        Assert.True (selected);

        return;

        void ViewOnSelected (object sender, CommandEventArgs e) { selected = true; }
    }

    [Fact]
    public void MouseClick_Invokes_Select_Command ()
    {
        var view = new ViewEventTester ();
        view.NewMouseEvent (new () { Flags = MouseFlags.Button1Clicked, Position = Point.Empty, View = view });

        Assert.Equal (1, view.OnSelectedCount);
    }

    #endregion OnSelect/Select tests

    #region OnHotKey/HotKey tests

    [Fact]
    public void HotKey_Command_SetsFocus ()
    {
        var view = new View ();

        view.CanFocus = true;
        Assert.False (view.HasFocus);
        view.InvokeCommand (Command.HotKey);
        Assert.True (view.HasFocus);
    }

    #endregion OnHotKey/HotKey tests

    public class ViewEventTester : View
    {
        public ViewEventTester ()
        {
            CanFocus = true;

            Accepted += (s, a) =>
                      {
                          a.Handled = HandleAccepted;
                          AcceptedCount++;
                      };

            HotKeyHandled += (s, a) =>
                             {
                                 a.Handled = HandleHotKeyHandled;
                                 HotKeyHandledCount++;
                             };


            Selecting += (s, a) =>
                             {
                                 a.Cancel = HandleSelected;
                                 SelectedCount++;
                             };
        }

        public int OnAcceptedCount { get; set; }
        public int AcceptedCount { get; set; }
        public bool HandleOnAccepted { get; set; }

        /// <inheritdoc />
        protected override bool OnAccepted (HandledEventArgs args)
        {
            OnAcceptedCount++;

            return HandleOnAccepted;
        }

        public bool HandleAccepted { get; set; }

        public int OnHotKeyHandledCount { get; set; }
        public int HotKeyHandledCount { get; set; }
        public bool HandleOnHotKeyHandled { get; set; }

        /// <inheritdoc />
        protected override bool OnHotKeyHandled (HandledEventArgs args)
        {
            OnHotKeyHandledCount++;

            return HandleOnHotKeyHandled;
        }

        public bool HandleHotKeyHandled { get; set; }


        public int OnSelectedCount { get; set; }
        public int SelectedCount { get; set; }
        public bool HandleOnSelected { get; set; }

        /// <inheritdoc />
        protected override bool OnSelecting (CommandEventArgs args)
        {
            OnSelectedCount++;

            return HandleOnSelected;
        }

        public bool HandleSelected { get; set; }

    }
}
