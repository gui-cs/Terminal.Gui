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
		var rightView = new FrameView {
			Title = "Configuration",
			X = Pos.Percent (50),
			Y = 0,
			Width = Dim.Fill (),
			Height = Dim.Fill (),
			ColorScheme = Colors.Dialog
		};

		Win.Add (rightView);

		MakeSliders (Win, new List<object> { 500, 1000, 1500, 2000, 2500, 3000, 3500, 4000, 4500, 5000 });
		Win.FocusFirst ();

		#region RightView
		{
			#region Config Slider

			var slider = new Slider<string> () {
				Title = "Options",
				X = Pos.Center (),
				Y = 0,
				Type = SliderType.Multiple,
				Width = Dim.Fill (),
				AllowEmpty = true
			};

			slider.Style.SetChar.Attribute = new Terminal.Gui.Attribute (Color.BrightGreen, Color.Black);
			slider.Style.LegendAttributes.SetAttribute = new Terminal.Gui.Attribute (Color.Green, Color.Black);

			slider.Options = new List<SliderOption<string>> {
					new SliderOption<string>{
						Legend="Legends"
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
				foreach (var s in Win.Subviews.OfType<Slider> ()) {
					if (options.ContainsKey (0))
						s.ShowLegends = true;
					else
						s.ShowLegends = false;

					if (options.ContainsKey (1))
						s.RangeAllowSingle = true;
					else
						s.RangeAllowSingle = false;

					if (options.ContainsKey (2))
						s.ShowSpacing = true;
					else
						s.ShowSpacing = false;
				}
				Win.LayoutSubviews ();
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

			var slider_orientation_slider = new Slider<string> (new List<string> { "Horizontal", "Vertical" }) {
				Title = "Slider Orientation",
				X = 0,
				Y = Pos.Bottom (slider) + 1,
				Width = Dim.Fill (),
			};

			slider_orientation_slider.SetOption (0);

			rightView.Add (slider_orientation_slider);

			slider_orientation_slider.OptionsChanged += (options) => {

				View prev = null;
				foreach (var s in Win.Subviews.OfType<Slider> ()) {

					if (options.ContainsKey (0)) {
						s.SliderOrientation = Orientation.Horizontal;

						s.AdjustBestHeight ();
						s.Width = Dim.Percent (50);

						s.Style.SpaceChar = new Cell () { Runes = { new Rune ('─') } };

						if (prev == null) {
							s.LayoutStyle = LayoutStyle.Absolute;
							s.Y = 0;
							s.LayoutStyle = LayoutStyle.Computed;
						} else {
							s.Y = Pos.Bottom (prev) + 1;
						}
						s.X = 0;
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
						s.Y = 0;
						prev = s;
					}
				}
				Win.LayoutSubviews ();
			};

			#endregion

			#region Legends Orientation Slider

			var legends_orientation_slider = new Slider<string> (new List<string> { "Horizontal", "Vertical" }) {
				Title = "Legends Orientation",
				X = Pos.Center (),
				Y = Pos.Bottom (slider_orientation_slider) + 1,
				Width = Dim.Fill (),
			};

			legends_orientation_slider.SetOption (0);

			rightView.Add (legends_orientation_slider);

			legends_orientation_slider.OptionsChanged += (options) => {
				foreach (var s in Win.Subviews.OfType<Slider> ()) {
					if (options.ContainsKey (0))
						s.LegendsOrientation = Orientation.Horizontal;
					else if (options.ContainsKey (1))
						s.LegendsOrientation = Orientation.Vertical;
				}
			};

			#endregion

			#region Color Slider

			var sliderColor = new Slider<(Color, Color)> () {
				Title = "Color",
				X = Pos.Center (),
				Y = Pos.Bottom (legends_orientation_slider) + 1,
				Type = SliderType.Single,
				Width = Dim.Fill (),
				AllowEmpty = true
			};

			sliderColor.Style.SetChar.Attribute = new Terminal.Gui.Attribute (Color.BrightGreen, Color.Black);
			sliderColor.Style.LegendAttributes.SetAttribute = new Terminal.Gui.Attribute (Color.Green, Color.Blue);

			sliderColor.LegendsOrientation = Orientation.Vertical;
			var colorOptions = new List<SliderOption<(Color, Color)>> ();
			foreach (var colorIndex in Enum.GetValues<Color> ()) {
				var colorName = colorIndex.ToString ();
				colorOptions.Add (new SliderOption<(Color, Color)> {
					Data = (colorIndex, Win.GetNormalColor ().Background),
					Legend = colorName,
					LegendAbbr = (Rune)colorName [0],
				});
			}
			sliderColor.Options = colorOptions;

			rightView.Add (sliderColor);

			sliderColor.OptionsChanged += (options) => {
				if (options.Count != 0) {
					var data = options.First ().Value.Data;

					foreach (var s in Win.Subviews.OfType<Slider> ()) {
						s.Style.OptionChar.Attribute = new Attribute (data.Item1, data.Item2);
						s.Style.SetChar.Attribute = new Attribute (data.Item1, data.Item2);
						s.Style.LegendAttributes.SetAttribute = new Attribute (data.Item1, Win.GetNormalColor ().Background);
						s.Style.RangeChar.Attribute = new Attribute (data.Item1, Win.GetNormalColor ().Background);
						s.Style.SpaceChar.Attribute = new Attribute (data.Item1, Win.GetNormalColor ().Background);
						s.Style.LegendAttributes.NormalAttribute = new Attribute (data.Item1, Win.GetNormalColor ().Background);
						// Here we can not call SetNeedDisplay(), because the OptionsChanged was triggered by Key Pressing,
						// that internaly calls SetNeedDisplay.
					}
				} else {
					foreach (var s in Win.Subviews.OfType<Slider> ()) {
						s.Style.SetChar.Attribute = null;
						s.Style.LegendAttributes.SetAttribute = null;
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
			var view = new Slider (options, Orientation.Horizontal) {
				Title = type.ToString (),
				X = 0,
				//X = Pos.Right (view) + 1,
				Y = prev == null ? 0 : Pos.Bottom (prev),
				//Y = Pos.Center (),
				Width = Dim.Percent (50),
				Type = type,
				LegendsOrientation = Orientation.Horizontal,
				AllowEmpty = true,
			};
			v.Add (view);
			prev = view;
		};

		var singleOptions = new List<object> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39 };

		var single = new Slider (singleOptions, Orientation.Horizontal) {
			Title = "Actual slider",
			X = 0,
			//X = Pos.Right (view) + 1,
			Y = prev == null ? 0 : Pos.Bottom (prev),
			//Y = Pos.Center (),
			Type = SliderType.Single,
			ShowLegends = true,
			LegendsOrientation = Orientation.Horizontal,
			Width = Dim.Percent (50),
			AllowEmpty = false,
			//ShowSpacing = true
		};
		single.Style.OptionChar = new Cell () { Runes = { CM.Glyphs.HLineDbl } };
		single.Style.SetChar = new Cell () { Runes = { CM.Glyphs.ContinuousMeterSegment } };
		single.Style.EmptyChar = new Cell () { Runes = { CM.Glyphs.HLineDbl } };
		//single.Style.RangeChar = new Cell () { Runes = { CM.Glyphs.AppleBMP } }; // ░ ▒ ▓   // Medium shade not blinking on curses.
		//single.Style.StartRangeChar = new Cell () { Runes = { new Rune ('█') } };
		//single.Style.EndRangeChar = new Cell () { Runes = { new Rune ('█') } };
		single.Style.SpaceChar = new Cell () { Runes = { CM.Glyphs.HLineDbl } };
		single.Style.DragChar = new Cell () { Runes = { CM.Glyphs.ContinuousMeterSegment } };

		v.Add (single);
	}
}
