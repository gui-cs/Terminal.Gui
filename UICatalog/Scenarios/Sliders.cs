using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace UICatalog.Scenarios;

[ScenarioMetadata (Name: "Sliders", Description: "Demonstrates the Slider view.")]
[ScenarioCategory ("Controls")]
public class Sliders : Scenario {
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
			MakeSliders (leftView, new List<object> { 500, 1000, 1500, 2000, 2500, 3000, 3500, 4000, 4500, 5000 });
			leftView.FocusFirst ();
		}
		#endregion

		#region RightView
		{
			#region Config Slider

			var slider = new Slider<string> () {
				X = Pos.Center (),
				Y = 0,
				Type = SliderType.Multiple,
				Width = Dim.Fill (),
				AllowEmpty = true
			};

			slider.Style.SetChar.Attribute = new Terminal.Gui.Attribute (Color.BrightGreen, Color.Black);
			slider.Style.LegendStyle.SetAttribute = new Terminal.Gui.Attribute (Color.Green, Color.Black);

			slider.ShowHeader = true;
			slider.Header = "Slider Config";
			slider.Options = new List<SliderOption<string>> {
					new SliderOption<string>{
						Legend="Legends"
					},
					new SliderOption<string>{
						Legend="Header"
					},
					new SliderOption<string>{
						Legend="RangeSingle"
					},
					new SliderOption<string>{
						Legend="Spacing"
					}
				};

			slider.SetOption (0);
			slider.SetOption (1);

			rightView.Add (slider);

			slider.OptionsChanged += (options) => {
				foreach (var s in leftView.Subviews.OfType<Slider> ()) {
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
			// 	foreach (var s in leftView.Subviews.OfType<Slider> () ()) {
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
				foreach (var s in leftView.Subviews.OfType<Slider> ()) {

					if (options.ContainsKey (0)) {
						s.SliderOrientation = Orientation.Horizontal;

						s.AdjustBestHeight ();
						s.Width = Dim.Fill ();

						s.Style.SpaceChar = new Cell () { Runes = { new Rune ('─') } };

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
						s.SliderOrientation = Orientation.Vertical;

						s.AdjustBestWidth ();
						s.Height = Dim.Fill ();

						s.Style.SpaceChar = new Cell () { Runes = { new Rune ('│') } };


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
				foreach (var s in leftView.Subviews.OfType<Slider> ()) {
					if (options.ContainsKey (0))
						s.LegendsOrientation = Orientation.Horizontal;
					else if (options.ContainsKey (1))
						s.LegendsOrientation = Orientation.Vertical;
				}
			};

			#endregion

			#region Color Slider

			var sliderColor = new Slider<(Color, Color)> () {
				X = Pos.Center (),
				Y = Pos.Bottom (legends_orientation_slider) + 1,
				Type = SliderType.Single,
				Width = Dim.Fill (),
				AllowEmpty = false
			};

			sliderColor.Style.SetChar.Attribute = new Terminal.Gui.Attribute (Color.BrightGreen, Color.Black);
			sliderColor.Style.LegendStyle.SetAttribute = new Terminal.Gui.Attribute (Color.Green, Color.Blue);

			sliderColor.ShowHeader = true;
			sliderColor.Header = "Slider Color";
			sliderColor.LegendsOrientation = Orientation.Vertical;
			var colorOptions = new List<SliderOption<(Color, Color)>> ();
			foreach (var colorIndex in Enum.GetValues<Color> ()) {
				var colorName = colorIndex.ToString ();
				colorOptions.Add (new SliderOption<(Color, Color)> {
					Data = (colorIndex, Color.Black),
					Legend = colorName,
					LegendAbbr = (Rune)colorName [0],
				});
			}
			sliderColor.Options = colorOptions;

			rightView.Add (sliderColor);

			sliderColor.OptionsChanged += (options) => {
				if (options.Count != 0) {
					var data = options.First ().Value.Data;

					foreach (var s in leftView.Subviews.OfType<Slider> ()) {
						s.Style.OptionChar.Attribute = new Attribute (data.Item1, data.Item2);
						s.Style.SetChar.Attribute = new Attribute (data.Item1, data.Item2);
						s.Style.LegendStyle.SetAttribute = new Attribute (data.Item1, Color.Black);
						s.Style.RangeChar.Attribute = new Attribute (data.Item1, Color.Black);
						s.Style.SpaceChar.Attribute = new Attribute (data.Item1, Color.Black);
						s.Style.HeaderStyle.NormalAttribute = new Attribute (data.Item1, Color.Black);
						s.Style.HeaderStyle.FocusAttribute = new Attribute (data.Item1, Color.Black);
						s.Style.LegendStyle.NormalAttribute = new Attribute (data.Item1, Color.Black);
						// Here we can not call SetNeedDisplay(), because the OptionsChanged was triggered by Key Pressing,
						// that internaly calls SetNeedDisplay.
					}
				} else {
					foreach (var s in leftView.Subviews.OfType<Slider> ()) {
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
		var types = Enum.GetValues (typeof (SliderType)).Cast<SliderType> ().ToList ();

		Slider prev = null;

		foreach (var type in types) {
			var view = new Slider (type.ToString (), options, Orientation.Horizontal) {
				X = Pos.Center (),
				//X = Pos.Right (view) + 1,
				Y = prev == null ? 0 : Pos.Bottom (prev) + 1,
				//Y = Pos.Center (),
				Type = type,
				LegendsOrientation = Orientation.Horizontal,
				Width = Dim.Fill (),
				AllowEmpty = true
			};
			v.Add (view);
			prev = view;
		};

		var singleOptions = new List<object> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39 };
		var style = new SliderStyle () {
			OptionChar = new Cell () { Runes = { CM.Glyphs.HLineDbl } },
			SetChar = new Cell () { Runes = { CM.Glyphs.Diamond } },
			EmptyChar = new Cell () { Runes = { CM.Glyphs.ContinuousMeterSegment } },
			RangeChar = new Cell () { Runes = { CM.Glyphs.AppleBMP } }, // ░ ▒ ▓   // Medium shade not blinking on curses.
			StartRangeChar = new Cell () { Runes = { new Rune ('█') } },
			EndRangeChar = new Cell () { Runes = { new Rune ('█') } },
			SpaceChar = new Cell () { Runes = { CM.Glyphs.UnChecked } },
		};
		var single = new Slider ("Actual slider", singleOptions, Orientation.Horizontal) {
			X = Pos.Center (),
			//X = Pos.Right (view) + 1,
			Y = prev == null ? 0 : Pos.Bottom (prev) + 1,
			//Y = Pos.Center (),
			Type = SliderType.Single,
			ShowLegends = true,
			LegendsOrientation = Orientation.Horizontal,
			Width = Dim.Fill (),
			AllowEmpty = false,
			//Style = style,
			//ShowSpacing = true
		};
		v.Add (single);
	}
}
