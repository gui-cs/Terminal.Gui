﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Sliders", "Demonstrates the Slider view.")]
[ScenarioCategory ("Controls")]
public class Sliders : Scenario {
	public override void Setup ()
	{
		MakeSliders (Win, new List<object> { 500, 1000, 1500, 2000, 2500, 3000, 3500, 4000, 4500, 5000 });
		var configView = new FrameView {
			Title = "Configuration",
			X = Pos.Percent (50),
			Y = 0,
			Width = Dim.Fill (),
			Height = Dim.Fill (),
			ColorScheme = Colors.Dialog
		};

		Win.Add (configView);

		#region Config Slider
		var slider = new Slider<string> {
			Title = "Options",
			X = 0,
			Y = 0,
			Type = SliderType.Multiple,
			Width = Dim.Fill (),
			Height = 4,
			AllowEmpty = true,
			BorderStyle = LineStyle.Single
		};

		slider.Style.SetChar.Attribute = new Attribute (Color.BrightGreen, Color.Black);
		slider.Style.LegendAttributes.SetAttribute = new Attribute (Color.Green, Color.Black);

		slider.Options = new List<SliderOption<string>> {
			new () {
				Legend = "Legends"
			},
			new () {
				Legend = "RangeAllowSingle"
			},
			new () {
				Legend = "EndSpacing"
			},
			new () {
				Legend = "AutoSize"
			}
		};

		configView.Add (slider);

		slider.OptionsChanged += (sender, e) => {
			foreach (var s in Win.Subviews.OfType<Slider> ()) {
				s.ShowLegends = e.Options.ContainsKey (0);
				s.RangeAllowSingle = e.Options.ContainsKey (1);
				s.ShowEndSpacing = e.Options.ContainsKey (2);
				s.AutoSize = e.Options.ContainsKey (3);
				if (!s.AutoSize) {
					if (s.Orientation == Orientation.Horizontal) {
						s.Width = Dim.Percent (50);
						var h = s.ShowLegends && s.LegendsOrientation == Orientation.Vertical ? s.Options.Max (o => o.Legend.Length) + 3 : 4;
						s.Height = h;
					} else {
						var w = s.ShowLegends ? s.Options.Max (o => o.Legend.Length) + 3 : 3;
						s.Width = w;
						s.Height = Dim.Fill ();
					}
				}
			}
			if (Win.IsInitialized) {
				Win.LayoutSubviews ();
			}
		};
		slider.SetOption (0); // Legends
		slider.SetOption (1); // RangeAllowSingle
		//slider.SetOption (3); // AutoSize

		#region Slider Orientation Slider
		var slider_orientation_slider = new Slider<string> (new List<string> { "Horizontal", "Vertical" }) {
			Title = "Slider Orientation",
			X = 0,
			Y = Pos.Bottom (slider) + 1,
			Width = Dim.Fill (),
			Height = 4,
			BorderStyle = LineStyle.Single
		};

		slider_orientation_slider.SetOption (0);

		configView.Add (slider_orientation_slider);

		slider_orientation_slider.OptionsChanged += (sender, e) => {
			View prev = null;
			foreach (var s in Win.Subviews.OfType<Slider> ()) {
				if (e.Options.ContainsKey (0)) {
					s.Orientation = Orientation.Horizontal;

					s.Style.SpaceChar = new Cell { Rune = CM.Glyphs.HLine };

					if (prev == null) {
						s.LayoutStyle = LayoutStyle.Absolute;
						s.Y = 0;
						s.LayoutStyle = LayoutStyle.Computed;
					} else {
						s.Y = Pos.Bottom (prev) + 1;
					}
					s.X = 0;
					prev = s;

				} else if (e.Options.ContainsKey (1)) {
					s.Orientation = Orientation.Vertical;

					s.Style.SpaceChar = new Cell { Rune = CM.Glyphs.VLine };

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

				if (s.Orientation == Orientation.Horizontal) {
					s.Width = Dim.Percent (50);
					var h = s.ShowLegends && s.LegendsOrientation == Orientation.Vertical ? s.Options.Max (o => o.Legend.Length) + 3 : 4;
					s.Height = h;
				} else {
					var w = s.ShowLegends ? s.Options.Max (o => o.Legend.Length) + 3 : 3;
					s.Width = w;
					s.Height = Dim.Fill ();
				}

			}
			Win.LayoutSubviews ();
		};
		#endregion Slider Orientation Slider

		#region Legends Orientation Slider
		var legends_orientation_slider = new Slider<string> (new List<string> { "Horizontal", "Vertical" }) {
			Title = "Legends Orientation",
			X = Pos.Center (),
			Y = Pos.Bottom (slider_orientation_slider) + 1,
			Width = Dim.Fill (),
			Height = 4,
			BorderStyle = LineStyle.Single
		};

		legends_orientation_slider.SetOption (0);

		configView.Add (legends_orientation_slider);

		legends_orientation_slider.OptionsChanged += (sender, e) => {
			foreach (var s in Win.Subviews.OfType<Slider> ()) {
				if (e.Options.ContainsKey (0)) {
					s.LegendsOrientation = Orientation.Horizontal;
				} else if (e.Options.ContainsKey (1)) {
					s.LegendsOrientation = Orientation.Vertical;
				}
				if (s.Orientation == Orientation.Horizontal) {
					s.Width = Dim.Percent (50);
					var h = s.ShowLegends && s.LegendsOrientation == Orientation.Vertical ? s.Options.Max (o => o.Legend.Length) + 3 : 4;
					s.Height = h;
				} else {
					var w = s.ShowLegends ? s.Options.Max (o => o.Legend.Length) + 3 : 3;
					s.Width = w;
					s.Height = Dim.Fill ();
				}
			}
			Win.LayoutSubviews ();
		};
		#endregion Legends Orientation Slider

		#region Color Slider
		foreach (var s in Win.Subviews.OfType<Slider> ()) {
			s.Style.OptionChar.Attribute = Win.GetNormalColor ();
			s.Style.SetChar.Attribute = Win.GetNormalColor ();
			s.Style.LegendAttributes.SetAttribute = Win.GetNormalColor ();
			s.Style.RangeChar.Attribute = Win.GetNormalColor ();
		}

		var sliderFGColor = new Slider<(Color, Color)> {
			Title = "FG Color",
			X = 0,
			Y = Pos.Bottom (legends_orientation_slider) + 1,
			Type = SliderType.Single,
			BorderStyle = LineStyle.Single,
			AllowEmpty = false,
			Orientation = Orientation.Vertical,
			LegendsOrientation = Orientation.Horizontal,
			AutoSize = true
		};

		sliderFGColor.Style.SetChar.Attribute = new Attribute (Color.BrightGreen, Color.Black);
		sliderFGColor.Style.LegendAttributes.SetAttribute = new Attribute (Color.Green, Color.Blue);

		var colorOptions = new List<SliderOption<(Color, Color)>> ();
		foreach (var colorIndex in Enum.GetValues<ColorName> ()) {
			var colorName = colorIndex.ToString ();
			colorOptions.Add (new SliderOption<(Color, Color)> {
				Data = (new Color (colorIndex), new Color (colorIndex)),
				Legend = colorName,
				LegendAbbr = (Rune)colorName [0]
			});
		}
		sliderFGColor.Options = colorOptions;

		configView.Add (sliderFGColor);

		sliderFGColor.OptionsChanged += (sender, e) => {
			if (e.Options.Count != 0) {
				var data = e.Options.First ().Value.Data;
				foreach (var s in Win.Subviews.OfType<Slider> ()) {
					s.ColorScheme = new ColorScheme (s.ColorScheme);
					s.ColorScheme.Normal = new Attribute (data.Item2, s.ColorScheme.Normal.Background);

					s.Style.OptionChar.Attribute = new Attribute (data.Item1, s.ColorScheme.Normal.Background);
					s.Style.SetChar.Attribute = new Attribute (data.Item1, s.Style.SetChar.Attribute?.Background ?? s.ColorScheme.Normal.Background);
					s.Style.LegendAttributes.SetAttribute = new Attribute (data.Item1, s.ColorScheme.Normal.Background);
					s.Style.RangeChar.Attribute = new Attribute (data.Item1, s.ColorScheme.Normal.Background);
					s.Style.SpaceChar.Attribute = new Attribute (data.Item1, s.ColorScheme.Normal.Background);
					s.Style.LegendAttributes.NormalAttribute = new Attribute (data.Item1, s.ColorScheme.Normal.Background);
				}
			}
		};

		var sliderBGColor = new Slider<(Color, Color)> {
			Title = "BG Color",
			X = Pos.Right (sliderFGColor),
			Y = Pos.Top (sliderFGColor),
			Type = SliderType.Single,
			BorderStyle = LineStyle.Single,
			AllowEmpty = false,
			Orientation = Orientation.Vertical,
			LegendsOrientation = Orientation.Horizontal,
			AutoSize = true
		};

		sliderBGColor.Style.SetChar.Attribute = new Attribute (Color.BrightGreen, Color.Black);
		sliderBGColor.Style.LegendAttributes.SetAttribute = new Attribute (Color.Green, Color.Blue);

		sliderBGColor.Options = colorOptions;

		configView.Add (sliderBGColor);

		sliderBGColor.OptionsChanged += (sender, e) => {
			if (e.Options.Count != 0) {
				var data = e.Options.First ().Value.Data;

				foreach (var s in Win.Subviews.OfType<Slider> ()) {
					s.ColorScheme = new ColorScheme (s.ColorScheme);
					s.ColorScheme.Normal = new Attribute (s.ColorScheme.Normal.Foreground, data.Item2);
				}
			}
		};
		#endregion Color Slider
		#endregion Config Slider

		Win.FocusFirst ();
		Application.Top.Initialized += (s, e) => Application.Top.LayoutSubviews ();
	}

	public void MakeSliders (View v, List<object> options)
	{
		var types = Enum.GetValues (typeof (SliderType)).Cast<SliderType> ().ToList ();
		Slider prev = null;

		foreach (var type in types) {
			var view = new Slider (options) {
				Title = type.ToString (),
				X = 0,
				Y = prev == null ? 0 : Pos.Bottom (prev),
				BorderStyle = LineStyle.Single,
				Type = type,
				AllowEmpty = true
			};
			v.Add (view);
			prev = view;
		}

		var singleOptions = new List<object> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39 };
		var single = new Slider (singleOptions) {
			Title = "Continuous",
			X = 0,
			Y = prev == null ? 0 : Pos.Bottom (prev),
			Type = SliderType.Single,
			BorderStyle = LineStyle.Single,
			AllowEmpty = false
		};

		single.LayoutStarted += (s, e) => {
			if (single.Orientation == Orientation.Horizontal) {
				single.Style.SpaceChar = new Cell { Rune = CM.Glyphs.HLine };
				single.Style.OptionChar = new Cell { Rune = CM.Glyphs.HLine };
			} else {
				single.Style.SpaceChar = new Cell { Rune = CM.Glyphs.VLine };
				single.Style.OptionChar = new Cell { Rune = CM.Glyphs.VLine };
			}
		};
		single.Style.SetChar = new Cell { Rune = CM.Glyphs.ContinuousMeterSegment };
		single.Style.DragChar = new Cell { Rune = CM.Glyphs.ContinuousMeterSegment };

		v.Add (single);

		single.OptionsChanged += (s, e) => {
			single.Title = $"Continuous {e.Options.FirstOrDefault ().Key}";
		};

		var oneOption = new List<object> { "The Only Option" };
		var one = new Slider (oneOption) {
			Title = "One Option",
			X = 0,
			Y = prev == null ? 0 : Pos.Bottom (single),
			Type = SliderType.Single,
			BorderStyle = LineStyle.Single,
			AllowEmpty = false
		};
		v.Add (one);

	}
}