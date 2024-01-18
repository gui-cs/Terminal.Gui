using System;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Adornments Demo", "Demonstrates Margin, Border, and Padding on Views.")]
[ScenarioCategory ("Layout"), ScenarioCategory ("Borders")]
public class Adornments : Scenario {

	public override void Init ()
	{
		Application.Init ();
		ConfigurationManager.Themes.Theme = Theme;
		ConfigurationManager.Apply ();
		Application.Top.ColorScheme = Colors.ColorSchemes [TopLevelColorScheme];

		var view = new Window { Title = "The Window" };
		var tf1 = new TextField ("TextField") { Width = 10 };
		var color = new ColorPicker () { Title = "BG", BoxHeight = 1, BoxWidth =1, X = Pos.AnchorEnd(11) };
		color.BorderStyle = LineStyle.RoundedDotted;
		color.ColorChanged += (s, e) => {
			color.SuperView.ColorScheme = new ColorScheme (color.SuperView.ColorScheme) {
				Normal = new Attribute(color.SuperView.ColorScheme.Normal.Foreground, e.Color)
			};
		};

		var button = new Button ("Press me!") {
			X = Pos.Center (),
			Y = Pos.Center ()
		};
		button.Clicked += (s, e) => MessageBox.Query (20, 7, "Hi", $"Am I a {view.GetType ().Name}?", "Yes", "No");

		var label = new TextView () {
			X = Pos.Center (),
			Y = Pos.Bottom (button),
			Title = "Title",
			Text = "I have a 3 row top border.\nMy border inherits from the SuperView.",
			Width = 40,
			Height = 6 // TODO: Use Dim.Auto
		};
		label.Border.Thickness = new Thickness (1, 3, 1, 1);

		var tf2 = new Button ("Button") {
			X = Pos.AnchorEnd (10),
			Y = Pos.AnchorEnd (1),
			Width = 10
		};
		var tv = new Label {
			Y = Pos.AnchorEnd (3),
			Width = 25,
			Height = Dim.Fill (),
			Text = "Label\nY=AnchorEnd(3),Height=Dim.Fill()"
		};

		view.Margin.Data = "Margin";
		view.Margin.Thickness = new Thickness (3);

		view.Border.Data = "Border";
		view.Border.Thickness = new Thickness (3);

		view.Padding.Data = "Padding";
		view.Padding.Thickness = new Thickness (3);

		view.Add (tf1, color, button, label, tf2, tv);

		var editor = new AdornmentsEditor {
			Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}",
			ColorScheme = Colors.ColorSchemes [TopLevelColorScheme],
		};
		view.X = 36;
		view.Y = 0;
		view.Width = Dim.Fill ();
		view.Height = Dim.Fill ();

		editor.Initialized += (s, e) => {
			editor.ViewToEdit = view;
		};
		//view.Margin.ColorScheme = new ColorScheme (Colors.ColorSchemes ["Dialog"]);
		//view.Border.ColorScheme = new ColorScheme (Colors.ColorSchemes ["Error"]);
		//view.Padding.ColorScheme = new ColorScheme (Colors.ColorSchemes ["Menu"]);

