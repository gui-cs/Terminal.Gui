using System;
using System.Globalization;
using System.Text;
using System.Threading;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "RuneWidthGreaterThanOne", Description: "Test rune width greater than one")]
	[ScenarioCategory ("Controls")]
	public class RuneWidthGreaterThanOne : Scenario {
		private Label _label;
		private TextField _text;
		private Button _button;
		private Label _labelR;
		private Label _labelV;
		private Window _win;

		public override void Init (Toplevel top, ColorScheme colorScheme)
		{
			Application.Init ();

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("Margin", new MenuItem [] {
					new MenuItem ("With margin", "", WithMargin),
					new MenuItem ("Without margin", "", WithoutMargin)
				}),
				new MenuBarItem ("Draw Margin Frame", new MenuItem [] {
					new MenuItem ("With draw", "", WithDrawMargin),
					new MenuItem ("Without draw", "", WithoutDrawMargin)
				}),
				new MenuBarItem ("Runes length", new MenuItem [] {
					new MenuItem ("Wide", "", WideRunes),
					new MenuItem ("Narrow", "", NarrowRunes),
					new MenuItem ("Mixed", "", MixedRunes)
				})
			});

			_label = new Label (text: "あなたの名前を入力してください：") {
				X = Pos.Center (),
				Y = 0,
				ColorScheme = new ColorScheme () {
					Normal = Colors.Base.Focus
				}
			};
			_text = new TextField ("ティラミス") {
				X = Pos.Center (),
				Y = 2,
				Width = 20
			};
			_button = new Button (text: "こんにちはと言う") {
				X = Pos.Center (),
				Y = 4
			};
			_button.Clicked += () => MessageBox.Query ("こんにちはと言う", $"こんにちは {_text.Text}", "Ok");
			_labelR = new Label (text: "あなたの名前を入力してください") {
				X = Pos.AnchorEnd (30),
				Y = 18
			};
			_labelV = new Label (text: "あなたの名前を入力してください", TextDirection.TopBottom_RightLeft) {
				X = Pos.AnchorEnd (30),
				Y = Pos.Bottom (_labelR)
			};
			_win = new Window ("デモエムポンズ") {
				X = 5,
				Y = 5,
				Width = Dim.Fill (22),
				Height = Dim.Fill (5)
			};
			_win.Add (_label, _text, _button, _labelR, _labelV);
			Application.Top.Add (menu, _win);
			Application.Run ();
		}

		private void MixedRunes ()
		{
			_label.Text = "Enter your name 你:";
			_text.Text = "gui.cs 你:";
			_button.Text = "Say Hello 你";
			_button.Clicked += () => MessageBox.Query ("Say Hello 你", $"Hello {_text.Text}", "Ok");
			_labelR.X = Pos.AnchorEnd (21);
			_labelR.Y = 18;
			_labelR.Text = "This is a test text 你";
			_labelV.X = Pos.AnchorEnd (21);
			_labelV.Y = Pos.Bottom (_labelR);
			_labelV.Text = "This is a test text 你";
			_win.Title = "HACC Demo 你";
		}

		private void NarrowRunes ()
		{
			_label.Text = "Enter your name:";
			_text.Text = "gui.cs";
			_button.Text = "Say Hello";
			_button.Clicked += () => MessageBox.Query ("Say Hello", $"Hello {_text.Text}", "Ok");
			_labelR.X = Pos.AnchorEnd (19);
			_labelR.Y = 18;
			_labelR.Text = "This is a test text";
			_labelV.X = Pos.AnchorEnd (19);
			_labelV.Y = Pos.Bottom (_labelR);
			_labelV.Text = "This is a test text";
			_win.Title = "HACC Demo";
		}

		private void WideRunes ()
		{
			_label.Text = "あなたの名前を入力してください：";
			_text.Text = "ティラミス";
			_button.Text = "こんにちはと言う";
			_button.Clicked += () => MessageBox.Query ("こんにちはと言う", $"こんにちは {_text.Text}", "Ok");
			_labelR.X = Pos.AnchorEnd (29);
			_labelR.Y = 18;
			_labelR.Text = "あなたの名前を入力してください";
			_labelV.X = Pos.AnchorEnd (29);
			_labelV.Y = Pos.Bottom (_labelR);
			_labelV.Text = "あなたの名前を入力してください";
			_win.Title = "デモエムポンズ";
		}

		private void WithoutDrawMargin ()
		{
			_win.Border.BorderStyle = BorderStyle.None;
			_win.Border.DrawMarginFrame = false;
		}

		private void WithDrawMargin ()
		{
			_win.Border.DrawMarginFrame = true;
			_win.Border.BorderStyle = BorderStyle.Single;
		}

		private void WithoutMargin ()
		{
			_win.X = 0;
			_win.Y = 0;
			_win.Width = Dim.Fill ();
			_win.Height = Dim.Fill ();
		}

		private void WithMargin ()
		{
			_win.X = 5;
			_win.Y = 5;
			_win.Width = Dim.Fill (22);
			_win.Height = Dim.Fill (5);
		}

		public override void Run ()
		{
		}
	}
}