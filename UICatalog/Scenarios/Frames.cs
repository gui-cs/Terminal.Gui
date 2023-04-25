using NStack;
using System;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Frames Demo", Description: "Demonstrates Margin, Border, and Padding on Views.")]
	[ScenarioCategory ("Layout")]
	[ScenarioCategory ("Borders")]
	public class Frames : Scenario {

		public class ThicknessEditor : View {
			private Thickness thickness;
			private TextField topEdit;
			private TextField leftEdit;
			private TextField rightEdit;
			private TextField bottomEdit;
			private bool isUpdating;

			public Thickness Thickness {
				get => thickness;
				set {
					if (isUpdating) {
						return;
					}
					thickness = value;
					ThicknessChanged?.Invoke (this, new ThicknessEventArgs () { Thickness = Thickness });
					if (IsInitialized) {
						isUpdating = true;
						if (topEdit.Text != thickness.Top.ToString ()) {
							topEdit.Text = thickness.Top.ToString ();
						}
						if (leftEdit.Text != thickness.Left.ToString ()) {
							leftEdit.Text = thickness.Left.ToString ();
						}
						if (rightEdit.Text != thickness.Right.ToString ()) {
							rightEdit.Text = thickness.Right.ToString ();
						}
						if (bottomEdit.Text != thickness.Bottom.ToString ()) {
							bottomEdit.Text = thickness.Bottom.ToString ();
						}
						isUpdating = false;
					}
				}
			}

			public event EventHandler<ThicknessEventArgs> ThicknessChanged;

			public ThicknessEditor ()
			{
				Margin.Thickness = new Thickness (0);
				BorderStyle = LineStyle.Single;
			}

			public override void BeginInit ()
			{
				base.BeginInit ();

				topEdit = new TextField ("") {
					X = Pos.Center (),
					Y = 0,
					Width = 5
				};
				topEdit.TextChanging += Edit_TextChanging;
				topEdit.Text = $"{Thickness.Top}";

				Add (topEdit);

				leftEdit = new TextField ("") {
					X = 1,
					Y = Pos.Bottom (topEdit),
					Width = 5
				};
				leftEdit.TextChanging += Edit_TextChanging;
				leftEdit.Text = $"{Thickness.Left}";
				Add (leftEdit);

				rightEdit = new TextField ("") {
					X = Pos.Right (topEdit),
					Y = Pos.Bottom (topEdit),
					Width = 5
				};
				rightEdit.TextChanging += Edit_TextChanging;
				rightEdit.Text = $"{Thickness.Right}";
				Add (rightEdit);

				bottomEdit = new TextField ("") {
					X = Pos.Center (),
					Y = Pos.Bottom (leftEdit),
					Width = 5
				};
				bottomEdit.TextChanging += Edit_TextChanging;
				bottomEdit.Text = $"{Thickness.Bottom}";
				Add (bottomEdit);

				var copyTop = new Button ("Copy Top") {
					X = Pos.Center (),
					Y = Pos.AnchorEnd (1)
				};
				copyTop.Clicked += (s, e) => {
					Thickness = new Thickness (Thickness.Top);
					if (topEdit.Text.IsEmpty) {
						topEdit.Text = "0";
					}
					bottomEdit.Text = leftEdit.Text = rightEdit.Text = topEdit.Text;
				};
				Add (copyTop);

				LayoutSubviews ();
				Height = Margin.Thickness.Vertical + Border.Thickness.Vertical + Padding.Thickness.Vertical + 4;
				Width = 20;
			}

			private void Edit_TextChanging (object sender, TextChangingEventArgs e)
			{
				try {
					if (string.IsNullOrEmpty (e.NewText.ToString ())) {
						e.Cancel = true;
						((TextField)sender).Text = "0";
						return;
					}
					switch (sender.ToString ()) {
					case var s when s == topEdit.ToString ():
						Thickness = new Thickness (Thickness.Left,
							int.Parse (e.NewText.ToString ()), Thickness.Right,
							Thickness.Bottom);
						break;
					case var s when s == leftEdit.ToString ():
						Thickness = new Thickness (int.Parse (e.NewText.ToString ()),
							Thickness.Top, Thickness.Right,
							Thickness.Bottom);
						break;
					case var s when s == rightEdit.ToString ():
						Thickness = new Thickness (Thickness.Left,
							Thickness.Top, int.Parse (e.NewText.ToString ()),
							Thickness.Bottom);
						break;
					case var s when s == bottomEdit.ToString ():
						Thickness = new Thickness (Thickness.Left,
							Thickness.Top, Thickness.Right,
							int.Parse (e.NewText.ToString ()));
						break;
					}
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			}
		}

		public class FramesEditor : Window {
			private View viewToEdit;
			private ThicknessEditor marginEditor;
			private ThicknessEditor borderEditor;
			private ThicknessEditor paddingEditor;

			public FramesEditor (NStack.ustring title, View viewToEdit)
			{
				this.viewToEdit = viewToEdit;

				viewToEdit.Margin.ColorScheme = new ColorScheme () {
					Normal = new Terminal.Gui.Attribute (Colors.ColorSchemes ["Toplevel"].Normal),
					Disabled = new Terminal.Gui.Attribute (Colors.ColorSchemes ["Toplevel"].Disabled),
					Focus = new Terminal.Gui.Attribute (Colors.ColorSchemes ["Toplevel"].Focus),
					HotFocus = new Terminal.Gui.Attribute (Colors.ColorSchemes ["Toplevel"].HotFocus),
					HotNormal = new Terminal.Gui.Attribute (Colors.ColorSchemes ["Toplevel"].HotNormal)
				};
				marginEditor = new ThicknessEditor () {
					X = 20,
					Y = 0,
					Title = "Margin",
					Thickness = viewToEdit.Margin.Thickness,
				};
				marginEditor.ThicknessChanged += Editor_ThicknessChanged;
				Add (marginEditor);

				viewToEdit.Border.ColorScheme = new ColorScheme () {
					Normal = new Terminal.Gui.Attribute (Colors.ColorSchemes ["Base"].Normal),
					Disabled = new Terminal.Gui.Attribute (Colors.ColorSchemes ["Base"].Disabled),
					Focus = new Terminal.Gui.Attribute (Colors.ColorSchemes ["Base"].Focus),
					HotFocus = new Terminal.Gui.Attribute (Colors.ColorSchemes ["Base"].HotFocus),
					HotNormal = new Terminal.Gui.Attribute (Colors.ColorSchemes ["Base"].HotNormal)
				};
				borderEditor = new ThicknessEditor () {
					X = Pos.Right (marginEditor) - 1,
					Y = 0,
					Title = "Border",
					Thickness = viewToEdit.Border.Thickness,
				};
				borderEditor.ThicknessChanged += Editor_ThicknessChanged;
				Add (borderEditor);

				viewToEdit.Padding.ColorScheme = new ColorScheme () {
					Normal = new Terminal.Gui.Attribute (Colors.ColorSchemes ["Error"].Normal),
					Disabled = new Terminal.Gui.Attribute (Colors.ColorSchemes ["Error"].Disabled),
					Focus = new Terminal.Gui.Attribute (Colors.ColorSchemes ["Error"].Focus),
					HotFocus = new Terminal.Gui.Attribute (Colors.ColorSchemes ["Error"].HotFocus),
					HotNormal = new Terminal.Gui.Attribute (Colors.ColorSchemes ["Error"].HotNormal)
				};
				paddingEditor = new ThicknessEditor () {
					X = Pos.Right (borderEditor) - 1,
					Y = 0,
					Title = "Padding",
					Thickness = viewToEdit.Padding.Thickness,
				};
				paddingEditor.ThicknessChanged += Editor_ThicknessChanged;
				Add (paddingEditor);

				viewToEdit.Y = Pos.Center () + 4;

				Add (new Label ("BorderStyle:"));

				var colorEnum = Enum.GetValues (typeof (Color)).Cast<Color> ().ToList ();

				var borderStyleEnum = Enum.GetValues (typeof (LineStyle)).Cast<LineStyle> ().ToList ();
				var rbBorderStyle = new RadioGroup (borderStyleEnum.Select (
					e => NStack.ustring.Make (e.ToString ())).ToArray ()) {

					X = 2,
					Y = 1,
					SelectedItem = (int)viewToEdit.Border.BorderStyle
				};
				Add (rbBorderStyle);

				rbBorderStyle.SelectedItemChanged += (s, e) => {
					var prevBorderStyle = viewToEdit.BorderStyle;
					viewToEdit.Border.BorderStyle = (LineStyle)e.SelectedItem;
					if (viewToEdit.Border.BorderStyle == LineStyle.None) {
						viewToEdit.Border.Thickness = new Thickness (0);
					} else if (prevBorderStyle == LineStyle.None && viewToEdit.Border.BorderStyle != LineStyle.None) {
						viewToEdit.Border.Thickness = new Thickness (1);
					}
					borderEditor.Thickness = new Thickness (viewToEdit.Border.Thickness.Left, viewToEdit.Border.Thickness.Top,
						viewToEdit.Border.Thickness.Right, viewToEdit.Border.Thickness.Bottom);
					viewToEdit.SetNeedsDisplay ();
				};

				Add (new Label ("BorderBrush:") {
					X = Pos.Right (rbBorderStyle),
					Y = Pos.Bottom (borderEditor)
				});

				var rbBorderBrush = new RadioGroup (colorEnum.Select (
					e => NStack.ustring.Make (e.ToString ())).ToArray ()) {

					X = Pos.Right (rbBorderStyle),
					Y = Pos.Bottom (borderEditor) + 1,
					SelectedItem = (int)viewToEdit.Border.ColorScheme.Normal.Background
				};
				rbBorderBrush.SelectedItemChanged += (s, e) => {
					if (viewToEdit.Border != null) {
						viewToEdit.Border.ColorScheme.Normal = new Terminal.Gui.Attribute (viewToEdit.Padding.ColorScheme.Normal.Background, (Color)e.SelectedItem);
					}
				};
				Add (rbBorderBrush);

				Add (new Label ("PaddingBrush:") {
					X = Pos.AnchorEnd (20),
					Y = Pos.Bottom (borderEditor)
				});

				var rbPaddingBrush = new RadioGroup (colorEnum.Select (
					e => NStack.ustring.Make (e.ToString ())).ToArray ()) {

					X = Pos.AnchorEnd (18),
					Y = Pos.Bottom (borderEditor) + 1,
					SelectedItem = (int)viewToEdit.Padding.ColorScheme.Normal.Background
				};
				rbPaddingBrush.SelectedItemChanged += (s, e) => {
					if (viewToEdit.Padding != null) {
						viewToEdit.Padding.ColorScheme.Normal = new Terminal.Gui.Attribute (viewToEdit.Padding.ColorScheme.Normal.Foreground, (Color)e.SelectedItem);
						viewToEdit.Border.ColorScheme.Normal = new Terminal.Gui.Attribute ((Color)e.SelectedItem, viewToEdit.Border.ColorScheme.Normal.Background);
					}
				};
				Add (rbPaddingBrush);

				var ckbTitle = new CheckBox ("With title") {
					X = Pos.Right (paddingEditor) + 1,
					Y = 2,
					Checked = ustring.IsNullOrEmpty (viewToEdit.Title)
				};
				Add (ckbTitle);

				viewToEdit.X = Pos.Center ();
				viewToEdit.Y = Pos.Bottom (marginEditor);
				viewToEdit.Width = 60;
				viewToEdit.Height = 25;
				Add (viewToEdit);

				viewToEdit.LayoutComplete += (s, e) => {
					if (ckbTitle.Checked == true) {
						viewToEdit.Title = viewToEdit.ToString ();
					} else {
						viewToEdit.Title = string.Empty;
					}
				};

				LayoutSubviews ();

				Title = title;
			}

			private void Editor_ThicknessChanged (object sender, ThicknessEventArgs e)
			{
				try {
					switch (sender.ToString ()) {
					case var s when s == marginEditor.ToString ():
						viewToEdit.Margin.Thickness = e.Thickness;
						break;
					case var s when s == borderEditor.ToString ():
						viewToEdit.Border.Thickness = e.Thickness;
						break;
					case var s when s == paddingEditor.ToString ():
						viewToEdit.Padding.Thickness = e.Thickness;
						break;
					}
				} catch {
					switch (sender.ToString ()) {
					case var s when s == marginEditor.ToString ():
						viewToEdit.Margin.Thickness = e.PreviousThickness;
						break;
					case var s when s == borderEditor.ToString ():
						viewToEdit.Border.Thickness = e.PreviousThickness;
						break;
					case var s when s == paddingEditor.ToString ():
						viewToEdit.Padding.Thickness = e.PreviousThickness;
						break;
					}
				}
			}
		}

		public override void Init ()
		{
			Application.Init ();
			ConfigurationManager.Themes.Theme = Theme;
			ConfigurationManager.Apply ();
			Application.Top.ColorScheme = Colors.ColorSchemes [TopLevelColorScheme];

			var view = new Window ();
			var tf1 = new TextField ("1234567890") { Width = 10 };

			var button = new Button ("Press me!") {
				X = Pos.Center (),
				Y = Pos.Center (),
			};
			button.Clicked += (s, e) => MessageBox.Query (20, 7, "Hi", $"I'm a {view.GetType ().Name}?", "Yes", "No");
			var label = new Label ($"I'm a {view.GetType ().Name}") {
				X = Pos.Center (),
				Y = Pos.Center () - 1,
			};
			var tf2 = new TextField ("1234567890") {
				X = Pos.AnchorEnd (10),
				Y = Pos.AnchorEnd (1),
				Width = 10
			};
			var tv = new TextView () {
				Y = Pos.AnchorEnd (2),
				Width = 10,
				Height = Dim.Fill (),
				Text = "1234567890"
			};

			view.Margin.Thickness = new Thickness (3);
			view.Padding.Thickness = new Thickness (1);

			view.Add (tf1, button, label, tf2, tv);

			var editor = new FramesEditor (
				$"{Application.QuitKey} to Quit - Scenario: {GetName ()}",
				view);

			Application.Run (editor);
			Application.Shutdown ();
		}

		public override void Run ()
		{
		}
	}
}