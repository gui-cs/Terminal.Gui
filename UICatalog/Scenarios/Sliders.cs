using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;



using S = Terminal.Gui.Sliders;

namespace UICatalog {
	[ScenarioMetadata (Name: "Slider", Description: "Demonstrates all sorts of Sliders")]
	//[ScenarioCategory ("Controls")]
	[ScenarioCategory ("")]

	class SlidersScenario : Scenario {
		public override void Setup ()
		{
			var leftView = new FrameView {
				Title = "Slider Types",
				X = 0,
				Y = 0,
				Width = Dim.Percent (50),
				Height = Dim.Fill (),
				ColorScheme = new ColorScheme {
					Normal = new Terminal.Gui.Attribute (Color.White, Color.Black),
					Focus = new Terminal.Gui.Attribute (Color.Black, Color.White),
				}
			};

			var rightView = new FrameView {
				Title = "Configuration",
				X = Pos.Right (leftView),
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};

			Win.Add (leftView);
			Win.Add (rightView);

			#region LeftView
			{
				MakeSliders (leftView, new List<object> { 500, 1000, 1500, 2000, 2500, 3000 });
				leftView.FocusFirst ();
			}
			#endregion

			#region RightView
			{
				#region Config Slider

				var slider = new Slider<string> () {
					X = Pos.Center (),
					Y = 0,
					Type = S.SliderType.Multiple,
					Width = Dim.Fill (),
					AllowEmpty = true
				};

				slider.Style.SetChar.Attribute = new Terminal.Gui.Attribute (Color.BrightGreen, Color.Black);
				slider.Style.LegendStyle.SetAttribute = new Terminal.Gui.Attribute (Color.Green, Color.Black);

				slider.ShowHeader = true;
				slider.Header = "Slider Config";
				slider.Options = new List<S.SliderOption<string>> {
					new S.SliderOption<string>{
						Legend="Legends"
					},
					new S.SliderOption<string>{
						Legend="Header"
					},
					new S.SliderOption<string>{
						Legend="RangeSingle"
					},
					new S.SliderOption<string>{
						Legend="Spacing"
					}
				};

				slider.SetOption (0);
				slider.SetOption (1);

				rightView.Add (slider);

				slider.OptionsChanged += (options) => {
					foreach (var s in leftView.Subviews [0].Subviews.OfType<Slider> ()) {
						if (options.ContainsKey (0))
							s.ShowLegends = true;
						else
							s.ShowLegends = false;

						if (options.ContainsKey (1))
							s.ShowHeader = true;
						else
							s.ShowHeader = false;

						if (options.ContainsKey (2))
							s.RangeAllowSingle = true;
						else
							s.RangeAllowSingle = false;

						if (options.ContainsKey (3))
							s.ShowSpacing = true;
						else
							s.ShowSpacing = false;
					}
				};

				#endregion

				#region InnerSpacing Input
				// var innerspacing_slider = new Slider<string> ("Innerspacing", new List<string> { "Auto", "0", "1", "2", "3", "4", "5" }) {
				// 	X = Pos.Center (),
				// 	Y = Pos.Bottom (slider) + 1
				// };

				// innerspacing_slider.SetOption (0);

				// rightView.Add (innerspacing_slider);

				// innerspacing_slider.OptionsChanged += (options) => {
				// 	foreach (var s in leftView.Subviews [0].Subviews.OfType<Slider> ()) {
				// 		if (options.ContainsKey (0)) { }
				// 		//s.la = S.SliderLayout.Auto;
				// 		else {
				// 			s.InnerSpacing = options.Keys.First () - 1;
				// 		}
				// 	}
				// };
				#endregion

				#region Slider Orientation Slider

				var slider_orientation_slider = new Slider<string> ("Slider Orientation", new List<string> { "Horizontal", "Vertical" }) {
					X = Pos.Center (),
					Y = Pos.Bottom (slider) + 1,
					Width = Dim.Fill (),
				};

				slider_orientation_slider.SetOption (0);

				rightView.Add (slider_orientation_slider);

				slider_orientation_slider.OptionsChanged += (options) => {

					View prev = null;
					foreach (var s in leftView.Subviews [0].Subviews.OfType<Slider> ()) {

						if (options.ContainsKey (0)) {
							s.SliderOrientation = S.Orientation.Horizontal;

							s.AdjustBestHeight ();
							s.Width = Dim.Fill ();

							s.Style.SpaceChar.Rune = '─';

							if (prev == null) {
								s.LayoutStyle = LayoutStyle.Absolute;
								s.Y = 0;
								s.LayoutStyle = LayoutStyle.Computed;
							} else {
								s.Y = Pos.Bottom (prev) + 1;
							}
							s.X = Pos.Center ();
							prev = s;

						} else if (options.ContainsKey (1)) {
							s.SliderOrientation = S.Orientation.Vertical;

							s.AdjustBestWidth ();
							s.Height = Dim.Fill ();

							s.Style.SpaceChar.Rune = '│';

							if (prev == null) {
								s.LayoutStyle = LayoutStyle.Absolute;
								s.X = 0;
								s.LayoutStyle = LayoutStyle.Computed;
							} else {
								s.X = Pos.Right (prev) + 2;
							}
							s.Y = Pos.Center ();
							prev = s;
						}
					}
				};

				#endregion

				#region Legends Orientation Slider

				var legends_orientation_slider = new Slider<string> ("Legends Orientation", new List<string> { "Horizontal", "Vertical" }) {
					X = Pos.Center (),
					Y = Pos.Bottom (slider_orientation_slider) + 1,
					Width = Dim.Fill (),
				};

				legends_orientation_slider.SetOption (0);

				rightView.Add (legends_orientation_slider);

				legends_orientation_slider.OptionsChanged += (options) => {
					foreach (var s in leftView.Subviews [0].Subviews.OfType<Slider> ()) {
						if (options.ContainsKey (0))
							s.LegendsOrientation = S.Orientation.Horizontal;
						else if (options.ContainsKey (1))
							s.LegendsOrientation = S.Orientation.Vertical;
					}
				};

				#endregion

				#region Color Slider

				var sliderColor = new Slider<(Color, Color)> () {
					X = Pos.Center (),
					Y = Pos.Bottom (legends_orientation_slider) + 1,
					Type = S.SliderType.Single,
					Width = Dim.Fill (),
					AllowEmpty = true
				};

				sliderColor.Style.SetChar.Attribute = new Terminal.Gui.Attribute (Color.BrightGreen, Color.Black);
				sliderColor.Style.LegendStyle.SetAttribute = new Terminal.Gui.Attribute (Color.Green, Color.Blue);

				sliderColor.ShowHeader = true;
				sliderColor.Header = "Slider Color";
				sliderColor.Options = new List<S.SliderOption<(Color, Color)>> {
					new S.SliderOption<(Color, Color)> {
						Data = (Color.Red,Color.BrightRed),
						Legend = "Red"
					},
					new S.SliderOption<(Color, Color)> {
						Data = (Color.Blue,Color.BrightBlue),
						Legend = "Blue"
					},
					new S.SliderOption<(Color, Color)> {
						Data = (Color.Green,Color.BrightGreen),
						Legend = "Green"
					},
					new S.SliderOption<(Color, Color)> {
						Data = (Color.Cyan,Color.BrightCyan),
						Legend = "Cyan"
					},
					new S.SliderOption<(Color, Color)> {
						Data = (Color.Brown,Color.BrightYellow),
						Legend = "Yellow"
					}
				};

				rightView.Add (sliderColor);

				sliderColor.OptionsChanged += (options) => {
					if (options.Count != 0) {
						var data = options.First ().Value.Data;

						foreach (var s in leftView.Subviews [0].Subviews.OfType<Slider> ()) {
							s.Style.SetChar.Attribute = new Terminal.Gui.Attribute (data.Item1, data.Item2);
							s.Style.LegendStyle.SetAttribute = new Terminal.Gui.Attribute (data.Item1, Color.Black);
							s.Style.RangeChar.Attribute = new Terminal.Gui.Attribute (data.Item2, Color.Black);

							// Here we can not call SetNeedDisplay(), because the OptionsChanged was triggered by Key Pressing, that internaly calls SetNeedDisplay.
						}
					} else {
						foreach (var s in leftView.Subviews [0].Subviews.OfType<Slider> ()) {
							s.Style.SetChar.Attribute = null;
							s.Style.LegendStyle.SetAttribute = null;
							s.Style.RangeChar.Attribute = null;
						}
					}
				};

				// Set option after Eventhandler def, so it updates the sliders color.
				// sliderColor.SetOption (2);

				#endregion
			}
			#endregion
		}

		public void MakeSliders (View v, List<object> options)
		{
			var types = Enum.GetValues (typeof (Terminal.Gui.Sliders.SliderType)).Cast<Terminal.Gui.Sliders.SliderType> ().ToList ();

			Slider prev = null;

			foreach (var type in types) {
				var view = new Slider (type.ToString (), options, S.Orientation.Horizontal) {
					X = Pos.Center (),
					//X = Pos.Right (view) + 1,
					Y = prev == null ? 0 : Pos.Bottom (prev) + 1,
					//Y = Pos.Center (),
					Type = type,
					LegendsOrientation = S.Orientation.Horizontal,
					Width = Dim.Fill (),
					AllowEmpty = true
				};
				v.Add (view);
				prev = view;
			};

			//view.AutoSize = true;
		}
	}
}
