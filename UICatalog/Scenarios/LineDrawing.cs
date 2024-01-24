using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using static Terminal.Gui.SpinnerStyle;

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

			Win.KeyDown += (s,e) => { e.Handled = canvas.OnKeyDown (e); };
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
				Width = Math.Max (_colorPicker.Frame.Width, _stylePicker.Frame.Width) + GetAdornmentsThickness ().Horizontal;
				Height = _colorPicker.Frame.Height + _stylePicker.Frame.Height + _addLayerBtn.Frame.Height + GetAdornmentsThickness ().Vertical;
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
				_stylePicker.SelectedItem = 1;

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

			//// BUGBUG: Why is this not handled by a key binding???
			public override bool OnKeyDown (Key e)
			{
				// BUGBUG: These should be implemented with key bindings
				if (e.KeyCode == (KeyCode.Z | KeyCode.CtrlMask)) {
					var pop = _currentLayer.RemoveLastLine ();
					if(pop != null) {
						undoHistory.Push (pop);
						SetNeedsDisplay ();
						return true;
					}
				}

				if (e.KeyCode == (KeyCode.Y | KeyCode.CtrlMask)) {
					if (undoHistory.Any()) {
						var pop = undoHistory.Pop ();
						_currentLayer.AddLine(pop);
						SetNeedsDisplay ();
						return true;
					}
				}
				return false;
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
						this.AddRune (c.Key.X, c.Key.Y, c.Value.Rune);
					}
				}
			}

			public override bool OnMouseEvent (MouseEvent mouseEvent)
			{
				if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed)) {
					if (_currentLine == null) {
						// Mouse pressed down
						_currentLine = new StraightLine (
							new Point (mouseEvent.X, mouseEvent.Y),
							0, Orientation.Vertical, LineStyle, new Attribute (_currentColor, GetNormalColor ().Background));
						
						_currentLayer.AddLine (_currentLine);
					} else {
						// Mouse dragged
						var start = _currentLine.Start;
						var end = new Point (mouseEvent.X, mouseEvent.Y);
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

					// Mouse released
					if (_currentLine != null) {

						if(_currentLine.Length == 0) {
							_currentLine.Length = 1;
						}

						if(_currentLine.Style == LineStyle.None) {

							// Treat none as eraser
							var idx = _layers.IndexOf (_currentLayer);
							_layers.Remove (_currentLayer);

							_currentLayer = new LineCanvas(
								_currentLayer.Lines.Exclude (_currentLine.Start, _currentLine.Length, _currentLine.Orientation)
								);

							_layers.Insert (idx, _currentLayer);
						}

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