		Application.Run (editor);
		Application.Shutdown ();
	}

	public override void Run () { }

	public class AdornmentEditor : View {
		readonly ColorPicker _backgroundColorPicker = new () {
			Title = "BG",
			BoxWidth = 1,
			BoxHeight = 1,
			BorderStyle = LineStyle.Single,
			SuperViewRendersLineCanvas = true
		};

		readonly ColorPicker _foregroundColorPicker = new () {
			Title = "FG",
			BoxWidth = 1,
			BoxHeight = 1,
			BorderStyle = LineStyle.Single,
			SuperViewRendersLineCanvas = true
		};

		TextField _bottomEdit;
		bool _isUpdating;
		TextField _leftEdit;
		TextField _rightEdit;
		Thickness _thickness;
		TextField _topEdit;

		public AdornmentEditor ()
		{
			Margin.Thickness = new Thickness (0);
			BorderStyle = LineStyle.Double;
			Initialized += AdornmentEditor_Initialized;
		}

		public Attribute Color {
			get => new (_foregroundColorPicker.SelectedColor, _backgroundColorPicker.SelectedColor);
			set {
				_foregroundColorPicker.SelectedColor = value.Foreground.ColorName;
				_backgroundColorPicker.SelectedColor = value.Background.ColorName;
			}
		}

		public Thickness Thickness {
			get => _thickness;
			set {
				if (_isUpdating) {
					return;
				}
				_thickness = value;
				ThicknessChanged?.Invoke (this, new ThicknessEventArgs { Thickness = Thickness });
				if (IsInitialized) {
					_isUpdating = true;
					if (_topEdit.Text != _thickness.Top.ToString ()) {
						_topEdit.Text = _thickness.Top.ToString ();
					}
					if (_leftEdit.Text != _thickness.Left.ToString ()) {
						_leftEdit.Text = _thickness.Left.ToString ();
					}
					if (_rightEdit.Text != _thickness.Right.ToString ()) {
						_rightEdit.Text = _thickness.Right.ToString ();
					}
					if (_bottomEdit.Text != _thickness.Bottom.ToString ()) {
						_bottomEdit.Text = _thickness.Bottom.ToString ();
					}
					_isUpdating = false;
				}
			}
		}

		public event EventHandler<ThicknessEventArgs> ThicknessChanged;

		public event EventHandler<Attribute> AttributeChanged;

		void AdornmentEditor_Initialized (object sender, EventArgs e)
		{
			var editWidth = 3;

			_topEdit = new TextField ("") {
				X = Pos.Center (),
				Y = 0,
				Width = editWidth
			};
			_topEdit.TextChanging += Edit_TextChanging;
			Add (_topEdit);

			_leftEdit = new TextField ("") {
				X = Pos.Left (_topEdit) - editWidth,
				Y = Pos.Bottom (_topEdit),
				Width = editWidth
			};
			_leftEdit.TextChanging += Edit_TextChanging;
			Add (_leftEdit);

			_rightEdit = new TextField ("") {
				X = Pos.Right (_topEdit),
				Y = Pos.Bottom (_topEdit),
				Width = editWidth
			};
			_rightEdit.TextChanging += Edit_TextChanging;
			Add (_rightEdit);

			_bottomEdit = new TextField ("") {
				X = Pos.Center (),
				Y = Pos.Bottom (_leftEdit),
				Width = editWidth
			};
			_bottomEdit.TextChanging += Edit_TextChanging;
			Add (_bottomEdit);

			var copyTop = new Button ("Cop_y Top") {
				X = Pos.Center () + 1,
				Y = Pos.Bottom (_bottomEdit)
			};
			copyTop.Clicked += (s, e) => {
				Thickness = new Thickness (Thickness.Top);
				if (string.IsNullOrEmpty (_topEdit.Text)) {
					_topEdit.Text = "0";
				}
				_bottomEdit.Text = _leftEdit.Text = _rightEdit.Text = _topEdit.Text;
			};
			Add (copyTop);

			// Foreground ColorPicker.
			_foregroundColorPicker.X = -1;
			_foregroundColorPicker.Y = Pos.Bottom (copyTop) + 1;
			_foregroundColorPicker.SelectedColor = Color.Foreground.ColorName;
			_foregroundColorPicker.ColorChanged += (o, a) =>
				AttributeChanged?.Invoke (this,
					new Attribute (_foregroundColorPicker.SelectedColor, _backgroundColorPicker.SelectedColor));
			Add (_foregroundColorPicker);

			// Background ColorPicker.
			_backgroundColorPicker.X = Pos.Right (_foregroundColorPicker) - 1;
			_backgroundColorPicker.Y = Pos.Top (_foregroundColorPicker);
			_backgroundColorPicker.SelectedColor = Color.Background.ColorName;
			_backgroundColorPicker.ColorChanged += (o, a) =>
				AttributeChanged?.Invoke (this,
					new Attribute (
						_foregroundColorPicker.SelectedColor,
						_backgroundColorPicker.SelectedColor));
			Add (_backgroundColorPicker);

			_topEdit.Text = $"{Thickness.Top}";
			_leftEdit.Text = $"{Thickness.Left}";
			_rightEdit.Text = $"{Thickness.Right}";
			_bottomEdit.Text = $"{Thickness.Bottom}";

			LayoutSubviews ();
			Height = GetAdornmentsThickness ().Vertical + 4 + 4;
			Width = GetAdornmentsThickness ().Horizontal + _foregroundColorPicker.Frame.Width * 2 - 3;
		}

		void Edit_TextChanging (object sender, TextChangingEventArgs e)
		{
			try {
				if (string.IsNullOrEmpty (e.NewText)) {
					e.Cancel = true;
					((TextField)sender).Text = "0";
					return;
				}
				switch (sender.ToString ()) {
				case var s when s == _topEdit.ToString ():
					Thickness = new Thickness (Thickness.Left,
						int.Parse (e.NewText), Thickness.Right,
						Thickness.Bottom);
					break;
				case var s when s == _leftEdit.ToString ():
					Thickness = new Thickness (int.Parse (e.NewText),
						Thickness.Top, Thickness.Right,
						Thickness.Bottom);
					break;
				case var s when s == _rightEdit.ToString ():
					Thickness = new Thickness (Thickness.Left,
						Thickness.Top, int.Parse (e.NewText),
						Thickness.Bottom);
					break;
				case var s when s == _bottomEdit.ToString ():
					Thickness = new Thickness (Thickness.Left,
						Thickness.Top, Thickness.Right,
						int.Parse (e.NewText));
					break;
				}
			} catch {
				if (!string.IsNullOrEmpty (e.NewText)) {
					e.Cancel = true;
				}
			}
		}
	}

	public class AdornmentsEditor : Window {
		AdornmentEditor _borderEditor;
		CheckBox _diagCheckBox;
		AdornmentEditor _marginEditor;
		String _origTitle = string.Empty;
		AdornmentEditor _paddingEditor;
		View _viewToEdit;

		public View ViewToEdit {
			get => _viewToEdit;
			set {
				_origTitle = value.Title;
				_viewToEdit = value;

				_marginEditor = new AdornmentEditor {
					X = 0,
					Y = 0,
					Title = "Margin",
					Thickness = _viewToEdit.Margin.Thickness,
					Color = new Attribute (_viewToEdit.Margin.ColorScheme.Normal),
					SuperViewRendersLineCanvas = true
				};
				_marginEditor.ThicknessChanged += Editor_ThicknessChanged;
				_marginEditor.AttributeChanged += Editor_AttributeChanged;
				Add (_marginEditor);

				_borderEditor = new AdornmentEditor {
					X = Pos.Left (_marginEditor),
					Y = Pos.Bottom (_marginEditor),
					Title = "Border",
					Thickness = _viewToEdit.Border.Thickness,
					Color = new Attribute (_viewToEdit.Border.ColorScheme.Normal),
					SuperViewRendersLineCanvas = true
				};
				_borderEditor.ThicknessChanged += Editor_ThicknessChanged;
				_borderEditor.AttributeChanged += Editor_AttributeChanged;
				Add (_borderEditor);


				var borderStyleEnum = Enum.GetValues (typeof (LineStyle)).Cast<LineStyle> ().ToList ();
				var rbBorderStyle = new RadioGroup (borderStyleEnum.Select (
					e => e.ToString ()).ToArray ()) {

					X = Pos.Right (_borderEditor) - 1,
					Y = Pos.Top (_borderEditor),
					SelectedItem = (int)_viewToEdit.Border.LineStyle,
					BorderStyle = LineStyle.Double,
					Title = "Border Style",
					SuperViewRendersLineCanvas = true
				};
				Add (rbBorderStyle);

				rbBorderStyle.SelectedItemChanged += (s, e) => {
					var prevBorderStyle = _viewToEdit.BorderStyle;
					_viewToEdit.Border.LineStyle = (LineStyle)e.SelectedItem;
					if (_viewToEdit.Border.LineStyle == LineStyle.None) {
						_viewToEdit.Border.Thickness = new Thickness (0);
					} else if (prevBorderStyle == LineStyle.None && _viewToEdit.Border.LineStyle != LineStyle.None) {
						_viewToEdit.Border.Thickness = new Thickness (1);
					}
					_borderEditor.Thickness = new Thickness (_viewToEdit.Border.Thickness.Left, _viewToEdit.Border.Thickness.Top,
						_viewToEdit.Border.Thickness.Right, _viewToEdit.Border.Thickness.Bottom);
					_viewToEdit.SetNeedsDisplay ();
					LayoutSubviews ();
				};

				var ckbTitle = new CheckBox ("Show Title") {
					BorderStyle = LineStyle.Double,
					X = Pos.Left (_borderEditor),
					Y = Pos.Bottom (_borderEditor) - 1,
					Width = Dim.Width (_borderEditor),
					Checked = true,
					SuperViewRendersLineCanvas = true
				};
				ckbTitle.Toggled += (sender, args) => {
					if (ckbTitle.Checked == true) {
						_viewToEdit.Title = _origTitle;
					} else {
						_viewToEdit.Title = string.Empty;
					}
				};
				Add (ckbTitle);

				_paddingEditor = new AdornmentEditor {
					X = Pos.Left (_borderEditor),
					Y = Pos.Bottom (rbBorderStyle),
					Title = "Padding",
					Thickness = _viewToEdit.Padding.Thickness,
					Color = new Attribute (_viewToEdit.Padding.ColorScheme.Normal),
					SuperViewRendersLineCanvas = true
				};
				_paddingEditor.ThicknessChanged += Editor_ThicknessChanged;
				_paddingEditor.AttributeChanged += Editor_AttributeChanged;
				Add (_paddingEditor);

				_diagCheckBox = new CheckBox {
					Text = "_Diagnostics",
					Y = Pos.Bottom (_paddingEditor)
				};
				_diagCheckBox.Toggled += (s, e) => {
					if (e.NewValue == true) {
						ConsoleDriver.Diagnostics = ConsoleDriver.DiagnosticFlags.FramePadding | ConsoleDriver.DiagnosticFlags.FrameRuler;
					} else {
						ConsoleDriver.Diagnostics = ConsoleDriver.DiagnosticFlags.Off;
					}
				};

				Add (_diagCheckBox);
				Add (_viewToEdit);

				_viewToEdit.LayoutComplete += (s, e) => {
					if (ckbTitle.Checked == true) {
						_viewToEdit.Title = _origTitle;
					} else {
						_viewToEdit.Title = string.Empty;
					}
				};
			}
		}

		void Editor_AttributeChanged (object sender, Attribute attr)
		{
			switch (sender.ToString ()) {
			case var s when s == _marginEditor.ToString ():
				_viewToEdit.Margin.ColorScheme = new ColorScheme (_viewToEdit.Margin.ColorScheme) { Normal = attr };
				break;
			case var s when s == _borderEditor.ToString ():
				_viewToEdit.Border.ColorScheme = new ColorScheme (_viewToEdit.Border.ColorScheme) { Normal = attr };
				break;
			case var s when s == _paddingEditor.ToString ():
				_viewToEdit.Padding.ColorScheme = new ColorScheme (_viewToEdit.Padding.ColorScheme) { Normal = attr };
				break;
			}
		}

		void Editor_ThicknessChanged (object sender, ThicknessEventArgs e)
		{
			try {
				switch (sender.ToString ()) {
				case var s when s == _marginEditor.ToString ():
					_viewToEdit.Margin.Thickness = e.Thickness;
					break;
				case var s when s == _borderEditor.ToString ():
					_viewToEdit.Border.Thickness = e.Thickness;
					break;
				case var s when s == _paddingEditor.ToString ():
					_viewToEdit.Padding.Thickness = e.Thickness;
					break;
				}
			} catch {
				switch (sender.ToString ()) {
				case var s when s == _marginEditor.ToString ():
					_viewToEdit.Margin.Thickness = e.PreviousThickness;
					break;
				case var s when s == _borderEditor.ToString ():
					_viewToEdit.Border.Thickness = e.PreviousThickness;
					break;
				case var s when s == _paddingEditor.ToString ():
					_viewToEdit.Padding.Thickness = e.PreviousThickness;
					break;
				}
			}
		}
	}
}