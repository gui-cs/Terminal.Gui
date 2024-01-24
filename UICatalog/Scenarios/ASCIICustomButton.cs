using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Terminal.Gui;
namespace UICatalog.Scenarios;

[ScenarioMetadata ("ASCIICustomButtonTest", "ASCIICustomButton sample")]
[ScenarioCategory ("Controls")]
public class ASCIICustomButtonTest : Scenario {
	static bool _smallerWindow;
	MenuItem _miSmallerWindow;
	ScrollViewTestWindow _scrollViewTestWindow;

	public override void Init ()
	{
		Application.Init ();
		_scrollViewTestWindow = new ScrollViewTestWindow ();
		var menu = new MenuBar (new [] {
			new MenuBarItem ("Window Size", new [] {
				_miSmallerWindow = new MenuItem ("Smaller Window", "", ChangeWindowSize) {
					CheckType = MenuItemCheckStyle.Checked
				},
				null,
				new MenuItem ("Quit", "", () => Application.RequestStop (), null, null, (KeyCode)Application.QuitKey)
			})
		});
		Application.Top.Add (menu, _scrollViewTestWindow);
		Application.Run ();
	}

	void ChangeWindowSize ()
	{
		_smallerWindow = (bool)(_miSmallerWindow.Checked = !_miSmallerWindow.Checked);
		_scrollViewTestWindow.Dispose ();
		Application.Top.Remove (_scrollViewTestWindow);
		_scrollViewTestWindow = new ScrollViewTestWindow ();
		Application.Top.Add (_scrollViewTestWindow);
	}

	public override void Run ()
	{
	}

	public class ASCIICustomButton : Button {
		FrameView border;

		Label fill;
		public string Description => $"Description of: {Id}";

		public event Action<ASCIICustomButton> PointerEnter;

		public void CustomInitialize ()
		{
			border = new FrameView {
				Width = Width,
				Height = Height
			};

			AutoSize = false;

			var fillText = new StringBuilder ();
			for (var i = 0; i < Bounds.Height; i++) {
				if (i > 0) {
					fillText.AppendLine ("");
				}
				for (var j = 0; j < Bounds.Width; j++) {
					fillText.Append ("█");
				}
			}

			fill = new Label {
				Visible = false,
				CanFocus = false,
				Text = fillText.ToString ()
			};

			var title = new Label {
				X = Pos.Center (),
				Y = Pos.Center (),
				Text = Text
			};

			border.MouseClick += This_MouseClick;
			fill.MouseClick += This_MouseClick;
			title.MouseClick += This_MouseClick;

			Add (border, fill, title);
		}

		void This_MouseClick (object sender, MouseEventEventArgs obj) => OnMouseEvent (obj.MouseEvent);

		public override bool OnMouseEvent (MouseEvent mouseEvent)
		{
			Debug.WriteLine ($"{mouseEvent.Flags}");
			if (mouseEvent.Flags == MouseFlags.Button1Clicked) {
				if (!HasFocus && SuperView != null) {
					if (!SuperView.HasFocus) {
						SuperView.SetFocus ();
					}
					SetFocus ();
					SetNeedsDisplay ();
				}

				OnClicked ();
				return true;
			}
			return base.OnMouseEvent (mouseEvent);
		}

		public override bool OnEnter (View view)
		{
			border.Visible = false;
			fill.Visible = true;
			PointerEnter.Invoke (this);
			view = this;
			return base.OnEnter (view);
		}

		public override bool OnLeave (View view)
		{
			border.Visible = true;
			fill.Visible = false;
			if (view == null) {
				view = this;
			}
			return base.OnLeave (view);
		}
	}

	public class ScrollViewTestWindow : Window {
		const int BUTTONS_ON_PAGE = 7;
		const int BUTTON_WIDTH = 25;
		const int BUTTON_HEIGHT = 3;
		readonly List<Button> buttons;

		readonly ScrollView scrollView;
		ASCIICustomButton selected;

		public ScrollViewTestWindow ()
		{
			Title = "ScrollViewTestWindow";

			Label titleLabel = null;
			if (_smallerWindow) {
				Width = 80;
				Height = 25;

				scrollView = new ScrollView {
					X = 3,
					Y = 1,
					Width = 24,
					Height = BUTTONS_ON_PAGE * BUTTON_HEIGHT,
					ShowVerticalScrollIndicator = true,
					ShowHorizontalScrollIndicator = false
				};
			} else {
				Width = Dim.Fill ();
				Height = Dim.Fill ();

				titleLabel = new Label {
					X = 0,
					Y = 0,
					Text = "DOCUMENTS"
				};

				scrollView = new ScrollView {
					X = 0,
					Y = 1,
					Width = 27,
					Height = BUTTONS_ON_PAGE * BUTTON_HEIGHT,
					ShowVerticalScrollIndicator = true,
					ShowHorizontalScrollIndicator = false
				};
			}

			scrollView.KeyBindings.Clear ();

			buttons = new List<Button> ();
			Button prevButton = null;
			var count = 20;
			for (var j = 0; j < count; j++) {
				var yPos = prevButton == null ? 0 : Pos.Bottom (prevButton);
				var button = new ASCIICustomButton { Id = j.ToString (), Text = $"section {j}", Y = yPos, Width = BUTTON_WIDTH, Height = BUTTON_HEIGHT };
				button.CustomInitialize ();
				button.Clicked += Button_Clicked;
				button.PointerEnter += Button_PointerEnter;
				button.MouseClick += Button_MouseClick;
				button.KeyDown += Button_KeyPress;
				scrollView.Add (button);
				buttons.Add (button);
				prevButton = button;
			}

			var closeButton = new ASCIICustomButton { Id = "close", Text = "Close", Y = Pos.Bottom (prevButton), Width = BUTTON_WIDTH, Height = BUTTON_HEIGHT };
			closeButton.CustomInitialize ();
			closeButton.Clicked += Button_Clicked;
			closeButton.PointerEnter += Button_PointerEnter;
			closeButton.MouseClick += Button_MouseClick;
			closeButton.KeyDown += Button_KeyPress;
			scrollView.Add (closeButton);
			buttons.Add (closeButton);

			var pages = buttons.Count / BUTTONS_ON_PAGE;
			if (buttons.Count % BUTTONS_ON_PAGE > 0) {
				pages++;
			}

			scrollView.ContentSize = new Size (25, pages * BUTTONS_ON_PAGE * BUTTON_HEIGHT);
			if (_smallerWindow) {
				Add (scrollView);
			} else {
				Add (titleLabel, scrollView);
			}
		}

