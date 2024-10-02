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

        Assert.Equal (1, view.OnAcceptCount);

        Assert.Equal (1, view.AcceptCount);

        Assert.False (view.HasFocus);
    }

    [Fact]
    public void Accept_Command_Handle_OnAccept_NoEvent ()
    {
        var view = new ViewEventTester ();
        Assert.False (view.HasFocus);

        view.HandleOnAccept = true;
        Assert.True (view.InvokeCommand (Command.Accept));

        Assert.Equal (1, view.OnAcceptCount);

        Assert.Equal (0, view.AcceptCount);
    }

    [Fact]
    public void Accept_Handle_Event_OnAccept_Returns_True ()
    {
        var view = new View ();
        var acceptInvoked = false;

        view.Accept += ViewOnAccept;

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

        view.Accept += ViewOnAccept;

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
        Assert.Equal (1, subview.OnAcceptCount);
        Assert.Equal (1, view.OnAcceptCount);

        subview.HandleOnAccept = true;
        subview.InvokeCommand (Command.Accept);
        Assert.Equal (2, subview.OnAcceptCount);
        Assert.Equal (1, view.OnAcceptCount);

        subview.HandleOnAccept = false;
        subview.HandleAccept = true;
        subview.InvokeCommand (Command.Accept);
        Assert.Equal (3, subview.OnAcceptCount);
        Assert.Equal (1, view.OnAcceptCount);

        // Add a super view to test deeper hierarchy
        var superView = new ViewEventTester () { Id = "superView" };
        superView.Add (view);

        subview.InvokeCommand (Command.Accept);
        Assert.Equal (4, subview.OnAcceptCount);
        Assert.Equal (1, view.OnAcceptCount);
        Assert.Equal (0, superView.OnAcceptCount);

        subview.HandleAccept = false;
        subview.InvokeCommand (Command.Accept);
        Assert.Equal (5, subview.OnAcceptCount);
        Assert.Equal (2, view.OnAcceptCount);
        Assert.Equal (1, superView.OnAcceptCount);

        view.HandleAccept = true;
        subview.InvokeCommand (Command.Accept);
        Assert.Equal (6, subview.OnAcceptCount);
        Assert.Equal (3, view.OnAcceptCount);
        Assert.Equal (1, superView.OnAcceptCount);

    }

    #endregion OnAccept/Accept tests

    #region OnSelect/Select tests
    [Fact]
    public void Select_Command_Raises_NoFocus ()
    {
        var view = new ViewEventTester ();
        Assert.False (view.HasFocus);

        Assert.False (view.InvokeCommand (Command.Select)); // false means it was not handled

        Assert.Equal (1, view.OnSelectCount);

        Assert.Equal (1, view.SelectCount);

        Assert.False (view.HasFocus);
    }

    [Fact]
    public void Select_Command_Handle_OnSelect_NoEvent ()
    {
        var view = new ViewEventTester ();
        Assert.False (view.HasFocus);

        view.HandleOnSelect = true;
        Assert.True (view.InvokeCommand (Command.Select));

        Assert.Equal (1, view.OnSelectCount);

        Assert.Equal (0, view.SelectCount);
    }

    [Fact]
    public void Select_Handle_Event_OnSelect_Returns_True ()
    {
        var view = new View ();
        var SelectInvoked = false;

        view.Select += ViewOnSelect;

        bool? ret = view.InvokeCommand (Command.Select);
        Assert.True (ret);
        Assert.True (SelectInvoked);

        return;

        void ViewOnSelect (object sender, HandledEventArgs e)
        {
            SelectInvoked = true;
            e.Handled = true;
        }
    }

    [Fact]
    public void Select_Command_Invokes_Select_Event ()
    {
        var view = new View ();
        var Selected = false;

        view.Select += ViewOnSelect;

        view.InvokeCommand (Command.Select);
        Assert.True (Selected);

        return;

        void ViewOnSelect (object sender, HandledEventArgs e) { Selected = true; }
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

            Accept += (s, a) =>
                      {
                          a.Handled = HandleAccept;
                          AcceptCount++;
                      };

            HotKeyCommand += (s, a) =>
                             {
                                 a.Handled = HandleHotKeyCommand;
                                 HotKeyCommandCount++;
                             };


            Select += (s, a) =>
                             {
                                 a.Handled = HandleSelect;
                                 SelectCount++;
                             };
        }

        public int OnAcceptCount { get; set; }
        public int AcceptCount { get; set; }
        public bool HandleOnAccept { get; set; }

        /// <inheritdoc />
        protected override bool OnAccept (HandledEventArgs args)
        {
            OnAcceptCount++;

            return HandleOnAccept;
        }

        public bool HandleAccept { get; set; }

        public int OnHotKeyCommandCount { get; set; }
        public int HotKeyCommandCount { get; set; }
        public bool HandleOnHotKeyCommand { get; set; }

        /// <inheritdoc />
        protected override bool OnHotKeyCommand (HandledEventArgs args)
        {
            OnHotKeyCommandCount++;

            return HandleOnHotKeyCommand;
        }

        public bool HandleHotKeyCommand { get; set; }


        public int OnSelectCount { get; set; }
        public int SelectCount { get; set; }
        public bool HandleOnSelect { get; set; }

        /// <inheritdoc />
        protected override bool OnSelect (HandledEventArgs args)
        {
            OnSelectCount++;

            return HandleOnSelect;
        }

        public bool HandleSelect { get; set; }

    }
}
