using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using JetBrains.Annotations;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ASCIICustomButtonTest", "ASCIICustomButton sample")]
[ScenarioCategory ("Controls")]
public class ASCIICustomButtonTest : Scenario
{
    private static bool _smallerWindow;
    private MenuItem _miSmallerWindow;
    private ScrollViewTestWindow _scrollViewTestWindow;

    public override void Main ()
    {
        _smallerWindow = false;

        Application.Init ();
        Toplevel top = new ();

        var menu = new MenuBar
        {
            Menus =
            [
                new MenuBarItem (
                                 "_Window Size",
                                 new []
                                 {
                                     _miSmallerWindow =
                                         new MenuItem (
                                                       "Smaller Window",
                                                       "",
                                                       ChangeWindowSize
                                                      )
                                         {
                                             CheckType = MenuItemCheckStyle
                                                 .Checked
                                         },
                                     null,
                                     new MenuItem (
                                                   "Quit",
                                                   "",
                                                   () => Application.RequestStop (),
                                                   null,
                                                   null,
                                                   (KeyCode)Application.QuitKey
                                                  )
                                 }
                                )
            ]
        };

        _scrollViewTestWindow = new ScrollViewTestWindow { Y = Pos.Bottom (menu) };

        top.Add (menu, _scrollViewTestWindow);
        Application.Run (top);
        top.Dispose ();

        Application.Shutdown ();

        return;

        void ChangeWindowSize ()
        {
            _smallerWindow = (bool)(_miSmallerWindow.Checked = !_miSmallerWindow.Checked);
            top.Remove (_scrollViewTestWindow);
            _scrollViewTestWindow.Dispose ();

            _scrollViewTestWindow = new ScrollViewTestWindow ();
            top.Add (_scrollViewTestWindow);
        }
    }

    public class ASCIICustomButton : Button
    {
        private FrameView _border;
        private Label _fill;
        public string Description => $"Description of: {Id}";

        public void CustomInitialize ()
        {
            _border = new FrameView { Width = Width, Height = Height };

            var fillText = new StringBuilder ();

            for (var i = 0; i < Viewport.Height; i++)
            {
                if (i > 0)
                {
                    fillText.AppendLine ("");
                }

                for (var j = 0; j < Viewport.Width; j++)
                {
                    fillText.Append ("█");
                }
            }

            _fill = new Label { Visible = false, CanFocus = false, Text = fillText.ToString () };

            var title = new Label { X = Pos.Center (), Y = Pos.Center (), Text = Text };

            _border.MouseClick += This_MouseClick;
            _fill.MouseClick += This_MouseClick;
            title.MouseClick += This_MouseClick;

            Add (_border, _fill, title);
        }

        protected override void OnHasFocusChanged (bool newHasFocus, [CanBeNull] View previousFocusedView, [CanBeNull] View focusedVew)
        {
            if (newHasFocus)
            {
                _border.Visible = false;
                _fill.Visible = true;
                PointerEnter?.Invoke (this);
            }
            else
            {
                _border.Visible = true;
                _fill.Visible = false;
            }
        }

        public event Action<ASCIICustomButton> PointerEnter;
        private void This_MouseClick (object sender, MouseEventArgs obj) { NewMouseEvent (obj); }
    }

    public class ScrollViewTestWindow : Window
    {
        private const int BUTTON_HEIGHT = 3;
        private const int BUTTON_WIDTH = 25;
        private const int BUTTONS_ON_PAGE = 7;

        private readonly List<Button> _buttons;
        private readonly ScrollView _scrollView;
        private ASCIICustomButton _selected;

