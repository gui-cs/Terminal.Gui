using System.ComponentModel;
using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class ViewCommandTests (ITestOutputHelper output)
{
    // OnAccept/Accept tests
    [Fact]
    public void Accept_Command_Raises ()
    {
        var view = new ViewEventTester ();
        Assert.False (view.HasFocus);

        Assert.False (view.InvokeCommand (Command.Accept)); // false means it was not handled

        Assert.Equal (1, view.OnAcceptCount);

        Assert.Equal (1, view.AcceptCount);

        Assert.True (view.HasFocus);
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

    [Fact]
    public void HotKey_Command_SetsFocus ()
    {
        var view = new View ();

        view.CanFocus = true;
        Assert.False (view.HasFocus);
        view.InvokeCommand (Command.HotKey);
        Assert.True (view.HasFocus);
    }

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

            if (!HandleOnAccept)
            {
                return base.OnAccept (args);
            }

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
            if (!HandleOnHotKeyCommand)
            {
                return base.OnHotKeyCommand (args);
            }


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

            if (!HandleOnSelect)
            {
                return base.OnSelect (args);
            }

            return HandleOnSelect;
        }

        public bool HandleSelect { get; set; }

    }
}
