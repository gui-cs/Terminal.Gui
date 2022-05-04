//
// Slider.cs: Slider control
//
// Authors:
//   José Miguel Perricone (jmperricone@hotmail.com)
//

using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui.Sliders;

namespace Terminal.Gui {

	namespace Sliders {

		/// <summary>
		/// Represents an option in the slider.
		/// </summary>
		/// <typeparam name="T">Datatype of the option.</typeparam>
		public class SliderOption<T> {
			/// <summary>
			/// Legend of the option.
			/// </summary>
			public ustring Legend { get; set; }

			/// <summary>
			/// Abbreviation of the Legend. When the slider is 1 char width.
			/// </summary>
			public Rune? LegendAbbr { get; set; }

			/// <summary>
			/// Custom data of the option.
			/// </summary>
			public T Data { get; set; }

			/// <summary>
			/// To Raise the <see cref="Set"/> event from the Slider.
			/// </summary>
			internal void OnSet ()
			{
				Set?.Invoke (this);
			}

			/// <summary>
			/// To Raise the <see cref="UnSet"/> event from the Slider.
			/// </summary>
			internal void OnUnSet ()
			{
				UnSet?.Invoke (this);
			}

			/// <summary>
			/// To Raise the <see cref="Changed"/> event from the Slider.
			/// </summary>
			internal void OnChanged (bool isSet)
			{
				Changed?.Invoke (this, isSet);
			}

			/// <summary>
			/// Event Raised when this option is set.
			/// </summary>
			public event Action<SliderOption<T>> Set;
			/// <summary>
			/// Event Raised when this option is unset.
			/// </summary>
			public event Action<SliderOption<T>> UnSet;
			/// <summary>
			/// Event Raised when this option change containing the CURRENT state.
			/// </summary>
			public event Action<SliderOption<T>, bool> Changed;
		}

		/// <summary>
		/// Slider Types
		/// </summary>
		public enum SliderType {
			/// <summary>
			/// ├─┼─┼─┼─┼─█─┼─┼─┼─┼─┼─┼─┤
			/// </summary>
			Single,
			/// <summary>
			/// ├─┼─█─┼─┼─█─┼─┼─┼─┼─█─┼─┤
			/// </summary>
			Multiple,
			/// <summary>
			/// ├▒▒▒▒▒▒▒▒▒█─┼─┼─┼─┼─┼─┼─┤
			/// </summary>
			LeftRange,
			/// <summary>
			/// ├─┼─┼─┼─┼─█▒▒▒▒▒▒▒▒▒▒▒▒▒┤
			/// </summary>
			RightRange,
			/// <summary>
			/// ├─┼─┼─┼─┼─█▒▒▒▒▒▒▒█─┼─┼─┤
			/// </summary>
			Range
		}

		// Note(jmperricone): I named this Orientation instead of SliderOrientation, because I use it for the Slider and for Legends.
		//                    Graph has also an Orientation class.
		/// <summary>
		/// Slider Orientation
		/// </summary>
		public enum Orientation {
			/// <summary>
			/// Horizontal
			/// </summary>
			Horizontal,
			/// <summary>
			/// Vertical
			/// </summary>
			Vertical
		}

		/// <summary>
		/// Slider Char
		/// Each distinct character of the slider is a SliderChar.
		/// </summary>
		public class SliderChar {
			/// <summary>
			/// Rune to show
			/// </summary>
			public Rune Rune { get; set; }

			/// <summary>
			/// Attribute
			/// </summary>
			public Attribute? Attribute { get; set; }

			/// <summary>
			/// Constructor
			/// </summary>
			public SliderChar (Rune rune)
			{
				Rune = rune;
			}

			/// <summary>
			/// Constructor
			/// </summary>
			public SliderChar (Rune rune, Attribute attr)
			{
				Rune = rune;
				Attribute = attr;
			}
		}

		/// <summary>
		/// Header Style
		/// </summary>
		public class SliderHeaderStyle {
			/// <summary>
			/// Attribute
			/// </summary>
			public Attribute? NormalAttribute { get; set; }
			/// <summary>
			/// Attribute
			/// </summary>
			public Attribute? FocusAttribute { get; set; }
		}

		/// <summary>
		/// Legend Style
		/// </summary>
		public class SliderLegendStyle {
			/// <summary>
			/// Attribute for when the respective Option is NOT Set.
			/// </summary>
			public Attribute? NormalAttribute { get; set; }
			/// <summary>
			/// Attribute for when the respective Option is Set.
			/// </summary>
			public Attribute? SetAttribute { get; set; }
			/// <summary>
			/// Attribute for the Legends Container.
			/// </summary>
			public Attribute? EmptyAttribute { get; set; }
		}

		/// <summary>
		/// Slider Style
		/// </summary>
		public class SliderStyle {
			/// <summary>
			/// Slider Style
			/// </summary>
			public SliderHeaderStyle HeaderStyle { get; set; }
			/// <summary>
			/// Slider Style
			/// </summary>
			public SliderLegendStyle LegendStyle { get; set; }
			/// <summary>
			/// Slider Style
			/// </summary>
			public SliderChar EmptyChar { get; set; }
			/// <summary>
			/// Slider Style
			/// </summary>
			public SliderChar OptionChar { get; set; }
			/// <summary>
			/// Slider Style
			/// </summary>
			public SliderChar SetChar { get; set; }
			/// <summary>
			/// Slider Style
			/// </summary>
			public SliderChar SpaceChar { get; set; }
			/// <summary>
			/// Slider Style
			/// </summary>
			public SliderChar RangeChar { get; set; }
			/// <summary>
			/// Slider Style
			/// </summary>
			public SliderChar StartRangeChar { get; set; }
			/// <summary>
			/// Slider Style
			/// </summary>
			public SliderChar EndRangeChar { get; set; }