        public ScrollViewTestWindow ()
        {
            Title = $"{Application.QuitKey} to Quit - Scenario: ScrollViewTestWindow";

            Label titleLabel = null;

            if (_smallerWindow)
            {
                Width = 80;
                Height = 25;

                _scrollView = new ScrollView
                {
                    X = 3,
                    Y = 1,
                    Width = 24,
                    Height = BUTTONS_ON_PAGE * BUTTON_HEIGHT,
                    ShowVerticalScrollIndicator = true,
                    ShowHorizontalScrollIndicator = false
                };
            }
            else
            {
                Width = Dim.Fill ();
                Height = Dim.Fill ();

                titleLabel = new Label { X = 0, Y = 0, Text = "DOCUMENTS" };

                _scrollView = new ScrollView
                {
                    X = 0,
                    Y = 1,
                    Width = 27,
                    Height = BUTTONS_ON_PAGE * BUTTON_HEIGHT,
                    ShowVerticalScrollIndicator = true,
                    ShowHorizontalScrollIndicator = false
                };
            }

            _scrollView.KeyBindings.Clear ();

            _buttons = new List<Button> ();
            Button prevButton = null;
            var count = 20;

            for (var j = 0; j < count; j++)
            {
                Pos yPos = prevButton == null ? 0 : Pos.Bottom (prevButton);

                var button = new ASCIICustomButton
                {
                    Id = j.ToString (),
                    Text = $"section {j}",
                    Y = yPos,
                    Width = BUTTON_WIDTH,
                    Height = BUTTON_HEIGHT
                };
                button.Initialized += Button_Initialized;
                button.Accepting += Button_Clicked;
                button.PointerEnter += Button_PointerEnter;
                button.MouseClick += Button_MouseClick;
                button.KeyDown += Button_KeyPress;
                _scrollView.Add (button);
                _buttons.Add (button);
                prevButton = button;
            }

            var closeButton = new ASCIICustomButton
            {
                Id = "close",
                Text = "Close",
                Y = Pos.Bottom (prevButton),
                Width = BUTTON_WIDTH,
                Height = BUTTON_HEIGHT
            };
            closeButton.Initialized += Button_Initialized;
            closeButton.Accepting += Button_Clicked;
            closeButton.PointerEnter += Button_PointerEnter;
            closeButton.MouseClick += Button_MouseClick;
            closeButton.KeyDown += Button_KeyPress;
            _scrollView.Add (closeButton);
            _buttons.Add (closeButton);

            int pages = _buttons.Count / BUTTONS_ON_PAGE;

            if (_buttons.Count % BUTTONS_ON_PAGE > 0)
            {
                pages++;
            }

            // BUGBUG: set_ContentSize is supposed to be `protected`. 
            _scrollView.SetContentSize (new (25, pages * BUTTONS_ON_PAGE * BUTTON_HEIGHT));

            if (_smallerWindow)
            {
                Add (_scrollView);
            }
            else
            {
                Add (titleLabel, _scrollView);
            }

            Y = 1;
        }
        private void Button_Initialized (object sender, EventArgs e)
        {
            var button = sender as ASCIICustomButton;
            button?.CustomInitialize ();
        }

        private void Button_Clicked (object sender, EventArgs e)
        {
            MessageBox.Query ("Button clicked.", $"'{_selected.Text}' clicked!", "Ok");

            if (_selected.Text == "Close")
            {
                Application.RequestStop ();
            }
        }

        private void Button_KeyPress (object sender, Key obj)
        {
            switch (obj.KeyCode)
            {
                case KeyCode.End:
                    _scrollView.ContentOffset = new Point (
                                                           _scrollView.ContentOffset.X,
                                                           -(_scrollView.GetContentSize ().Height
                                                             - _scrollView.Frame.Height
                                                             + (_scrollView.ShowHorizontalScrollIndicator ? 1 : 0))
                                                          );
                    obj.Handled = true;

                    return;
                case KeyCode.Home:
                    _scrollView.ContentOffset = new Point (_scrollView.ContentOffset.X, 0);
                    obj.Handled = true;

                    return;
                case KeyCode.PageDown:
                    _scrollView.ContentOffset = new Point (
                                                           _scrollView.ContentOffset.X,
                                                           Math.Max (
                                                                     _scrollView.ContentOffset.Y
                                                                     - _scrollView.Frame.Height,
                                                                     -(_scrollView.GetContentSize ().Height
                                                                       - _scrollView.Frame.Height
                                                                       + (_scrollView.ShowHorizontalScrollIndicator
                                                                              ? 1
                                                                              : 0))
                                                                    )
                                                          );
                    obj.Handled = true;

                    return;
                case KeyCode.PageUp:
                    _scrollView.ContentOffset = new Point (
                                                           _scrollView.ContentOffset.X,
                                                           Math.Min (
                                                                     _scrollView.ContentOffset.Y
                                                                     + _scrollView.Frame.Height,
                                                                     0
                                                                    )
                                                          );
                    obj.Handled = true;

                    return;
            }
        }

