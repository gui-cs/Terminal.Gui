using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios {

	[ScenarioMetadata (Name: "Line Drawing", Description: "Demonstrates LineCanvas.")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Drawing")]
	public class LineDrawing : Scenario {

		public override void Setup ()
		{
			var canvas = new DrawingArea {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};

			var tools = new ToolsView () {
				Title = "Tools",
				X = Pos.Right (canvas) - 20,
				Y = 2
			};

			tools.ColorChanged += (c) => canvas.SetColor (c);
			tools.SetStyle += (b) => canvas.LineStyle = b;
			tools.AddLayer += () => canvas.AddLayer ();

			Win.Add (canvas);
			Win.Add (tools);

			Win.KeyPressed += (s,e) => { e.Handled = canvas.OnKeyPressed (e); };
		}

		class ToolsView : Window {
			public event Action<Color> ColorChanged;
			public event Action<LineStyle> SetStyle;
			public event Action AddLayer;

			private RadioGroup _stylePicker;
			private ColorPicker _colorPicker;
			private Button _addLayerBtn;

			public ToolsView ()
			{
				BorderStyle = LineStyle.Dotted;
				Border.Thickness = new Thickness (1, 2, 1, 1);
				Initialized += ToolsView_Initialized;
			}

			private void ToolsView_Initialized (object sender, EventArgs e)
			{
				LayoutSubviews ();
				Width = Math.Max (_colorPicker.Frame.Width, _stylePicker.Frame.Width) + GetFramesThickness ().Horizontal;
				Height = _colorPicker.Frame.Height + _stylePicker.Frame.Height + _addLayerBtn.Frame.Height + GetFramesThickness ().Vertical;
				SuperView.LayoutSubviews ();
			}

			public override void BeginInit ()
			{
				base.BeginInit ();

				_colorPicker = new ColorPicker () {
					X = 0,
					Y = 0,
					BoxHeight = 1,
					BoxWidth = 2
				};

				_colorPicker.ColorChanged += (s, a) => ColorChanged?.Invoke (a.Color);

				_stylePicker = new RadioGroup (Enum.GetNames (typeof (LineStyle)).ToArray ()) {
					X = 0,
					Y = Pos.Bottom (_colorPicker)
				};

				_stylePicker.SelectedItemChanged += (s, a) => {
					SetStyle?.Invoke ((LineStyle)a.SelectedItem);
				};

				_addLayerBtn = new Button () {
					Text = "New Layer",
					X = Pos.Center (),
					Y = Pos.Bottom (_stylePicker),
				};

				_addLayerBtn.Clicked += (s, a) => AddLayer?.Invoke ();
				Add (_colorPicker, _stylePicker, _addLayerBtn);
			}
		}

		class DrawingArea : View {
			List<LineCanvas> _layers = new List<LineCanvas> ();
			LineCanvas _currentLayer;
			Color _currentColor = new Color (Color.White);
			StraightLine _currentLine = null;

			public LineStyle LineStyle { get; set; }

			public DrawingArea ()
			{
				AddLayer ();
			}

			Stack<StraightLine> undoHistory = new ();

			public override bool OnKeyPressed (KeyEventArgs e)
			{
				if (e.Key == (Key.Z | Key.CtrlMask)) {
					var pop = _currentLayer.RemoveLastLine ();
					if(pop != null) {
						undoHistory.Push (pop);
						SetNeedsDisplay ();
						return true;
					}
				}

				if (e.Key == (Key.Y | Key.CtrlMask)) {
					if (undoHistory.Any()) {
						var pop = undoHistory.Pop ();
						_currentLayer.AddLine(pop);
						SetNeedsDisplay ();
						return true;
					}
				}

				return base.OnKeyPressed (e);
			}
			internal void AddLayer ()
			{
				_currentLayer = new LineCanvas ();
				_layers.Add (_currentLayer);
			}

			public override void OnDrawContentComplete (Rect contentArea)
			{
				base.OnDrawContentComplete (contentArea);
				foreach (var canvas in _layers) {

					foreach (var c in canvas.GetCellMap ()) {
						Driver.SetAttribute (c.Value.Attribute ?? ColorScheme.Normal);
						// TODO: #2616 - Support combining sequences that don't normalize
						this.AddRune (c.Key.X, c.Key.Y, c.Value.Runes [0]);
					}
				}
			}

			public override bool OnMouseEvent (MouseEvent mouseEvent)
			{
				if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed)) {
					if (_currentLine == null) {

						_currentLine = new StraightLine (
							new Point (mouseEvent.X - GetBoundsOffset ().X, mouseEvent.Y - GetBoundsOffset ().X),
							0, Orientation.Vertical, LineStyle, new Attribute (_currentColor, GetNormalColor ().Background));
						_currentLayer.AddLine (_currentLine);
					} else {
						var start = _currentLine.Start;
						var end = new Point (mouseEvent.X - GetBoundsOffset ().X, mouseEvent.Y - GetBoundsOffset ().Y);
						var orientation = Orientation.Vertical;
						var length = end.Y - start.Y;

						// if line is wider than it is tall switch to horizontal
						if (Math.Abs (start.X - end.X) > Math.Abs (start.Y - end.Y)) {
							orientation = Orientation.Horizontal;
							length = end.X - start.X;
						}

						if (length > 0) {
							length++;
						} else {
							length--;
						}
						_currentLine.Length = length;
						_currentLine.Orientation = orientation;
						_currentLayer.ClearCache ();
						SetNeedsDisplay ();
					}
				} else {
					if (_currentLine != null) {
						_currentLine = null;
						undoHistory.Clear ();
						SetNeedsDisplay ();
					}
				}

				return base.OnMouseEvent (mouseEvent);
			}

			internal void SetColor (Color c)
			{
				_currentColor = c;
			}
		}
	}
}