			/// <summary>
			/// Slider Style
			/// </summary>
			public SliderStyle ()
			{
				HeaderStyle = new SliderHeaderStyle { };
				LegendStyle = new SliderLegendStyle { };
			}
		}

		/// <summary>
		/// All Slider Configuration are grouped in this class.
		/// </summary>
		internal class SliderConfiguration {
			internal bool RangeAllowSingle;
			internal bool AllowEmpty;

			internal int MouseClickXOptionThreshold;

			internal bool AutoSize;



			internal int StartMargin;
			internal int EndMargin;
			internal int StartSpacing;
			internal int EndSpacing;
			internal int InnerSpacing;

			internal bool ShowSpacing;
			internal bool ShowHeader;
			internal bool ShowLegends;
			internal bool ShowLegendsAbbr;

			internal SliderType Type = SliderType.Single;
			internal Orientation SliderOrientation = Orientation.Horizontal;
			internal Orientation LegendsOrientation = Orientation.Horizontal;
		}
	}

	/// <summary>
	/// Slider control.
	/// </summary>
	public class Slider : Slider<object> {

		/// <summary>
		/// Initializes a new instance of the <see cref="Slider"/> class.
		/// </summary>
		public Slider () : base () { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Slider"/> class.
		/// </summary>
		/// <param name="options">Initial slider options.</param>
		/// <param name="orientation">Initial slider options.</param>
		public Slider (List<object> options, Orientation orientation = Orientation.Horizontal) : base (options, orientation) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Slider"/> class.
		/// </summary>
		/// <param name="header">Header text of the slider.</param>
		/// <param name="options">Initial slider options.</param>
		/// <param name="orientation">Initial slider options.</param>
		public Slider (ustring header, List<object> options, Orientation orientation = Orientation.Horizontal) : base (header, options, orientation) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Slider"/> class.
		/// </summary>
		/// <param name="options">Initial slider options.</param>
		/// <param name="orientation">Initial slider options.</param>
		public Slider (List<SliderOption<object>> options, Orientation orientation = Orientation.Horizontal) : base (options, orientation) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Slider"/> class.
		/// </summary>
		/// <param name="header">Header text of the slider.</param>
		/// <param name="options">Initial slider options.</param>
		/// <param name="orientation">Initial slider options.</param>
		public Slider (ustring header, List<SliderOption<object>> options, Orientation orientation = Orientation.Horizontal) : base (header, options, orientation) { }
	}