		void Button_KeyPress (object sender, Key obj)
		{
			switch (obj.KeyCode) {
			case KeyCode.End:
				scrollView.ContentOffset = new Point (scrollView.ContentOffset.X,
					-(scrollView.ContentSize.Height - scrollView.Frame.Height
					+ (scrollView.ShowHorizontalScrollIndicator ? 1 : 0)));
				obj.Handled = true;
				return;
			case KeyCode.Home:
				scrollView.ContentOffset = new Point (scrollView.ContentOffset.X, 0);
				obj.Handled = true;
				return;
			case KeyCode.PageDown:
				scrollView.ContentOffset = new Point (scrollView.ContentOffset.X,
					Math.Max (scrollView.ContentOffset.Y - scrollView.Frame.Height,
						-(scrollView.ContentSize.Height - scrollView.Frame.Height
						+ (scrollView.ShowHorizontalScrollIndicator ? 1 : 0))));
				obj.Handled = true;
				return;
			case KeyCode.PageUp:
				scrollView.ContentOffset = new Point (scrollView.ContentOffset.X,
					Math.Min (scrollView.ContentOffset.Y + scrollView.Frame.Height, 0));
				obj.Handled = true;
				return;
			}
		}

		void Button_MouseClick (object sender, MouseEventEventArgs obj)
		{
			if (obj.MouseEvent.Flags == MouseFlags.WheeledDown) {
				scrollView.ContentOffset = new Point (scrollView.ContentOffset.X,
					scrollView.ContentOffset.Y - BUTTON_HEIGHT);
				obj.Handled = true;
			} else if (obj.MouseEvent.Flags == MouseFlags.WheeledUp) {
				scrollView.ContentOffset = new Point (scrollView.ContentOffset.X,
					Math.Min (scrollView.ContentOffset.Y + BUTTON_HEIGHT, 0));
				obj.Handled = true;
			}
		}

		void Button_Clicked (object sender, EventArgs e)
		{
			MessageBox.Query ("Button clicked.", $"'{selected.Text}' clicked!", "Ok");
			if (selected.Text == "Close") {
				Application.RequestStop ();
			}
		}

		void Button_PointerEnter (ASCIICustomButton obj)
		{
			bool? moveDown;
			if (obj.Frame.Y > selected?.Frame.Y) {
				moveDown = true;
			} else if (obj.Frame.Y < selected?.Frame.Y) {
				moveDown = false;
			} else {
				moveDown = null;
			}
			var offSet = selected != null ? obj.Frame.Y - selected.Frame.Y + -scrollView.ContentOffset.Y % BUTTON_HEIGHT : 0;
			selected = obj;
			if (moveDown == true && selected.Frame.Y + scrollView.ContentOffset.Y + BUTTON_HEIGHT >= scrollView.Frame.Height && offSet != BUTTON_HEIGHT) {
				scrollView.ContentOffset = new Point (scrollView.ContentOffset.X,
					Math.Min (scrollView.ContentOffset.Y - BUTTON_HEIGHT, -(selected.Frame.Y - scrollView.Frame.Height + BUTTON_HEIGHT)));
			} else if (moveDown == true && selected.Frame.Y + scrollView.ContentOffset.Y >= scrollView.Frame.Height) {
				scrollView.ContentOffset = new Point (scrollView.ContentOffset.X,
					scrollView.ContentOffset.Y - BUTTON_HEIGHT);
			} else if (moveDown == true && selected.Frame.Y + scrollView.ContentOffset.Y < 0) {
				scrollView.ContentOffset = new Point (scrollView.ContentOffset.X,
					-selected.Frame.Y);
			} else if (moveDown == false && selected.Frame.Y < -scrollView.ContentOffset.Y) {
				scrollView.ContentOffset = new Point (scrollView.ContentOffset.X,
					Math.Max (scrollView.ContentOffset.Y + BUTTON_HEIGHT, selected.Frame.Y));
			} else if (moveDown == false && selected.Frame.Y + scrollView.ContentOffset.Y > scrollView.Frame.Height) {
				scrollView.ContentOffset = new Point (scrollView.ContentOffset.X,
					-(selected.Frame.Y - scrollView.Frame.Height + BUTTON_HEIGHT));
			}
		}
	}
}