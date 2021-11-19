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
		/// Layout
		/// </summary>
		public enum SliderLayout {
			/// <summary>
			///	Auto calculates the values <see cref="SliderStyle.StartSpacing"/>, <see cref="SliderStyle.InnerSpacing"/> and <see cref="SliderStyle.EndSpacing"/>.
			/// </summary>
			Auto,
			/// <summary>
			///	Auto calculates the values <see cref="SliderStyle.StartSpacing"/>, <see cref="SliderStyle.InnerSpacing"/> and <see cref="SliderStyle.EndSpacing"/> to match the layout values.
			/// </summary>
			Layout,
			/// <summary>
			///	Use the values <see cref="SliderStyle.StartSpacing"/>, <see cref="SliderStyle.InnerSpacing"/> and <see cref="SliderStyle.EndSpacing"/>.
			/// </summary>
			Custom
		}

		/// <summary>
		/// Represents an option in the slider.
		/// </summary>
		/// <typeparam name="T">Data of the option.</typeparam>
		public class SliderOption<T> {
			/// <summary>
			/// Display legend of the option.
			/// </summary>
			public string Legend { get; set; }

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
			/// Event Raised when this option change with the current state.
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
		/// </summary>
		public class SliderChar {
			/// <summary>
			/// Rune
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
		/// Slider Style
		/// </summary>
		public class SliderStyle {
			public SliderHeaderStyle HeaderStyle { get; set; }
			public SliderLegendStyle LegendStyle { get; set; }

			public SliderLayout Layout { get; set; }
			public SliderChar EmptyChar { get; set; }
			public SliderChar OptionChar { get; set; }
			public SliderChar SetChar { get; set; }
			public SliderChar SpaceChar { get; set; }
			public SliderChar RangeChar { get; set; }
			public SliderChar StartRangeChar { get; set; }
			public SliderChar EndRangeChar { get; set; }

			public SliderStyle ()
			{
				HeaderStyle = new SliderHeaderStyle { };
				LegendStyle = new SliderLegendStyle { };
			}
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

		internal class SliderConfiguration {
			internal bool RangeAllowSingle;

			internal int MouseClickXOptionThreshold;

			internal int StartMargin;
			internal int EndMargin;
			internal int StartSpacing;
			internal int EndSpacing;
			internal int InnerSpacing;

			internal bool ShowSpacing;
			internal bool ShowBorders;
			internal bool ShowHeader;
			internal bool ShowLegends;

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
		public Slider (List<object> options, Orientation orientation = Orientation.Horizontal) : base (options, orientation) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Slider"/> class.
		/// </summary>
		/// <param name="header">Header text of the slider.</param>
		/// <param name="options">Initial slider options.</param>
		public Slider (ustring header, List<object> options, Orientation orientation = Orientation.Horizontal) : base (header, options, orientation) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Slider"/> class.
		/// </summary>
		/// <param name="options">Initial slider options.</param>
		public Slider (List<SliderOption<object>> options, Orientation orientation = Orientation.Horizontal) : base (options, orientation) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Slider"/> class.
		/// </summary>
		/// <param name="header">Header text of the slider.</param>
		/// <param name="options">Initial slider options.</param>
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

		#region Constructors & Init()

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
		public Slider (List<T> options, Orientation orientation = Orientation.Horizontal) : this (ustring.Empty, options, orientation) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Slider"/> class.
		/// </summary>
		/// <param name="header">Header text of the slider.</param>
		/// <param name="options">Initial slider options.</param>
		public Slider (ustring header, List<T> options, Orientation orientation = Orientation.Horizontal)
		{

			if (options == null) {
				Init (header, null, orientation);
			} else {
				Init (header, options.Select (e => new SliderOption<T> { Data = e, Legend = e.ToString () }).ToList (), orientation);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Slider"/> class.
		/// </summary>
		/// <param name="options">Initial slider options.</param>
		public Slider (List<SliderOption<T>> options, Orientation orientation = Orientation.Horizontal) : this (ustring.Empty, options, orientation) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Slider"/> class.
		/// </summary>
		/// <param name="header">Header text of the slider.</param>
		/// <param name="options">Initial slider options.</param>
		public Slider (ustring header, List<SliderOption<T>> options, Orientation orientation = Orientation.Horizontal)
		{
			Init (header, options, orientation);
		}

		void Init (ustring header, List<SliderOption<T>> options, Orientation orientation = Orientation.Horizontal)
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

			// When we lose focus of the View(Slider), if we are range selecting we finish.
			Leave += (FocusEventArgs e) => {
				if (settingRange == true) {
					settingRange = false;
				}
				//Application.MainLoop.RemoveTimeout (blink_token);
			};

			Adjust ();
			CalcBounds ();

			// Enter += (FocusEventArgs e) => {
			// 	blink_token = Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (300), (ee) => {
			// 		if (cursorPosition != null) {
			// 			Move (cursorPosition.Value.Item1, cursorPosition.Value.Item2);
			// 			if (blink) {
			// 				Driver.SetAttribute (new Attribute (Color.Red, Color.Blue));
			// 			} else {
			// 				Driver.SetAttribute (new Attribute (Color.Blue, Color.Red));
			// 			}
			// 			Driver.AddRune ('█');
			// 			blink = !blink;
			// 		}
			// 		return true;
			// 	});
			// };
		}

		#endregion

		#region Properties

		/// <summary>
		/// Slider InnerSpecing.
		/// Changing this value will set the layout to Custom.  <see cref="SliderLayout"></see>.
		/// </summary>
		public int InnerSpacing {
			get { return config.InnerSpacing; }
			set {
				config.InnerSpacing = value;
				style.Layout = SliderLayout.Custom;
				Adjust ();
				CalcBounds ();
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

				//Note(jmperricone): Custom logic to preserve options ???
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
				CalcBounds ();
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
				CalcBounds ();
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
				CalcBounds ();
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Slider styles. <see cref="SliderStyle"></see>
		/// </summary>
		public SliderStyle Style {
			get {
				// Note(jmperricone): Maybe SliderStyle should be a struct so we return a copy ???
				return style;
			}
			set {
				// Note(jmperricone): If the user change a style, he/she must call SetNeedsDisplay(). OK ???
				style = Style;
				CalcBounds ();
			}
		}

		/// <summary>
		/// Set the slider options.
		/// </summary>
		public List<SliderOption<T>> Options {
			get {
				// Note(jmperricone): Maybe SliderOption should be a struct so we return a copy ???
				//                    Events will be preserved ? Need a test.
				return options;
			}
			set {
				options = value;

				if (options == null || options.Count == 0)
					return;

				Adjust ();
				CalcBounds ();
			}
		}

		/// <summary>
		/// Allow range start and end be in the same option, as a single option.
		/// </summary>
		public SliderLayout AdjustLayout {
			get { return style.Layout; }
			set {
				style.Layout = value;
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
		/// Show <see cref="SliderStyle.LeftBorder"/> character and <see cref="SliderStyle.RightBorder"/> character at the beginning and at the end of the slider respectively.
		/// </summary>
		public bool ShowBorders {
			get { return config.ShowBorders; }
			set {
				config.ShowBorders = value;
				CalcBounds ();
			}
		}

		/// <summary>
		/// Show/Hide the slider Header.
		/// </summary>
		public bool ShowHeader {
			get { return config.ShowHeader; }
			set {
				config.ShowHeader = value;
				CalcBounds ();
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
				CalcBounds ();
			}
		}

		/// <summary>
		/// Set Option
		/// </summary>
		public bool SetOption (int optionIndex)
		{
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
		/// Get the current setted options indexes in the user set order.
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
			// Note(jmperricone):
			switch (config.SliderOrientation) {
			case Orientation.Horizontal:
				style.SpaceChar = new SliderChar ('─'); // '─'
				style.OptionChar = new SliderChar ('□'); // '┼' '●🗹□⏹'
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
			style.SetChar = new SliderChar ('■');
			style.RangeChar = new SliderChar ('▓'); // ░ ▒ ▓   // Medium shade not blinking on curses.
			style.StartRangeChar = new SliderChar ('█');
			style.EndRangeChar = new SliderChar ('█');

			// LeftBorder = '▕',
			// RightBorder = '▏',
			// First = '├',
			// Last = '┤',
			//style.SetAllAttributes (new Attribute (Color.White, Color.Red));
		}

		void Adjust ()
		{
			if (options.Count == 0) return;
			// Layout cases:
			// 1) Find "Best" values.
			// 1) Using Computed/Absolute Layout.
			// 3) Values set by the user.

			// TODO(jmperricone): Start and end spacing == 0 with legend inside or centered.

			switch (style.Layout) {
			case SliderLayout.Auto:
				// Case 1
				if (config.SliderOrientation == config.LegendsOrientation) {
					// Note(jmperricone): This are not the best values, but they are good enough.
					var max = options.Max (e => e.Legend.ToString ().Length);
					config.StartSpacing = max / 2;
					config.InnerSpacing = max + (max % 2 == 0 ? 1 : 0);
					config.EndSpacing = max / 2;
				} else {
					// Horizontal slider with vertical legends OR vertical slider with horizontal legends.
					config.StartSpacing = 1;
					config.InnerSpacing = 1;
					config.EndSpacing = 1;
				}
				break;
			case SliderLayout.Layout:
				// TODO:
				break;
			case SliderLayout.Custom:
				// We use the values set by the user.
				break;
			}
		}

		void CalcBounds ()
		{
			// Note(jmperricone): There may be better names for these functions, CalcSliderWidth() and CalcSliderHeight(), because they are confusing.

			//Here we swap the width and height calculations based on the Slider Orientation.
			if (config.SliderOrientation == Orientation.Horizontal) {
				Width = CalcSliderWidth ();
				Height = CalcSliderHeight ();
			} else {
				// Swap
				Width = CalcSliderHeight ();
				Height = CalcSliderWidth ();
			}
		}

		int CalcSliderWidth ()
		{
			if (options.Count == 0)
				return 0;

			var width = 0;
			width += config.ShowBorders ? 2 : 0;
			width += config.StartSpacing + config.EndSpacing;
			width += options.Count;
			width += (options.Count - 1) * config.InnerSpacing;

			// If header is bigger than the slider, add margin to the slider and return header's width.
			if (config.ShowHeader && header != null && header != ustring.Empty && header.Length > width) {
				var diff = header.Length - width;
				config.StartMargin = diff / 2;
				config.EndMargin = diff / 2 + diff % 2;
				return header.Length;
			}

			return width;
		}

		int CalcSliderHeight ()
		{
			var height = 1; // Always shows the slider.

			height += config.ShowHeader ? 1 : 0;

			// If vertical we leave a space between header and slider, and between slider and legends.
			if (config.SliderOrientation == Orientation.Vertical && config.ShowHeader) {
				height += config.ShowHeader ? 1 : 0;
				height += config.ShowLegends ? 1 : 0;
			}

			if (config.ShowLegends) {
				if (config.LegendsOrientation != config.SliderOrientation) {
					// If the legends Orientation is opposite to the Slider Orientation, we add the max legend length.
					height += options.OrderByDescending (s => s.Legend.Length).First ().Legend.Length;
				} else {
					// Otherwise just 1.
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

			var offset = config.ShowBorders ? 1 : 0;
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
				cx -= config.ShowBorders ? 1 : 0;
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
				// Raise unset event.
				if (currentOptions.Count == 1) {
					options [currentOptions [0]].OnUnSet ();
				}

				// Set the option.
				currentOptions.Clear ();
				currentOptions.Add (currentOption);

				// Raise set event.
				options [currentOption].OnSet ();

				// Raise slider changed event.
				OptionsChanged?.Invoke (currentOptions.ToDictionary (e => e, e => options [e]));

				break;
			case SliderType.Multiple:
				if (currentOptions.Contains (currentOption)) {
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
				Move (position.x, position.y);
				//cursorPosition = (position.x, position.y);
			}
		}

		/// <inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			Driver.SetCursorVisibility (CursorVisibility.Box);

			var normalScheme = ColorScheme?.Normal ?? Application.Current.ColorScheme.Normal;

			if (this.options == null && this.options.Count > 0) {
				return;
			}

			// Debug
			Driver.SetAttribute (new Attribute (Color.White, Color.Red));
			for (int y = 0; y < bounds.Height; y++) {
				for (int x = 0; x < bounds.Width; x++) {
					MoveAndAdd (x, y, 'X');
				}
			}

			// Draw Header
			if (config.ShowHeader) {
				DrawHeader ();

				// If vertical we leave a space between header and slider.
				if (config.SliderOrientation == Orientation.Vertical) {
					Driver.SetAttribute (normalScheme);
					for (int y = 0; y < bounds.Height; y++) {
						MoveAndAdd (1, y, ' ');
					}
				}
			}

			// Draw Slider
			DrawSlider ();

			// Draw Legends.
			if (config.ShowLegends) {

				// If vertical we leave a space between slider and legends.
				if (config.SliderOrientation == Orientation.Vertical) {
					Driver.SetAttribute (normalScheme);
					for (int y = 0; y < bounds.Height; y++) {
						MoveAndAdd (config.ShowHeader ? 3 : 2, y, ' ');
					}
				}

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

			switch (textAlignment) {
			case TextAlignment.Justified:
				return TextFormatter.Justify (text, width);
			case TextAlignment.Left:
				return text + s1 + s1 + s2;
			case TextAlignment.Centered:
				return s1 + s2 + text + s1;
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
				var i = 0;
				foreach (var c in text) {
					MoveAndAdd (0, i++, c);
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

			var isVertical = config.SliderOrientation == Orientation.Vertical;
			var isLegendsVertical = config.LegendsOrientation == Orientation.Vertical;
			var isReverse = config.SliderOrientation != config.LegendsOrientation;

			var x = 0;
			var y = 0;
			var offset = config.ShowHeader ? 1 : 0;

			if (config.SliderOrientation == Orientation.Vertical) {
				x = offset + 1;
				//y = config.StartMargin;
			} else {
				y = offset;
				//x = config.StartMargin;
			}

			var isSet = currentOptions.Count > 0;

			// Left Margin
			Driver.SetAttribute (normalScheme);
			for (int i = 0; i < this.config.StartMargin; i++) {
				MoveAndAdd (x, y, ' ');
				if (isVertical) y++; else x++;
			}

			// Left Border
			if (config.ShowBorders) {
				Driver.SetAttribute (normalScheme);
				MoveAndAdd (x, y, '@');
				if (isVertical) y++; else x++;
			}

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
					Driver.SetAttribute (isSet && currentOptions.Contains (i) ? style.SetChar.Attribute ?? normalScheme : drawRange ? style.RangeChar.Attribute ?? normalScheme : style.OptionChar.Attribute ?? normalScheme);

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
						Driver.SetAttribute (drawRange && isSet ? style.RangeChar.Attribute ?? normalScheme : style.SpaceChar.Attribute ?? normalScheme);
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

			// Right Border
			if (config.ShowBorders) {
				Driver.SetAttribute (new Attribute (Color.Red, Color.BrightYellow));
				MoveAndAdd (x, y, '@');
				if (isVertical) y++; else x++;
			}

			// Right Margin
			Driver.SetAttribute (normalScheme);
			for (int i = 0; i < this.config.EndMargin; i++) {
				MoveAndAdd (x, y, ' ');
				if (isVertical) y++; else x++;
			}
		}

		void DrawLegends ()
		{
			// Attributes
			var normalScheme = style.LegendStyle.NormalAttribute ?? ColorScheme?.Normal ?? Application.Current.ColorScheme.Normal;
			var setScheme = style.LegendStyle.SetAttribute ?? ColorScheme?.Focus ?? Application.Current.ColorScheme.Focus;
			var spaceScheme = style.LegendStyle.EmptyAttribute ?? normalScheme;

			var isVertical = config.SliderOrientation == Orientation.Vertical;
			var isLegendsVertical = config.LegendsOrientation == Orientation.Vertical;

			// Todo(jmperricone): Change name
			var isReverse = config.SliderOrientation != config.LegendsOrientation;

			// TODO: Is this ok ??? for range maybe ? Who programmed this ? ;)
			var isSet = currentOptions.Count > 0;

			var x = 0;
			var y = 0;

			var offset = config.ShowHeader ? 2 : 1;
			var max_legends_length = isLegendsVertical ? Bounds.Height - offset : Bounds.Width - offset;

			var verticalSeparator = 0;
			if (config.SliderOrientation == Orientation.Vertical) {
				verticalSeparator += config.ShowHeader ? 1 : 0;
				verticalSeparator += config.ShowLegends ? 1 : 0;

				max_legends_length -= verticalSeparator;
			}

			if (isVertical) {
				x = offset + verticalSeparator;
				y = 0;
			} else {
				y = offset;
				x = 0;
			}

			// Start Margin
			Driver.SetAttribute (normalScheme);
			if (isReverse) {
				for (int i = 0; i < this.config.StartMargin; i++) {
					for (int j = 0; j < max_legends_length; j++) {
						MoveAndAdd (x, y, ' ');
						if (isLegendsVertical) y++; else x++;
					}
					if (isLegendsVertical) {
						x++;
						y = offset;
					} else {
						y++;
						x = offset + verticalSeparator;
					}
				}
			} else {
				for (int i = 0; i < this.config.StartMargin; i++) {
					MoveAndAdd (x, y, ' ');
					if (isLegendsVertical) y++; else x++;
				}
			}

			// Start Spacing (only when is reverse)
			if (isReverse) {
				for (int i = 0; i < this.config.StartSpacing; i++) {
					for (int j = 0; j < max_legends_length; j++) {
						MoveAndAdd (x, y, ' ');
						if (isLegendsVertical) y++; else x++;
					}
					if (isLegendsVertical) {
						x++;
						y = offset;
					} else {
						y++;
						x = offset + verticalSeparator;
					}
				}
			}

			// Legends
			for (int idx = 0; idx < options.Count; idx++) {

				var text = (ustring)options [idx].Legend;

				if (isReverse) {
					text = AlignText (options [idx].Legend.ToString (), max_legends_length, TextAlignment.Left);
				} else {
					text = AlignText (options [idx].Legend.ToString (), config.InnerSpacing, TextAlignment.Centered);
				};

				// Check if the Option is Set.
				bool isOptionSet = false;

				switch (config.Type) {
				case SliderType.Single:
				case SliderType.Multiple:
					if (isSet && currentOptions.Contains (idx))
						isOptionSet = true;
					break;
				case SliderType.LeftRange:
					if (isSet && idx <= currentOptions [0])
						isOptionSet = true;
					break;
				case SliderType.RightRange:
					if (isSet && idx >= currentOptions [0])
						isOptionSet = true;
					break;
				case SliderType.Range:
					if (isSet && ((idx >= currentOptions [0] && idx <= currentOptions [1]) || (idx >= currentOptions [1] && idx <= currentOptions [0])))
						isOptionSet = true;
					break;
				}

				// Draw Text
				Driver.SetAttribute (isOptionSet ? setScheme : normalScheme);
				foreach (var c in text) {
					MoveAndAdd (x, y, c);
					if (isLegendsVertical) y++; else x++;
				}
				Driver.SetAttribute (normalScheme);

				if (isReverse) {
					if (isLegendsVertical) {
						x++;
						y = offset;
					} else {
						y++;
						x = offset + verticalSeparator;
					}
				}

				// Draw InnerSpacing if it is not the last option.
				if (idx != options.Count - 1) {
					if (isReverse) {
						for (int i = 0; i < this.config.InnerSpacing; i++) {
							for (int j = 0; j < max_legends_length; j++) {
								MoveAndAdd (x, y, ' ');
								if (isLegendsVertical) y++; else x++;
							}
							if (isLegendsVertical) {
								x++;
								y = offset;
							} else {
								y++;
								x = offset + verticalSeparator;
							}
						}
					} else {
						MoveAndAdd (x, y, ' ');
						if (isLegendsVertical) y++; else x++;
					}
				}

				// Reset cursor
				if (isReverse) {
					if (config.SliderOrientation == Orientation.Vertical) {
						x = offset + verticalSeparator;
					} else {
						y = offset;
					}
				}
			}

			// End Spacing ( only when is reverse )
			if (isReverse) {
				for (int i = 0; i < this.config.EndSpacing; i++) {
					for (int j = 0; j < max_legends_length; j++) {
						MoveAndAdd (x, y, ' ');
						if (isLegendsVertical) y++; else x++;
					}
					if (isLegendsVertical) {
						x++;
						y = offset;
					} else {
						y++;
						x = offset + verticalSeparator;
					}
				}
			}

			// End Margin
			if (isReverse) {
				for (int i = 0; i < this.config.EndMargin; i++) {
					for (int j = 0; j < max_legends_length; j++) {
						MoveAndAdd (x, y, ' ');
						if (isLegendsVertical) y++; else x++;
					}
					if (isLegendsVertical) {
						x++;
						y = offset;
					} else {
						y++;
						x = offset + verticalSeparator;
					}
				}
			} else {
				for (int i = 0; i < this.config.EndMargin; i++) {
					MoveAndAdd (x, y, ' ');
					if (isLegendsVertical) y++; else x++;
				}
			}
		}

		void DrawLegends2 ()
		{
			// Attributes
			var normalScheme = style.LegendStyle.NormalAttribute ?? ColorScheme?.Normal ?? Application.Current.ColorScheme.Normal;
			var setScheme = ColorScheme.Focus; //style.LegendStyle.SetAttribute ?? new Attribute (normalScheme.Background, normalScheme.Foreground);
			var spaceScheme = style.LegendStyle.EmptyAttribute ?? normalScheme;

			var x = 0;
			var y = config.ShowHeader ? 2 : 1;

			Move (x, y);

			if (config.SliderOrientation == Orientation.Horizontal && config.LegendsOrientation == Orientation.Vertical) {
				x += config.StartSpacing;
			}

			if (config.LegendsOrientation == Orientation.Horizontal) {

			} else if (config.LegendsOrientation == Orientation.Vertical) {

			}

			// TODO: CHECK
			// Move (style.StartMargin + (style.ShowBorders ? 1 : 0), 2);
			var isTextVertical = config.LegendsOrientation == Orientation.Vertical;

			// TODO: Is this ok ??? for range maybe ? Who programmed this ? ;)
			var isSet = currentOptions.Count > 0;

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

				ustring text = "";
				switch (config.SliderOrientation) {
				case Orientation.Horizontal:
					switch (config.LegendsOrientation) {
					case Orientation.Horizontal:
						text = AlignText (options [i].Legend.ToString (), config.InnerSpacing, TextAlignment.Centered);
						break;
					case Orientation.Vertical:
						y = config.ShowHeader ? 2 : 1;
						text = options [i].Legend.ToString ();
						break;
					}
					break;
				case Orientation.Vertical:
					switch (config.LegendsOrientation) {
					case Orientation.Horizontal:
						text = options [i].Legend.ToString ();
						break;
					case Orientation.Vertical:
						text = AlignText (options [i].Legend.ToString (), config.InnerSpacing, TextAlignment.Centered);
						break;
					}
					break;
				}

				// Text
				//var text = AlignText (options [i].Legend.ToString (), style.InnerSpacing, TextAlignment.Centered);
				var left_spaces_count = text.TakeWhile (e => e == ' ').Count ();
				var right_spaces_count = text.Reverse ().TakeWhile (e => e == ' ').Count ();
				text = text.TrimSpace ();

				// TODO(jmperricone): Improve the Orientation check.

				// Start Spacing
				Driver.SetAttribute (spaceScheme);
				for (int j = 0; j < left_spaces_count; j++) {
					if (isTextVertical) Move (x, y++);
					Driver.AddRune (' ');
				}

				// Legend
				Driver.SetAttribute (isOptionSet ? setScheme : normalScheme);
				foreach (var c in text) {
					if (isTextVertical) Move (x, y++);
					Driver.AddRune (c);
				}

				// End Spacing
				Driver.SetAttribute (spaceScheme);
				for (int j = 0; j < right_spaces_count; j++) {
					if (isTextVertical) Move (x, y++);
					Driver.AddRune (' ');
				}
				//// Separator
				if (i != options.Count - 1) {
					if (isTextVertical) Move (x, y++);
					Driver.AddRune (' ');
				}

				if (config.SliderOrientation == Orientation.Horizontal && config.LegendsOrientation == Orientation.Vertical) {
					x += config.InnerSpacing + 1;
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
					SetCurrentOption ();
				}
				break;
			default:
				return false;
			}

			SetNeedsDisplay ();
			return true;
		}

		#endregion
	}
}