	/// <summary>
	///
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class Slider<T> : View {

		ustring header;
		bool settingRange;

		SliderConfiguration config = new SliderConfiguration ();
		SliderStyle style = new SliderStyle ();

		// Options
		List<SliderOption<T>> options;
		List<int> currentOptions = new List<int> ();
		int currentOption = 0;


		#region CUSTOM CURSOR
		object blink_token;
		bool blink = false;
		(int, int)? cursorPosition;
		#endregion

		#region Events

		/// <summary>
		/// Event raised when the slider option/s changed.
		/// The dictionary contains: key = option index, value = T
		/// </summary>
		public event Action<Dictionary<int, SliderOption<T>>> OptionsChanged;

		/// <summary>
		/// Event raised When the option is hovered with the keys or the mouse.
		/// </summary>
		public event Action<int, SliderOption<T>> OptionFocused;

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="Slider"/> class.
		/// </summary>
		public Slider () : this (ustring.Empty, new List<T> ())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Slider"/> class.
		/// </summary>
		/// <param name="options">Initial slider options.</param>
		/// <param name="orientation">Initial slider orientation.</param>
		public Slider (List<T> options, Orientation orientation = Orientation.Horizontal) : this (ustring.Empty, options, orientation) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Slider"/> class.
		/// </summary>
		/// <param name="header">Header text of the slider.</param>
		/// <param name="options">Initial slider options.</param>
		/// <param name="orientation">Initial slider orientation.</param>
		public Slider (ustring header, List<T> options, Orientation orientation = Orientation.Horizontal)
		{

			if (options == null) {
				Initialize (header, null, orientation);
			} else {
				Initialize (header, options.Select (e => new SliderOption<T> { Data = e, Legend = e.ToString () }).ToList (), orientation);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Slider"/> class.
		/// </summary>
		/// <param name="options">Initial slider options.</param>
		/// <param name="orientation">Initial slider orientation.</param>
		public Slider (List<SliderOption<T>> options, Orientation orientation = Orientation.Horizontal) : this (ustring.Empty, options, orientation) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Slider"/> class.
		/// </summary>
		/// <param name="header">Header text of the slider.</param>
		/// <param name="options">Initial slider options.</param>
		/// <param name="orientation">Initial slider orientation.</param>
		public Slider (ustring header, List<SliderOption<T>> options, Orientation orientation = Orientation.Horizontal)
		{
			Initialize (header, options, orientation);
		}

		#endregion

		#region Initialize
		void Initialize (ustring header, List<SliderOption<T>> options, Orientation orientation = Orientation.Horizontal)
		{
			CanFocus = true;

			if (header != ustring.Empty) {
				this.header = header;
				config.ShowHeader = true;
			}

			this.options = options ?? new List<SliderOption<T>> ();

			config.SliderOrientation = orientation;

			config.ShowLegends = true;

			SetDefaultStyle ();

			// When we lose focus of the View(Slider), if we are range selecting we stop it.
			Leave += (FocusEventArgs e) => {
				if (settingRange == true) {
					settingRange = false;
				}
				Application.MainLoop.RemoveTimeout (blink_token);
			};

			AdjustBestHeight ();
			AdjustBestWidth ();

			// Custom cursor
			Driver.SetCursorVisibility (CursorVisibility.Invisible);

			// CUSTOM CURSOR
			Action f = () => {
				if (cursorPosition != null) {
					Move (cursorPosition.Value.Item1, cursorPosition.Value.Item2);
					if (blink) {
						Driver.SetAttribute (new Attribute (Color.Red, Color.Blue));
					} else {
						Driver.SetAttribute (new Attribute (Color.Blue, Color.Red));
					}
					Driver.AddRune (GetSetOptions ().Contains (currentOption) ? style.SetChar.Rune : style.OptionChar.Rune);
					blink = !blink;
				}
			};

			Enter += (FocusEventArgs e) => {
				f ();
				blink_token = Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (300), (ee) => {
					f ();
					return true;
				});
			};
		}

		#endregion

		#region Properties

		/// <summary>
		/// Allow no selection.
		/// </summary>
		public bool AllowEmpty {
			get => config.AllowEmpty;
			set {
				config.AllowEmpty = value;
			}
		}

		/// <summary>
		/// Autosize
		/// </summary>
		public override bool AutoSize {
			get => config.AutoSize;
			set {
				config.AutoSize = value;
				//SetNeedsLayout ();
				//SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Slider InnerSpecing.
		/// Changing this value will set the layout to Custom.
		/// </summary>
		public int InnerSpacing {

			get { return config.InnerSpacing; }
			set {
				config.InnerSpacing = value;
				Adjust ();
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Slider Type. <see cref="SliderType"></see>
		/// </summary>
		public SliderType Type {
			get { return config.Type; }
			set {
				config.Type = value;

				// Todo: Custom logic to preserve options.
				currentOptions.Clear ();

				SetNeedsDisplay ();
			}
		}

		// VUELA ??
		/// <summary>
		/// Slider Orientation. <see cref="Orientation"></see>
		/// </summary>
		public Orientation SliderOrientation {
			get { return config.SliderOrientation; }
			set {
				config.SliderOrientation = value;
				Adjust ();
				CalculateSliderDimensions ();
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Legends Orientation. <see cref="Orientation"></see>
		/// </summary>
		public Orientation LegendsOrientation {
			get { return config.LegendsOrientation; }
			set {
				config.LegendsOrientation = value;
				Adjust ();
				CalculateSliderDimensions ();
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Header Text Property.
		/// To show or hide the header use <see cref="ShowHeader"/>.
		/// </summary>
		public ustring Header {
			get { return header; }
			set {
				header = value;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Slider styles. <see cref="SliderStyle"></see>
		/// </summary>
		public SliderStyle Style {
			get {
				// Note(jmperricone): Maybe SliderStyle should be a struct so we return a copy ???
				// Or SetStyle() and ( GetStyle() || Style getter copy )
				return style;
			}
			set {
				// Note(jmperricone): If the user change a style, he/she must call SetNeedsDisplay(). OK ???
				style = Style;
			}
		}

		/// <summary>
		/// Set the slider options.
		/// </summary>
		public List<SliderOption<T>> Options {
			get {
				// Note(jmperricone): Maybe SliderOption should be a struct so we return a copy ???
				//                    Events will be preserved ? Need a test.
				// Or SetOptions() and ( GetOptions() || Options getter copy )
				return options;
			}
			set {
				options = value;

				if (options == null || options.Count == 0)
					return;

				Adjust ();
			}
		}

		/// <summary>
		/// Allow range start and end be in the same option, as a single option.
		/// </summary>
		public bool RangeAllowSingle {
			get { return config.RangeAllowSingle; }
			set {
				config.RangeAllowSingle = value;
			}
		}

		/// <summary>
		/// Show/Hide the slider Header.
		/// </summary>
		public bool ShowHeader {
			get { return config.ShowHeader; }
			set {
				config.ShowHeader = value;
				Adjust ();
			}
		}

		/// <summary>
		/// Show/Hide the slider Header.
		/// </summary>
		public bool ShowSpacing {
			get { return config.ShowSpacing; }
			set {
				config.ShowSpacing = value;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Show/Hide the options legends.
		/// </summary>
		public bool ShowLegends {
			get { return config.ShowLegends; }
			set {
				config.ShowLegends = value;
				Adjust ();
			}
		}

		/// <summary>
		/// Set Option
		/// </summary>
		public bool SetOption (int optionIndex)
		{
			// TODO: Handle range type.			
			// Note: Maybe return false only when optionIndex doesn't exist, otherwise true.

			if (!currentOptions.Contains (optionIndex) && optionIndex >= 0 && optionIndex < options.Count) {
				currentOption = optionIndex;
				SetCurrentOption ();
				return true;
			}
			return false;
		}

		/// <summary>
		/// UnSet Option
		/// </summary>
		public bool UnSetOption (int optionIndex)
		{
			// TODO: Handle range type.			
			// Note: Maybe return false only when optionIndex doesn't exist, otherwise true.

			if (currentOptions.Contains (optionIndex) && optionIndex >= 0 && optionIndex < options.Count) {
				currentOption = optionIndex;
				SetCurrentOption ();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Get the current set options indexes.
		/// </summary>
		public List<int> GetSetOptions ()
		{
			// Copy
			return currentOptions.OrderBy (e => e).ToList ();
		}

		/// <summary>
		/// Get the current set options indexes in the user set order.
		/// </summary>
		public List<int> GetSetOptionsUnOrdered ()
		{
			// Copy
			return currentOptions.ToList ();
		}

		#endregion

		#region Helpers
		void MoveAndAdd (int x, int y, Rune rune)
		{
			/*
			// This debug test fails.
			#if (DEBUG)
						if (Bounds.Contains (x, y) == false) {
							header = $"w={Bounds.Width}h={Bounds.Height} x={x}y={y} out of Bounds";
						}
			#endif
			*/
			Move (x, y);
			Driver.AddRune (rune);
		}

		void MoveAndAdd (int x, int y, ustring str)
		{
			Move (x, y);
			Driver.AddStr (str);
		}

		void SetDefaultStyle ()
		{
			switch (config.SliderOrientation) {
			case Orientation.Horizontal:
				style.SpaceChar = new SliderChar ('─'); // '─'
				style.OptionChar = new SliderChar ('●'); // '┼●🗹□⏹'
				break;
			case Orientation.Vertical:
				style.SpaceChar = new SliderChar ('│');
				style.OptionChar = new SliderChar ('□');
				break;
			}

			// TODO(jmperricone) Wide Vertical ???
			/*
			 │
			 │
			 ┼─ 40
			 │
			 │
			███ 30
			▒▒▒
			▒▒▒
			▒▒▒ 20
			▒▒▒
			▒▒▒
			███ 10
			 │
			 │
			─●─ 0
			*/

			config.LegendsOrientation = config.SliderOrientation;
			style.EmptyChar = new SliderChar (' ');
			style.SetChar = new SliderChar ('▓'); // ■
			style.RangeChar = new SliderChar ('░'); // ░ ▒ ▓   // Medium shade not blinking on curses.
			style.StartRangeChar = new SliderChar ('█');
			style.EndRangeChar = new SliderChar ('█');

			// LeftBorder = '▕',
			// RightBorder = '▏',
			// First = '├',
			// Last = '┤',
			// style.SetAllAttributes (new Attribute (Color.White, Color.Red));
		}

		Rect prev_bounds;
		Rect prev_redraw_bounds;
		private bool BoundsChanged (Rect redraw_bounds)
		{
			if (prev_bounds != Bounds) {
				prev_bounds = Bounds;
				// header = "Bounds Changed " + Bounds.Width;
				return true;
			}
			if (prev_redraw_bounds != redraw_bounds) {
				prev_redraw_bounds = redraw_bounds;
				// header = "Redraw Bounds Changed";
				return true;
			}
			// header = "Equal " + Bounds.Width;
			return false;
		}

		void CalculateBestSliderDimensions ()
		{
			if (options.Count == 0) return;

			if (config.SliderOrientation == config.LegendsOrientation) {
				var max = options.Max (e => e.Legend.ToString ().Length);
				config.StartSpacing = max / 2;
				config.InnerSpacing = max + (max % 2 == 0 ? 1 : 0);
				config.EndSpacing = max / 2 + (max % 2);
			} else {
				// H Slider with V Legends || V Slider with H Legends.
				config.StartSpacing = 1;
				config.InnerSpacing = 1;
				config.EndSpacing = 1;
			}
		}

		void CalculateSliderDimensions ()
		{
			int size;
			if (config.SliderOrientation == Orientation.Horizontal) {
				size = Bounds.Width;
			} else {
				size = Bounds.Height;
			}

			// bounds = Bounds;
			header = Bounds.Width.ToString () + "-" + Bounds.Height.ToString ();

			if (options.Count == 0) return;

			if (config.AutoSize) {
				// Best values and change width and height.
				// TODO.
			} else {
				// Fit Slider to the actual width and height.

				int max_legend;
				if (config.SliderOrientation == config.LegendsOrientation) {
					max_legend = options.Max (e => e.Legend.ToString ().Length);
				} else {
					max_legend = 1;
				}

				var min = (size - max_legend) / (options.Count - 1);

				ustring first;
				ustring last;

				if (max_legend >= min) {
					if (config.SliderOrientation == config.LegendsOrientation) {
						config.ShowLegendsAbbr = true;
					}
					first = "x";
					last = "x";
				} else {
					config.ShowLegendsAbbr = false;
					first = options.First ().Legend;
					last = options.Last ().Legend;
				}

				// --o--
				// Hello
				// Left = He
				// Right = lo
				var first_left = (first.Length - 1) / 2; // Chars count of the first option to the left.
				var last_right = (last.Length) / 2;      // Chars count of the last option to the right.

				if (config.SliderOrientation != config.LegendsOrientation) {
					first_left = 0;
					last_right = 0;
				}

				var width = size - first_left - last_right - 1;

				var b = width / (options.Count - 1);
				var c = width % (options.Count - 1);

				config.StartSpacing = (c / 2) + first_left;
				config.InnerSpacing = b - 1;
				config.EndSpacing = (c / 2) + (c % 2) + last_right;
			}
		}

		/// <summary>
		/// Adjust the height of the Slider to the best value.
		///</summary>
		public void AdjustBestHeight ()
		{
			// Hack???  Otherwise we can't go back to Dim.Absolute.
			LayoutStyle = LayoutStyle.Absolute;

			if (config.SliderOrientation == Orientation.Horizontal) {
				Height = CalcSliderHeight ();
			} else {
				Height = CalcSliderWidth ();
			}

			LayoutStyle = LayoutStyle.Computed;
		}

		/// <summary>
		/// Adjust the height of the Slider to the best value. (Only works if height is DimAbsolute).
		///</summary>
		public void AdjustBestWidth ()
		{
			LayoutStyle = LayoutStyle.Absolute;

			if (config.SliderOrientation == Orientation.Horizontal) {
				Width = CalcSliderWidth ();
			} else {
				Width = CalcSliderHeight ();
			}

			LayoutStyle = LayoutStyle.Computed;
		}

		void Adjust ()
		{
			if (Width is Dim.DimAbsolute) {
				AdjustBestWidth ();
			}

			if (Height is Dim.DimAbsolute) {
				AdjustBestHeight ();
			}
		}

		int CalcSliderWidth ()
		{
			if (options.Count == 0)
				return 0;

			var width = 0;
			width += config.StartSpacing + config.EndSpacing;
			width += options.Count;
			width += (options.Count - 1) * config.InnerSpacing;

			// If header is bigger than the slider, add margin to the slider and return header's width.
			// if (config.ShowHeader && header != null && header != ustring.Empty && header.Length > width) {
			// 	var diff = header.Length - width;
			// 	config.StartMargin = diff / 2;
			// 	config.EndMargin = diff / 2 + diff % 2;
			// 	return header.Length;
			// }

			return width;
		}

		int CalcSliderHeight ()
		{
			var height = 1; // Always show the slider.

			if (config.ShowHeader) {
				switch (config.SliderOrientation) {
				case Orientation.Horizontal: {
						height += 1;
						break;
					}
				case Orientation.Vertical: {
						height += 2;
						break;
					}
				default:
					throw new ArgumentOutOfRangeException (config.SliderOrientation.ToString ());
				}
			}

			if (config.ShowLegends) {
				// Space between the slider and the legends.
				if (config.SliderOrientation == Orientation.Vertical) {
					height += 1;
				}

				if (config.LegendsOrientation != config.SliderOrientation) {
					height += options.Max (s => s.Legend.Length);
				} else {
					height += 1;
				}
			}

			return height;
		}

		bool TryGetPositionByOption (int option, out (int x, int y) position)
		{
			position = (-1, -1);

			if (option < 0 || option >= options.Count ()) {
				return false;
			}

			var offset = 0;
			offset += config.StartMargin;
			offset += config.StartSpacing;
			offset += option * (config.InnerSpacing + 1);

			if (config.SliderOrientation == Orientation.Vertical) {
				position = (config.ShowHeader ? 2 : 0, offset);
			} else {
				position = (offset, config.ShowHeader ? 1 : 0);
			}

			return true;
		}

		bool TryGetOptionByPosition (int x, int y, int x_threshold, out int option_idx)
		{
			// Fix(jmperricone): Not working.
			option_idx = -1;

			if (y != (config.ShowHeader ? 1 : 0))
				return false;

			for (int xx = (x - x_threshold); xx < (x + x_threshold + 1); xx++) {
				var cx = xx;
				cx -= config.StartMargin;
				cx -= config.StartSpacing;

				var option = cx / (config.InnerSpacing + 1);
				var valid = cx % (config.InnerSpacing + 1) == 0;

				if (!valid || option < 0 || option > options.Count - 1) {
					continue;
				}

				option_idx = option;
				return true;
			}

			return false;
		}

		void SetCurrentOption ()
		{
			switch (config.Type) {
			case SliderType.Single:
			case SliderType.LeftRange:
			case SliderType.RightRange:

				if (currentOptions.Count == 1) {
					var prev = currentOptions [0];

					if (!config.AllowEmpty && prev == currentOption) {
						break;
					}

					currentOptions.Clear ();
					options [currentOption].OnUnSet ();

					if (currentOption != prev) {
						currentOptions.Add (currentOption);
						options [currentOption].OnSet ();
					}
				} else {
					currentOptions.Add (currentOption);
					options [currentOption].OnSet ();
				}

				// Raise slider changed event.
				OptionsChanged?.Invoke (currentOptions.ToDictionary (e => e, e => options [e]));

				break;
			case SliderType.Multiple:
				if (currentOptions.Contains (currentOption)) {
					if (!config.AllowEmpty && currentOptions.Count () == 1) {
						break;
					}
					currentOptions.Remove (currentOption);
					options [currentOption].OnUnSet ();
				} else {
					currentOptions.Add (currentOption);
					options [currentOption].OnSet ();
				}
				OptionsChanged?.Invoke (currentOptions.ToDictionary (e => e, e => options [e]));
				break;

			case SliderType.Range:

				// Start range setting
				if (settingRange == false) {

					currentOptions.Clear ();            // Clear the range
					currentOptions.Add (currentOption); // Set first option to current under the cursor

					if (config.RangeAllowSingle) {
						// Allows range to be like a single option, this mean that both range options(left and right) are
						// in the same option.
						currentOptions.Add (currentOption);
					} else {
						// If range dosen't allow single option, we select the next one, otherwise, the previous one.

						if ((currentOption + 1) < options.Count ()) { // next
							currentOptions.Add (currentOption + 1);
							currentOption = currentOption + 1; // set cursor to the right
						} else if ((currentOption - 1) >= 0) { // prev
							currentOptions.Add (currentOption - 1);
							currentOption = currentOption - 1; // set cursor to the left
						} else {
							// If it only has one option...what ?.... you better use a checkbox or set style.RangeAllowSingle = true.
						}
					}
					// Set Range mode
					settingRange = true;

				} else { // moving
					 // Check if range is not single and cursor is on the same option, then check if we are going left or right and skip one option, if can.

					if (config.RangeAllowSingle == false && currentOption == currentOptions [0]) {
						// is Single
						if (currentOption < currentOptions [1] && (currentOption - 1 >= 0)) { // going left
							currentOption = currentOption - 1;
						} else if (currentOption > currentOptions [1] && (currentOption + 1 < options.Count ())) { // going right
							currentOption = currentOption + 1;
						} else {
							// Reset to the previous currentOption becasue we cant move.
							currentOption = currentOptions [1];
						}
					}
					currentOptions [1] = currentOption;
				}

				// Raise per Option Set event.
				// Fix(jmperricone): Should raise only when range selecting ends.
				options [currentOptions [0]].OnSet ();
				options [currentOptions [1]].OnSet ();

				// Raise Slider Option Changed Event.
				OptionsChanged?.Invoke (currentOptions.ToDictionary (e => e, e => options [e]));

				break;
			default:
				throw new ArgumentOutOfRangeException (config.Type.ToString ());
			}
		}
		#endregion

		#region Cursor and Drawing

		/// <inheritdoc/>
		public override void PositionCursor ()
		{
			if (TryGetPositionByOption (currentOption, out (int x, int y) position)) {
				if (Bounds.Contains (position.x, position.y)) {
					Move (position.x, position.y);
					cursorPosition = (position.x, position.y);
				}
			}
		}

		/// <inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			// Note(jmperricone): If there is a way to know when the bounds change, this code should go there without the check.
			//       I tested LayoutComplete event, but it's always called. Maybe I'm doing something wrong.
			if (BoundsChanged (bounds)) {
				if (AutoSize == true) {
					// unreachable
				} else {
					CalculateSliderDimensions ();
				}
			}

			//Driver.SetCursorVisibility (CursorVisibility.Box);

			var normalScheme = ColorScheme?.Normal ?? Application.Current.ColorScheme.Normal;

			if (this.options == null && this.options.Count > 0) {
				return;
			}

			// Debug
#if (DEBUG)
			Driver.SetAttribute (new Attribute (Color.White, Color.Red));
			for (int y = 0; y < Bounds.Height; y++) {
				for (int x = 0; x < Bounds.Width; x++) {
					// MoveAndAdd (x, y, '·');
				}
			}
#endif

			// Draw Header
			if (config.ShowHeader) {
				DrawHeader ();
			}

			// Draw Slider
			DrawSlider ();

			// Draw Legends.
			if (config.ShowLegends) {
				DrawLegends ();
			}
		}

		ustring AlignText (ustring text, int width, TextAlignment textAlignment)
		{
			if (text == null) {
				return "";
			}

			if (text.Length > width) {
				text = ustring.Make (text.Take (width).ToArray ());
			}

			var w = width - text.Length;
			var s1 = new string (' ', w / 2);
			var s2 = new string (' ', w % 2);

			// Note: The formatter doesn't handle all of this ???
			switch (textAlignment) {
			case TextAlignment.Justified:

				return TextFormatter.Justify (text, width);
			case TextAlignment.Left:
				return text + s1 + s1 + s2;
			case TextAlignment.Centered:
				if (text.Length % 2 != 0) {
					return s1 + text + s1 + s2;
				} else {
					return s1 + s2 + text + s1;
				}
			case TextAlignment.Right:
				return s1 + s1 + s2 + text;
			default:
				return text;
			}
		}

		void DrawHeader ()
		{
			// Attribute
			if (HasFocus) {
				Driver.SetAttribute (style.HeaderStyle.FocusAttribute ?? ColorScheme?.Focus ?? Application.Current.ColorScheme.Focus);
			} else {
				Driver.SetAttribute (style.HeaderStyle.NormalAttribute ?? ColorScheme?.Normal ?? Application.Current.ColorScheme.Normal);
			}

			// Text
			var text = AlignText (header, config.SliderOrientation == Orientation.Horizontal ? Bounds.Width : Bounds.Height, TextAlignment.Centered);

			switch (config.SliderOrientation) {
			case Orientation.Horizontal:
				MoveAndAdd (0, 0, text);
				break;
			case Orientation.Vertical:
				var y = 0;
				foreach (var c in text) {
					MoveAndAdd (0, y++, c);
				}
				break;
			default:
				throw new ArgumentOutOfRangeException (config.SliderOrientation.ToString ());
			}
		}

		void DrawSlider ()
		{
			// Attributes
			var normalScheme = ColorScheme?.Normal ?? Application.Current.ColorScheme.Normal;
			//var normalScheme = style.LegendStyle.NormalAttribute ?? ColorScheme.Disabled;
			var setScheme = style.SetChar.Attribute ?? ColorScheme.HotNormal;//  ColorScheme?.Focus ?? Application.Current.ColorScheme.Focus;

			var isVertical = config.SliderOrientation == Orientation.Vertical;
			var isLegendsVertical = config.LegendsOrientation == Orientation.Vertical;
			var isReverse = config.SliderOrientation != config.LegendsOrientation;

			var x = 0;
			var y = 0;

			if (config.SliderOrientation == Orientation.Vertical) {
				x = config.ShowHeader ? 2 : 0;
				//y = config.StartMargin;
			} else {
				y = config.ShowHeader ? 1 : 0;
				//x = config.StartMargin;
			}

			var isSet = currentOptions.Count > 0;

			// Left Margin
			// Driver.SetAttribute (normalScheme);
			// for (int i = 0; i < this.config.StartMargin; i++) {
			// 	MoveAndAdd (x, y, ' ');
			// 	if (isVertical) y++; else x++;
			// }

			// Left Spacing
			if (config.ShowSpacing && config.StartSpacing > 0) {

				Driver.SetAttribute (isSet && config.Type == SliderType.LeftRange ? style.RangeChar.Attribute ?? normalScheme : style.SpaceChar.Attribute ?? normalScheme);
				var rune = isSet && config.Type == SliderType.LeftRange ? style.RangeChar.Rune : style.SpaceChar.Rune;

				for (int i = 0; i < this.config.StartSpacing; i++) {
					MoveAndAdd (x, y, rune);
					if (isVertical) y++; else x++;
				}
			} else {
				Driver.SetAttribute (style.EmptyChar.Attribute ?? normalScheme);
				// for (int i = 0; i < this.config.StartSpacing + ((this.config.StartSpacing + this.config.EndSpacing) % 2 == 0 ? 1 : 2); i++) {
				for (int i = 0; i < this.config.StartSpacing; i++) {
					MoveAndAdd (x, y, style.EmptyChar.Rune);
					if (isVertical) y++; else x++;
				}
			}

			// Slider
			if (options.Count > 0) {
				for (int i = 0; i < options.Count; i++) {

					var drawRange = false;

					if (isSet && config.Type == SliderType.LeftRange && i <= currentOptions [0]) {
						drawRange = i < currentOptions [0];
					} else if (isSet && config.Type == SliderType.RightRange && i >= currentOptions [0]) {
						drawRange = i >= currentOptions [0];
					} else if (isSet && config.Type == SliderType.Range && ((i >= currentOptions [0] && i <= currentOptions [1]) || (i >= currentOptions [1] && i <= currentOptions [0]))) {
						drawRange = (i >= currentOptions [0] && i < currentOptions [1]) || (i >= currentOptions [1] && i < currentOptions [0]);
					} else {
						// Is Not a Range.
					}

					// Draw Option
					Driver.SetAttribute (isSet && currentOptions.Contains (i) ? style.SetChar.Attribute ?? setScheme : drawRange ? style.RangeChar.Attribute ?? setScheme : style.OptionChar.Attribute ?? normalScheme);

					// Note(jmperricone): Maybe only for curses, windows inverts actual colors, while curses inverts bg with fg.
					if (Application.Driver is CursesDriver) {
						if (currentOption == i && HasFocus) {
							Driver.SetAttribute (ColorScheme.Focus);
						}
					}

					var rune = (isSet && currentOptions.Contains (i) ? style.SetChar.Rune : drawRange ? style.RangeChar.Rune : style.OptionChar.Rune);
					MoveAndAdd (x, y, rune);
					if (isVertical) y++; else x++;

					// Draw Spacing
					if (i < options.Count - 1) { // Skip if is the Last Spacing.
						Driver.SetAttribute (drawRange && isSet ? style.RangeChar.Attribute ?? setScheme : style.SpaceChar.Attribute ?? normalScheme);
						for (int s = 0; s < config.InnerSpacing; s++) {
							MoveAndAdd (x, y, drawRange && isSet ? style.RangeChar.Rune : style.SpaceChar.Rune);
							if (isVertical) y++; else x++;
						}
					}
				}
			}

			// Right Spacing
			if (config.ShowSpacing && config.EndSpacing > 0) {
				Driver.SetAttribute (isSet && config.Type == SliderType.RightRange ? style.RangeChar.Attribute ?? normalScheme : style.SpaceChar.Attribute ?? normalScheme);
				var rune = isSet && config.Type == SliderType.RightRange ? style.RangeChar.Rune : style.SpaceChar.Rune;
				for (int i = 0; i < this.config.EndSpacing; i++) {
					MoveAndAdd (x, y, rune);
					if (isVertical) y++; else x++;
				}
			} else {
				Driver.SetAttribute (style.EmptyChar.Attribute ?? normalScheme);
				for (int i = 0; i < this.config.EndSpacing; i++) {
					MoveAndAdd (x, y, style.EmptyChar.Rune);
					if (isVertical) y++; else x++;
				}
			}

			// Right Margin
			// Driver.SetAttribute (normalScheme);
			// for (int i = 0; i < this.config.EndMargin; i++) {
			// 	MoveAndAdd (x, y, ' ');
			// 	if (isVertical) y++; else x++;
			// }
		}

		void DrawLegends ()
		{
			// Attributes
			var normalScheme = style.LegendStyle.NormalAttribute ?? ColorScheme?.Normal ?? ColorScheme.Disabled;
			var setScheme = style.LegendStyle.SetAttribute ?? ColorScheme?.HotNormal ?? ColorScheme.Normal;
			var spaceScheme = normalScheme;// style.LegendStyle.EmptyAttribute ?? normalScheme;

			var isTextVertical = config.LegendsOrientation == Orientation.Vertical;
			var isSet = config.Type == SliderType.Range ? currentOptions.Count == 2 : currentOptions.Count > 0;

			var x = 0;
			var y = 0;

			Move (x, y);

			if (config.SliderOrientation == Orientation.Horizontal && config.LegendsOrientation == Orientation.Vertical) {
				x += config.StartSpacing;
			}
			if (config.SliderOrientation == Orientation.Vertical && config.LegendsOrientation == Orientation.Horizontal) {
				y += config.StartSpacing;
			}

			if (config.SliderOrientation == Orientation.Horizontal) {
				y += config.ShowHeader ? 2 : 1;
			} else { // Vertical
				x += config.ShowHeader ? 4 : 2;
			}

			for (int i = 0; i < options.Count; i++) {

				bool isOptionSet = false;

				// Check if the Option is Set.
				switch (config.Type) {
				case SliderType.Single:
				case SliderType.Multiple:
					if (isSet && currentOptions.Contains (i))
						isOptionSet = true;
					break;
				case SliderType.LeftRange:
					if (isSet && i <= currentOptions [0])
						isOptionSet = true;
					break;
				case SliderType.RightRange:
					if (isSet && i >= currentOptions [0])
						isOptionSet = true;
					break;
				case SliderType.Range:
					if (isSet && ((i >= currentOptions [0] && i <= currentOptions [1]) || (i >= currentOptions [1] && i <= currentOptions [0])))
						isOptionSet = true;
					break;
				}

				// Text || Abbrevaiation
				ustring text = config.ShowLegendsAbbr ? options [i].LegendAbbr?.ToString () ?? new Rune (options [i].Legend.First ()).ToString () : options [i].Legend.ToString ();

				switch (config.SliderOrientation) {
				case Orientation.Horizontal:
					switch (config.LegendsOrientation) {
					case Orientation.Horizontal:
						text = AlignText (text, config.InnerSpacing + 1, TextAlignment.Centered);
						break;
					case Orientation.Vertical:
						y = config.ShowHeader ? 2 : 1;
						break;
					}
					break;
				case Orientation.Vertical:
					switch (config.LegendsOrientation) {
					case Orientation.Horizontal:
						x = config.ShowHeader ? 4 : 2;
						break;
					case Orientation.Vertical:
						text = AlignText (text, config.InnerSpacing + 1, TextAlignment.Centered);
						break;
					}
					break;
				}

				// Text
				var legend_left_spaces_count = text.TakeWhile (e => e == ' ').Count ();
				var legend_right_spaces_count = text.Reverse ().TakeWhile (e => e == ' ').Count ();
				text = text.TrimSpace ();

				// TODO(jmperricone): Improve the Orientation check.

				// Calculate Start Spacing
				if (config.SliderOrientation == config.LegendsOrientation) {
					if (i == 0) {
						// The spacing for the slider use the StartSpacing but...
						// The spacing for the legends is the StartSpacing MINUS the total chars to the left of the first options.
						//    ●────●────●
						//  Hello Bye World
						//
						// chars_left is 2 for Hello => (5 - 1) / 2
						//
						// then the spacing is 2 for the slider but 0 for the legends.

						var chars_left = (text.Length - 1) / 2;
						legend_left_spaces_count = config.StartSpacing - chars_left;
					}

					// Option Left Spacing
					if (isTextVertical) y += legend_left_spaces_count;
					else x += legend_left_spaces_count;
					//Move (x, y);
				}

				// Legend
				Driver.SetAttribute (isOptionSet ? setScheme : normalScheme);
				foreach (var c in text) {
					MoveAndAdd (x, y, c);
					//Driver.AddRune (c);
					if (isTextVertical) y += 1;
					else x += 1;
				}

				// Calculate End Spacing
				if (i == options.Count () - 1) {
					// See Start Spacing explanation.
					var chars_right = text.Length / 2;
					legend_right_spaces_count = config.EndSpacing - chars_right;
				}

				// Option Right Spacing of Option
				Driver.SetAttribute (spaceScheme);
				if (isTextVertical) y += legend_right_spaces_count;
				else x += legend_right_spaces_count;
				//Move (x, y);

				if (config.SliderOrientation == Orientation.Horizontal && config.LegendsOrientation == Orientation.Vertical) {
					x += config.InnerSpacing + 1;
				} else if (config.SliderOrientation == Orientation.Vertical && config.LegendsOrientation == Orientation.Horizontal) {
					y += config.InnerSpacing + 1;
				}
			}
		}

		#endregion

		#region Keys and Mouse

		/// <inheritdoc/>
		public override bool MouseEvent (MouseEvent mouseEvent)
		{
			// Note(jmperricone): Maybe we click to focus the cursor, and on next click we set the option.
			//                    That will makes OptionFocused Event more relevant.
			// TODO(jmperricone): Make Range Type work with mouse.

			if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked)) {
				var success = TryGetOptionByPosition (mouseEvent.X, mouseEvent.Y, config.MouseClickXOptionThreshold, out var option);
				if (success) {
					currentOption = option;
					OptionFocused?.Invoke (currentOption, options [currentOption]);
					SetCurrentOption ();
					SetNeedsDisplay ();
					return true;
				}
			}
			return false;
		}

		/// <inheritdoc/>
		public override bool ProcessKey (KeyEvent keyEvent)
		{
			switch (keyEvent.Key) {
			case Key.CursorLeft | Key.CtrlMask:
				header = "SIP SIP";
				if (currentOptions.Contains (currentOption)) {
					var prev = currentOption > 0 ? currentOption - 1 : currentOption;
					if (!currentOptions.Contains (prev) || (config.Type == SliderType.Range && config.RangeAllowSingle)) {
						currentOptions.Remove (currentOption);
						currentOption = prev;
						// Note(jmperricone): We are setting the option here, do we send the OptionFocused Event too ?
						OptionFocused?.Invoke (currentOption, options [currentOption]);
						currentOptions.Add (currentOption);
						currentOptions.Sort (); // Range Type
						OptionsChanged?.Invoke (currentOptions.ToDictionary (e => e, e => options [e]));
					}
				}
				break;
			case Key.CursorRight | Key.CtrlMask:
				if (currentOptions.Contains (currentOption)) {
					var next = currentOption < options.Count - 1 ? currentOption + 1 : currentOption;
					if (!currentOptions.Contains (next) || (config.Type == SliderType.Range && config.RangeAllowSingle)) {
						currentOptions.Remove (currentOption);
						currentOption = next;
						// Note(jmperricone): We are setting the option here, do we send the OptionFocused Event too ?
						OptionFocused?.Invoke (currentOption, options [currentOption]);
						currentOptions.Add (currentOption);
						currentOptions.Sort (); // Range Type
						OptionsChanged?.Invoke (currentOptions.ToDictionary (e => e, e => options [e]));
					}
				}
				break;
			case Key.Home:
				currentOption = 0;
				OptionFocused?.Invoke (currentOption, options [currentOption]);

				//Console.WriteLine (LayoutStyle.ToString ());
				break;
			case Key.End:
				currentOption = options.Count - 1;
				OptionFocused?.Invoke (currentOption, options [currentOption]);
				break;
			case Key.CursorUp:
			case Key.CursorLeft:

				if (keyEvent.Key == Key.CursorUp && config.SliderOrientation == Orientation.Horizontal) return false;
				if (keyEvent.Key == Key.CursorLeft && config.SliderOrientation == Orientation.Vertical) return false;

				currentOption = currentOption > 0 ? currentOption - 1 : currentOption;
				OptionFocused?.Invoke (currentOption, options [currentOption]);

				if (settingRange == true) {
					SetCurrentOption ();
				}
				break;
			case Key.CursorDown:
			case Key.CursorRight:

				if (keyEvent.Key == Key.CursorDown && config.SliderOrientation == Orientation.Horizontal) return false;
				if (keyEvent.Key == Key.CursorRight && config.SliderOrientation == Orientation.Vertical) return false;

				currentOption = currentOption < options.Count - 1 ? currentOption + 1 : currentOption;
				OptionFocused?.Invoke (currentOption, options [currentOption]);

				if (settingRange == true) {
					SetCurrentOption ();
				}
				break;

			case Key.Enter:
				if (settingRange == true) {
					settingRange = false;
				} else {
					if (Width is Dim.DimAbsolute) {
						//		Console.WriteLine ("ABSOLUTE");
					}

					SetCurrentOption ();
				}
				break;
			default:
				return false;
			}

			ClearLayoutNeeded ();
			SetNeedsDisplay ();
			return true;
		}

		#endregion
	}
}