        private void Button_MouseClick (object sender, MouseEventArgs obj)
        {
            if (obj.Flags == MouseFlags.WheeledDown)
            {
                _scrollView.ContentOffset = new Point (
                                                       _scrollView.ContentOffset.X,
                                                       _scrollView.ContentOffset.Y - BUTTON_HEIGHT
                                                      );
                obj.Handled = true;
            }
            else if (obj.Flags == MouseFlags.WheeledUp)
            {
                _scrollView.ContentOffset = new Point (
                                                       _scrollView.ContentOffset.X,
                                                       Math.Min (_scrollView.ContentOffset.Y + BUTTON_HEIGHT, 0)
                                                      );
                obj.Handled = true;
            }
        }

        private void Button_PointerEnter (ASCIICustomButton obj)
        {
            bool? moveDown;

            if (obj.Frame.Y > _selected?.Frame.Y)
            {
                moveDown = true;
            }
            else if (obj.Frame.Y < _selected?.Frame.Y)
            {
                moveDown = false;
            }
            else
            {
                moveDown = null;
            }

            int offSet = _selected != null
                             ? obj.Frame.Y - _selected.Frame.Y + -_scrollView.ContentOffset.Y % BUTTON_HEIGHT
                             : 0;
            _selected = obj;

            if (moveDown == true && _selected.Frame.Y + _scrollView.ContentOffset.Y + BUTTON_HEIGHT >= _scrollView.Frame.Height && offSet != BUTTON_HEIGHT)
            {
                _scrollView.ContentOffset = new Point (
                                                       _scrollView.ContentOffset.X,
                                                       Math.Min (
                                                                 _scrollView.ContentOffset.Y - BUTTON_HEIGHT,
                                                                 -(_selected.Frame.Y
                                                                   - _scrollView.Frame.Height
                                                                   + BUTTON_HEIGHT)
                                                                )
                                                      );
            }
            else if (moveDown == true && _selected.Frame.Y + _scrollView.ContentOffset.Y >= _scrollView.Frame.Height)
            {
                _scrollView.ContentOffset = new Point (
                                                       _scrollView.ContentOffset.X,
                                                       _scrollView.ContentOffset.Y - BUTTON_HEIGHT
                                                      );
            }
            else if (moveDown == true && _selected.Frame.Y + _scrollView.ContentOffset.Y < 0)
            {
                _scrollView.ContentOffset = new Point (
                                                       _scrollView.ContentOffset.X,
                                                       -_selected.Frame.Y
                                                      );
            }
            else if (moveDown == false && _selected.Frame.Y < -_scrollView.ContentOffset.Y)
            {
                _scrollView.ContentOffset = new Point (
                                                       _scrollView.ContentOffset.X,
                                                       Math.Max (
                                                                 _scrollView.ContentOffset.Y + BUTTON_HEIGHT,
                                                                 _selected.Frame.Y
                                                                )
                                                      );
            }
            else if (moveDown == false && _selected.Frame.Y + _scrollView.ContentOffset.Y > _scrollView.Frame.Height)
            {
                _scrollView.ContentOffset = new Point (
                                                       _scrollView.ContentOffset.X,
                                                       -(_selected.Frame.Y - _scrollView.Frame.Height + BUTTON_HEIGHT)
                                                      );
            }
        }
    }
}
