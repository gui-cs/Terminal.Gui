using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace Terminal.Gui;

/// <summary>
/// Represents an option in the slider.
/// </summary>
/// <typeparam name="T">Datatype of the option.</typeparam>
public class SliderOption<T> {
	/// <summary>
	/// Legend of the option.
	/// </summary>
	public string Legend { get; set; }

	/// <summary>
	/// Abbreviation of the Legend. When the slider is 1 char width.
	/// </summary>
	public Rune LegendAbbr { get; set; }

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

/// <summary>
/// Legend Style
/// </summary>
public class SliderAttributes {
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
	/// Legend Style
	/// </summary>
	public SliderAttributes LegendAttributes { get; set; }
	/// <summary>
	/// The glyph and the attribute used for empty spaces on the slider.
	/// </summary>
	public Cell EmptyChar { get; set; }
	/// <summary>
	/// The glyph and the attribute used for each option (tick) on the slider.
	/// </summary>
	public Cell OptionChar { get; set; }
	/// <summary>
	/// The glyph and the attribute used for options (ticks) that are set on the slider.
	/// </summary>
	public Cell SetChar { get; set; }
	/// <summary>
	/// The glyph and the attribute to indicate mouse dragging.
	/// </summary>
	public Cell DragChar { get; set; }
	/// <summary>
	/// The glyph and the attribute used for spaces between options (ticks) on the slider.
	/// </summary>
	public Cell SpaceChar { get; set; }
	/// <summary>
	/// The glyph and the attribute used for filling in ranges on the slider.
	/// </summary>
	public Cell RangeChar { get; set; }
	/// <summary>
	/// The glyph and the attribute used for the start of ranges on the slider.
	/// </summary>
	public Cell StartRangeChar { get; set; }
	/// <summary>
	/// The glyph and the attribute used for the end of ranges on the slider.
	/// </summary>
	public Cell EndRangeChar { get; set; }

	/// <summary>
	/// Slider Style
	/// </summary>
	public SliderStyle ()
	{
		LegendAttributes = new SliderAttributes { };
	}
}

/// <summary>
/// All Slider Configuration are grouped in this class.
/// </summary>
internal class SliderConfiguration {
	internal bool _rangeAllowSingle;
	internal bool _allowEmpty;

	internal int _mouseClickXOptionThreshold;

	internal bool _autoSize;

	internal int _startSpacing;
	internal int _endSpacing;
	internal int _innerSpacing;

	internal bool _showSpacing;
	internal bool _showLegends;
	internal bool _showLegendsAbbr;

	internal SliderType _type = SliderType.Single;
	internal Orientation _sliderOrientation = Orientation.Horizontal;
	internal Orientation _legendsOrientation = Orientation.Horizontal;
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
}

/// <summary>
/// Slider control.
/// </summary>
/// <typeparam name="T"></typeparam>
public class Slider<T> : View {
	bool _settingRange;

	SliderConfiguration _config = new SliderConfiguration ();
	SliderStyle _style = new SliderStyle ();

	// Options
	List<SliderOption<T>> _options;
	List<int> _currentOptions = new List<int> ();
	int _currentOption = 0;


	#region CUSTOM CURSOR
	object _blink_token;
	bool _blink = false;
	(int, int)? _cursorPosition;
	#endregion

	#region Events

	// TODO: Refactor the events to match the standard Terminal.Gui v2 event pattern.
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
	public Slider () : this (new List<T> ())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Slider"/> class.
	/// </summary>
	/// <param name="options">Initial slider options.</param>
	/// <param name="orientation">Initial slider orientation.</param>
	public Slider (List<T> options, Orientation orientation = Orientation.Horizontal)
	{
		if (options == null) {
			SetInitialProperties (null, orientation);
		} else {
			SetInitialProperties (options.Select (e => {
				var legend = e.ToString ();
				return new SliderOption<T> {
					Data = e,
					Legend = legend,
					LegendAbbr = (Rune)(legend?.Length > 0 ? legend [0] : ' '),
				};
			}).ToList (), orientation);
		}
	}

	#endregion

	#region Initialize
	void SetInitialProperties (List<SliderOption<T>> options, Orientation orientation = Orientation.Horizontal)
	{
		CanFocus = true;

		this._options = options ?? new List<SliderOption<T>> ();

		_config._sliderOrientation = orientation;

		_config._showLegends = true;

		SetDefaultStyle ();

		// When we lose focus of the View(Slider), if we are range selecting we stop it.
		Leave += (object s, FocusEventArgs e) => {
			if (_settingRange == true) {
				_settingRange = false;
			}
			//Application.MainLoop.RemoveTimeout (_blink_token);
		};


		// Custom cursor
		Driver.SetCursorVisibility (CursorVisibility.Invisible);

		// CUSTOM CURSOR
		Action f = () => {
			if (_cursorPosition != null) {
				Move (_cursorPosition.Value.Item1, _cursorPosition.Value.Item2);
				if (_blink) {
					Driver.SetAttribute (new Attribute (Color.Red, Color.Blue));
					Driver.AddRune (GetSetOptions ().Contains (_currentOption) ? _style.SetChar.Runes [0] : _style.OptionChar.Runes [0]);
				} else {
					Driver.SetAttribute (new Attribute (Color.Blue, Color.Red));
					Driver.AddRune (GetSetOptions ().Contains (_currentOption) ? _style.OptionChar.Runes [0] : _style.SetChar.Runes [0]);
				}
				_blink = !_blink;
			}
		};

		Enter += (object s, FocusEventArgs e) => {
			//f ();
			//_blink_token = Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (1300), (ee) => {
			//	f ();
			//	return true;
			//});
		};

		LayoutComplete += (s, e) => {
			CalculateSliderDimensions ();
			AdjustBestHeight ();
			AdjustBestWidth ();
		};
	}

	#endregion

	#region Properties

	/// <summary>
	/// Allow no selection.
	/// </summary>
	public bool AllowEmpty {
		get => _config._allowEmpty;
		set => _config._allowEmpty = value;
	}

	/// <summary>
	/// AutoSize
	/// </summary>
	public override bool AutoSize {
		get => _config._autoSize;
		set => _config._autoSize = value;
	}

	/// <summary>
	/// Gets or sets the number of rows/columns between <see cref="Options"/>
	/// Changing this value will set the layout to Custom.
	/// </summary>
	public int InnerSpacing {

		get => _config._innerSpacing;
		set {
			_config._innerSpacing = value;
			CalculateSliderDimensions ();
			Adjust ();
			SetNeedsDisplay ();
			SuperView?.SetNeedsLayout ();
		}
	}

	/// <summary>
	/// Slider Type. <see cref="SliderType"></see>
	/// </summary>
	public SliderType Type {
		get => _config._type;
		set {
			_config._type = value;

			// Todo: Custom logic to preserve options.
			_currentOptions.Clear ();

			SetNeedsDisplay ();
		}
	}

	/// <summary>
	/// Slider Orientation. <see cref="Orientation"></see>
	/// </summary>
	public Orientation SliderOrientation {
		get => _config._sliderOrientation;
		set {
			_config._sliderOrientation = value;
			CalculateSliderDimensions ();
			Adjust ();
			SetNeedsDisplay ();
			SuperView?.SetNeedsLayout ();
		}
	}

	/// <summary>
	/// Legends Orientation. <see cref="Orientation"></see>
	/// </summary>
	public Orientation LegendsOrientation {
		get => _config._legendsOrientation;
		set {
			_config._legendsOrientation = value;
			CalculateSliderDimensions ();
			Adjust ();
			SetNeedsDisplay ();
			SuperView?.SetNeedsLayout ();
		}
	}

	/// <summary>
	/// Slider styles. <see cref="SliderStyle"></see>
	/// </summary>
	public SliderStyle Style {
		get {
			// Note(jmperricone): Maybe SliderStyle should be a struct so we return a copy ???
			// Or SetStyle() and ( GetStyle() || Style getter copy )
			return _style;
		}
		set {
			// Note(jmperricone): If the user change a style, he/she must call SetNeedsDisplay(). OK ???
			_style = value;
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
			return _options;
		}
		set {
			_options = value;

			if (_options == null || _options.Count == 0) {
				return;
			}

			CalculateSliderDimensions ();
			Adjust ();
			SetNeedsDisplay ();
			SuperView?.SetNeedsLayout ();
		}
	}

	/// <summary>
	/// Allow range start and end be in the same option, as a single option.
	/// </summary>
	public bool RangeAllowSingle {
		get => _config._rangeAllowSingle;
		set {
			_config._rangeAllowSingle = value;
		}
	}

	/// <summary>
	/// Show/Hide spacing before and after the first and last option.
	/// </summary>
	public bool ShowSpacing {
		get => _config._showSpacing;
		set {
			_config._showSpacing = value;
			SetNeedsDisplay ();
		}
	}

	/// <summary>
	/// Show/Hide the options legends.
	/// </summary>
	public bool ShowLegends {
		get => _config._showLegends;
		set {
			_config._showLegends = value;
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

		if (!_currentOptions.Contains (optionIndex) && optionIndex >= 0 && optionIndex < _options.Count) {
			_currentOption = optionIndex;
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

		if (_currentOptions.Contains (optionIndex) && optionIndex >= 0 && optionIndex < _options.Count) {
			_currentOption = optionIndex;
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
		return _currentOptions.OrderBy (e => e).ToList ();
	}

	/// <summary>
	/// Get the current set options indexes in the user set order.
	/// </summary>
	public List<int> GetSetOptionsUnOrdered ()
	{
		// Copy
		return _currentOptions.ToList ();
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

	void MoveAndAdd (int x, int y, string str)
	{
		Move (x, y);
		Driver.AddStr (str);
	}

	// TODO: Use CM.Glyphs
	// TODO: Make configurable via ConfigurationManager
	void SetDefaultStyle ()
	{
		switch (_config._sliderOrientation) {
		case Orientation.Horizontal:
			_style.SpaceChar = new Cell () { Runes = { new Rune ('─') } }; // '─'
			_style.OptionChar = new Cell () { Runes = { new Rune ('●') } }; // '┼●🗹□⏹'
			break;
		case Orientation.Vertical:
			_style.SpaceChar = new Cell () { Runes = { new Rune ('│') } };
			_style.OptionChar = new Cell () { Runes = { new Rune ('□') } };
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

		_config._legendsOrientation = _config._sliderOrientation;
		_style.EmptyChar = new Cell () { Runes = { new Rune (' ') } };
		_style.SetChar = new Cell () { Runes = { new Rune ('▓') } }; // ■
		_style.RangeChar = new Cell () { Runes = { new Rune ('░') } }; // ░ ▒ ▓   // Medium shade not blinking on curses.
		_style.StartRangeChar = new Cell () { Runes = { new Rune ('█') } };
		_style.EndRangeChar = new Cell () { Runes = { new Rune ('█') } };
		_style.DragChar = new Cell () { Runes = { CM.Glyphs.Diamond } };

		// TODO: Support left & right (top/bottom)
		// First = '├',
		// Last = '┤',
	}

	//void CalculateBestSliderDimensions ()
	//{
	//	if (_options.Count == 0) return;

	//	if (_config._sliderOrientation == _config._legendsOrientation) {
	//		var max = _options.Max (e => e.Legend.ToString ().Length);
	//		_config._startSpacing = max / 2;
	//		_config._innerSpacing = max + (max % 2 == 0 ? 1 : 0);
	//		_config._endSpacing = max / 2 + (max % 2);
	//	} else {
	//		// H Slider with V Legends || V Slider with H Legends.
	//		_config._startSpacing = 1;
	//		_config._innerSpacing = 1;
	//		_config._endSpacing = 1;
	//	}
	//}

	void CalculateSliderDimensions ()
	{
		if (!IsInitialized) {
			return;
		}

		int size;
		if (_config._sliderOrientation == Orientation.Horizontal) {
			size = Bounds.Width;
		} else {
			size = Bounds.Height;
		}

		if (_options.Count == 0) {
			return;
		}

		if (_config._autoSize) {
			// Best values and change width and height.
			// TODO.
		} else {
			// Fit Slider to the actual width and height.

			int max_legend;
			if (_config._sliderOrientation == _config._legendsOrientation) {
				max_legend = _options.Max (e => e.Legend.ToString ().Length);
			} else {
				max_legend = 1;
			}

			var min = (size - max_legend) / (_options.Count - 1);

			string first;
			string last;

			if (max_legend >= min) {
				if (_config._sliderOrientation == _config._legendsOrientation) {
					_config._showLegendsAbbr = true;
				}
				first = "x";
				last = "x";
			} else {
				_config._showLegendsAbbr = false;
				first = _options.First ().Legend;
				last = _options.Last ().Legend;
			}

			// --o--
			// Hello
			// Left = He
			// Right = lo
			var first_left = (first.Length - 1) / 2; // Chars count of the first option to the left.
			var last_right = (last.Length) / 2;      // Chars count of the last option to the right.

			if (_config._sliderOrientation != _config._legendsOrientation) {
				first_left = 0;
				last_right = 0;
			}

			var width = size - first_left - last_right - 1;

			_config._startSpacing = first_left;
			_config._innerSpacing = Math.Max (0, (int)Math.Floor ((double)width / (_options.Count - 1)) - 1);
			_config._endSpacing = last_right;
		}
	}

	/// <summary>
	/// Adjust the height of the Slider to the best value.
	///</summary>
	public void AdjustBestHeight ()
	{
		// Hack???  Otherwise we can't go back to Dim.Absolute.
		LayoutStyle = LayoutStyle.Absolute;

		if (_config._sliderOrientation == Orientation.Horizontal) {
			Bounds = new Rect (Bounds.Location, new Size (Bounds.Width, CalcSliderThickness ()));
		} else {
			Bounds = new Rect (Bounds.Location, new Size (Bounds.Width, CalcSliderLength ()));
		}

		LayoutStyle = LayoutStyle.Computed;
	}

	/// <summary>
	/// Adjust the height of the Slider to the best value. (Only works if height is DimAbsolute).
	///</summary>
	public void AdjustBestWidth ()
	{
		LayoutStyle = LayoutStyle.Absolute;

		if (_config._sliderOrientation == Orientation.Horizontal) {
			Bounds = new Rect (Bounds.Location, new Size (CalcSliderLength (), Bounds.Height));
		} else {
			Bounds = new Rect (Bounds.Location, new Size (CalcSliderThickness (), Bounds.Height));
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

	int CalcSliderLength ()
	{
		if (_options.Count == 0) {
			return 0;
		}

		var length = 0;
		length += _config._startSpacing + _config._endSpacing;
		length += _options.Count;
		length += (_options.Count - 1) * _config._innerSpacing;
		return length;
	}

	int CalcSliderThickness ()
	{
		var thickness = 1; // Always show the slider.

		if (_config._showLegends) {
			if (_config._legendsOrientation != _config._sliderOrientation && _options.Count > 0) {
				thickness += _options.Max (s => s.Legend.Length);
			} else {
				thickness += 1;
			}
		}

		return thickness;
	}

	bool TryGetPositionByOption (int option, out (int x, int y) position)
	{
		position = (-1, -1);

		if (option < 0 || option >= _options.Count ()) {
			return false;
		}

		var offset = 0;
		offset += _config._startSpacing;
		offset += option * (_config._innerSpacing + 1);

		if (_config._sliderOrientation == Orientation.Vertical) {
			position = (0, offset);
		} else {
			position = (offset, 0);
		}

		return true;
	}

	bool TryGetOptionByPosition (int x, int y, int threshold, out int option_idx)
	{
		// Fix(jmperricone): Not working.
		option_idx = -1;
		if (SliderOrientation == Orientation.Horizontal) {
			if (y != 0) {
				return false;
			}

			for (int xx = (x - threshold); xx < (x + threshold + 1); xx++) {
				var cx = xx;
				cx -= _config._startSpacing;

				var option = cx / (_config._innerSpacing + 1);
				var valid = cx % (_config._innerSpacing + 1) == 0;

				if (!valid || option < 0 || option > _options.Count - 1) {
					continue;
				}

				option_idx = option;
				return true;
			}

		} else {
			if (x != 0) {
				return false;
			}

			for (int yy = (y - threshold); yy < (y + threshold + 1); yy++) {
				var cy = yy;
				cy -= _config._startSpacing;

				var option = cy / (_config._innerSpacing + 1);
				var valid = cy % (_config._innerSpacing + 1) == 0;

				if (!valid || option < 0 || option > _options.Count - 1) {
					continue;
				}

				option_idx = option;
				return true;
			}
		}

		return false;
	}

	void SetCurrentOption ()
	{
		switch (_config._type) {
		case SliderType.Single:
		case SliderType.LeftRange:
		case SliderType.RightRange:

			if (_currentOptions.Count == 1) {
				var prev = _currentOptions [0];

				if (!_config._allowEmpty && prev == _currentOption) {
					break;
				}

				_currentOptions.Clear ();
				_options [_currentOption].OnUnSet ();

				if (_currentOption != prev) {
					_currentOptions.Add (_currentOption);
					_options [_currentOption].OnSet ();
				}
			} else {
				_currentOptions.Add (_currentOption);
				_options [_currentOption].OnSet ();
			}

			// Raise slider changed event.
			OptionsChanged?.Invoke (_currentOptions.ToDictionary (e => e, e => _options [e]));

			break;
		case SliderType.Multiple:
			if (_currentOptions.Contains (_currentOption)) {
				if (!_config._allowEmpty && _currentOptions.Count () == 1) {
					break;
				}
				_currentOptions.Remove (_currentOption);
				_options [_currentOption].OnUnSet ();
			} else {
				_currentOptions.Add (_currentOption);
				_options [_currentOption].OnSet ();
			}
			OptionsChanged?.Invoke (_currentOptions.ToDictionary (e => e, e => _options [e]));
			break;

		case SliderType.Range:

			// Start range setting
			if (_settingRange == false) {

				_currentOptions.Clear ();            // Clear the range
				_currentOptions.Add (_currentOption); // Set first option to current under the cursor

				if (_config._rangeAllowSingle) {
					// Allows range to be like a single option, this mean that both range options(left and right) are
					// in the same option.
					_currentOptions.Add (_currentOption);
				} else {
					// If range dosen't allow single option, we select the next one, otherwise, the previous one.

					if ((_currentOption + 1) < _options.Count ()) { // next
						_currentOptions.Add (_currentOption + 1);
						_currentOption = _currentOption + 1; // set cursor to the right
					} else if ((_currentOption - 1) >= 0) { // prev
						_currentOptions.Add (_currentOption - 1);
						_currentOption = _currentOption - 1; // set cursor to the left
					} else {
						// If it only has one option...what ?.... you better use a checkbox or set style.RangeAllowSingle = true.
					}
				}
				// Set Range mode
				_settingRange = true;

			} else { // moving
				 // Check if range is not single and cursor is on the same option, then check if we are going left or right and skip one option, if can.

				if (_config._rangeAllowSingle == false && _currentOption == _currentOptions [0]) {
					// is Single
					if (_currentOption < _currentOptions [1] && (_currentOption - 1 >= 0)) { // going left
						_currentOption = _currentOption - 1;
					} else if (_currentOption > _currentOptions [1] && (_currentOption + 1 < _options.Count ())) { // going right
						_currentOption = _currentOption + 1;
					} else {
						// Reset to the previous currentOption becasue we cant move.
						_currentOption = _currentOptions [1];
					}
				}
				_currentOptions [1] = _currentOption;
			}

			// Raise per Option Set event.
			// Fix(jmperricone): Should raise only when range selecting ends.
			_options [_currentOptions [0]].OnSet ();
			_options [_currentOptions [1]].OnSet ();

			// Raise Slider Option Changed Event.
			OptionsChanged?.Invoke (_currentOptions.ToDictionary (e => e, e => _options [e]));

			break;
		default:
			throw new ArgumentOutOfRangeException (_config._type.ToString ());
		}
	}
	#endregion

	#region Cursor and Drawing

	/// <inheritdoc/>
	public override void PositionCursor ()
	{
		base.PositionCursor ();
		//if (_moveRuneRenderLocation.HasValue) {
		//	var location = _moveRuneRenderLocation ??
		//			new Point (Bounds.Width / 2, Bounds.Height / 2);
		//	Move (location.X, location.Y);
		//	cursorPosition = (location.X, location.Y);
		//	return;
		//}  
		if (TryGetPositionByOption (_currentOption, out (int x, int y) position)) {
			if (Bounds.Contains (position.x, position.y)) {
				Move (position.x, position.y);
				_cursorPosition = (position.x, position.y);
			}
		}

		if (HasFocus) {
			Driver.SetCursorVisibility (CursorVisibility.Default);
			Move (_cursorPosition.Value.Item1, _cursorPosition.Value.Item2);
		} else {
			Driver.SetCursorVisibility (CursorVisibility.Invisible);
		}
	}

	/// <inheritdoc/>
	public override void OnDrawContent (Rect contentArea)
	{
		//// Note(jmperricone): If there is a way to know when the bounds change, this code should go there without the check.
		////       I tested LayoutComplete event, but it's always called. Maybe I'm doing something wrong.
		//if (BoundsChanged (contentArea)) {
		//	if (AutoSize == true) {
		//		// unreachable
		//	} else {
		//		CalculateSliderDimensions ();
		//	}
		//}

		//Driver.SetCursorVisibility (CursorVisibility.Box);

		var normalScheme = ColorScheme?.Normal ?? Application.Current.ColorScheme.Normal;

		if (this._options == null && this._options.Count > 0) {
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

		// Draw Slider
		DrawSlider ();

		// Draw Legends.
		if (_config._showLegends) {
			DrawLegends ();
		}

		if (_dragPosition.HasValue && _moveRenderPosition.HasValue) {
			AddRune (_moveRenderPosition.Value.X, _moveRenderPosition.Value.Y, _style.DragChar.Runes [0]);
		}
	}

	string AlignText (string text, int width, TextAlignment textAlignment)
	{
		if (text == null) {
			return "";
		}

		if (text.Length > width) {
			text = text [0..width];
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

	void DrawSlider ()
	{
		// TODO: be more surgical on clear
		Clear ();

		// Attributes
		var normalScheme = ColorScheme?.Normal ?? Application.Current.ColorScheme.Normal;
		//var normalScheme = style.LegendStyle.NormalAttribute ?? ColorScheme.Disabled;
		var setScheme = _style.SetChar.Attribute ?? ColorScheme.HotNormal;//  ColorScheme?.Focus ?? Application.Current.ColorScheme.Focus;

		var isVertical = _config._sliderOrientation == Orientation.Vertical;
		var isLegendsVertical = _config._legendsOrientation == Orientation.Vertical;
		var isReverse = _config._sliderOrientation != _config._legendsOrientation;

		var x = 0;
		var y = 0;

		var isSet = _currentOptions.Count > 0;

		// Left Spacing
		if (_config._showSpacing && _config._startSpacing > 0) {

			Driver.SetAttribute (isSet && _config._type == SliderType.LeftRange ? _style.RangeChar.Attribute ?? normalScheme : _style.SpaceChar.Attribute ?? normalScheme);
			var rune = isSet && _config._type == SliderType.LeftRange ? _style.RangeChar.Runes [0] : _style.SpaceChar.Runes [0];

			for (int i = 0; i < this._config._startSpacing; i++) {
				MoveAndAdd (x, y, rune);
				if (isVertical) y++; else x++;
			}
		} else {
			Driver.SetAttribute (_style.EmptyChar.Attribute ?? normalScheme);
			// for (int i = 0; i < this.config.StartSpacing + ((this.config.StartSpacing + this.config.EndSpacing) % 2 == 0 ? 1 : 2); i++) {
			for (int i = 0; i < this._config._startSpacing; i++) {
				MoveAndAdd (x, y, _style.EmptyChar.Runes [0]);
				if (isVertical) y++; else x++;
			}
		}

		// Slider
		if (_options.Count > 0) {
			for (int i = 0; i < _options.Count; i++) {

				var drawRange = false;

				if (isSet && _config._type == SliderType.LeftRange && i <= _currentOptions [0]) {
					drawRange = i < _currentOptions [0];
				} else if (isSet && _config._type == SliderType.RightRange && i >= _currentOptions [0]) {
					drawRange = i >= _currentOptions [0];
				} else if (isSet && _config._type == SliderType.Range && ((i >= _currentOptions [0] && i <= _currentOptions [1]) || (i >= _currentOptions [1] && i <= _currentOptions [0]))) {
					drawRange = (i >= _currentOptions [0] && i < _currentOptions [1]) || (i >= _currentOptions [1] && i < _currentOptions [0]);
				} else {
					// Is Not a Range.
				}

				// Draw Option
				Driver.SetAttribute (isSet && _currentOptions.Contains (i) ? _style.SetChar.Attribute ?? setScheme : drawRange ? _style.RangeChar.Attribute ?? setScheme : _style.OptionChar.Attribute ?? normalScheme);

				// Note(jmperricone): Maybe only for curses, windows inverts actual colors, while curses inverts bg with fg.
				if (Application.Driver is CursesDriver) {
					if (_currentOption == i && HasFocus) {
						Driver.SetAttribute (ColorScheme.Focus);
					}
				}

				var rune = (isSet && _currentOptions.Contains (i) ? _style.SetChar.Runes [0] : drawRange ? _style.RangeChar.Runes [0] : _style.OptionChar.Runes [0]);
				MoveAndAdd (x, y, rune);
				if (isVertical) y++; else x++;

				// Draw Spacing
				if (_config._showSpacing || i < _options.Count - 1) { // Skip if is the Last Spacing.
					Driver.SetAttribute (drawRange && isSet ? _style.RangeChar.Attribute ?? setScheme : _style.SpaceChar.Attribute ?? normalScheme);
					for (int s = 0; s < _config._innerSpacing; s++) {
						MoveAndAdd (x, y, drawRange && isSet ? _style.RangeChar.Runes [0] : _style.SpaceChar.Runes [0]);
						if (isVertical) y++; else x++;
					}
				}
			}
		}

		var remaining = isVertical ? Bounds.Height - y : Bounds.Width - x;
		// Right Spacing
		if (_config._showSpacing) {
			Driver.SetAttribute (isSet && _config._type == SliderType.RightRange ? _style.RangeChar.Attribute ?? normalScheme : _style.SpaceChar.Attribute ?? normalScheme);
			var rune = isSet && _config._type == SliderType.RightRange ? _style.RangeChar.Runes [0] : _style.SpaceChar.Runes [0];
			for (int i = 0; i < remaining; i++) {
				MoveAndAdd (x, y, rune);
				if (isVertical) y++; else x++;
			}
		} else {
			Driver.SetAttribute (_style.EmptyChar.Attribute ?? normalScheme);
			for (int i = 0; i < remaining; i++) {
				MoveAndAdd (x, y, _style.EmptyChar.Runes [0]);
				if (isVertical) y++; else x++;
			}
		}
	}

	void DrawLegends ()
	{
		// Attributes
		var normalScheme = _style.LegendAttributes.NormalAttribute ?? ColorScheme?.Normal ?? ColorScheme.Disabled;
		var setScheme = _style.LegendAttributes.SetAttribute ?? ColorScheme?.HotNormal ?? ColorScheme.Normal;
		var spaceScheme = normalScheme;// style.LegendStyle.EmptyAttribute ?? normalScheme;

		var isTextVertical = _config._legendsOrientation == Orientation.Vertical;
		var isSet = _config._type == SliderType.Range ? _currentOptions.Count == 2 : _currentOptions.Count > 0;

		var x = 0;
		var y = 0;

		Move (x, y);

		if (_config._sliderOrientation == Orientation.Horizontal && _config._legendsOrientation == Orientation.Vertical) {
			x += _config._startSpacing;
		}
		if (_config._sliderOrientation == Orientation.Vertical && _config._legendsOrientation == Orientation.Horizontal) {
			y += _config._startSpacing;
		}

		if (_config._sliderOrientation == Orientation.Horizontal) {
			y += 1;
		} else { // Vertical
			x += 1;
		}

		for (int i = 0; i < _options.Count; i++) {

			bool isOptionSet = false;

			// Check if the Option is Set.
			switch (_config._type) {
			case SliderType.Single:
			case SliderType.Multiple:
				if (isSet && _currentOptions.Contains (i))
					isOptionSet = true;
				break;
			case SliderType.LeftRange:
				if (isSet && i <= _currentOptions [0])
					isOptionSet = true;
				break;
			case SliderType.RightRange:
				if (isSet && i >= _currentOptions [0])
					isOptionSet = true;
				break;
			case SliderType.Range:
				if (isSet && ((i >= _currentOptions [0] && i <= _currentOptions [1]) || (i >= _currentOptions [1] && i <= _currentOptions [0])))
					isOptionSet = true;
				break;
			}

			// Text || Abbreviation
			string text = string.Empty;
			if (_config._showLegendsAbbr) {
				text = _options [i].LegendAbbr.ToString () ?? new Rune (_options [i].Legend.First ()).ToString ();
			} else {
				text = _options [i].Legend;
			}

			switch (_config._sliderOrientation) {
			case Orientation.Horizontal:
				switch (_config._legendsOrientation) {
				case Orientation.Horizontal:
					text = AlignText (text, _config._innerSpacing + 1, TextAlignment.Centered);
					break;
				case Orientation.Vertical:
					y = 1;
					break;
				}
				break;
			case Orientation.Vertical:
				switch (_config._legendsOrientation) {
				case Orientation.Horizontal:
					x = 1;
					break;
				case Orientation.Vertical:
					text = AlignText (text, _config._innerSpacing + 1, TextAlignment.Centered);
					break;
				}
				break;
			}

			// Text
			var legend_left_spaces_count = text.TakeWhile (e => e == ' ').Count ();
			var legend_right_spaces_count = text.Reverse ().TakeWhile (e => e == ' ').Count ();
			text = text.Trim ();

			// TODO(jmperricone): Improve the Orientation check.

			// Calculate Start Spacing
			if (_config._sliderOrientation == _config._legendsOrientation) {
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
					legend_left_spaces_count = _config._startSpacing - chars_left;
				}

				// Option Left Spacing
				if (isTextVertical) y += legend_left_spaces_count;
				else x += legend_left_spaces_count;
				//Move (x, y);
			}

			// Legend
			Driver.SetAttribute (isOptionSet ? setScheme : normalScheme);
			foreach (var c in text.EnumerateRunes ()) {
				MoveAndAdd (x, y, c);
				//Driver.AddRune (c);
				if (isTextVertical) y += 1;
				else x += 1;
			}

			// Calculate End Spacing
			if (i == _options.Count () - 1) {
				// See Start Spacing explanation.
				var chars_right = text.Length / 2;
				legend_right_spaces_count = _config._endSpacing - chars_right;
			}

			// Option Right Spacing of Option
			Driver.SetAttribute (spaceScheme);
			if (isTextVertical) y += legend_right_spaces_count;
			else x += legend_right_spaces_count;
			//Move (x, y);

			if (_config._sliderOrientation == Orientation.Horizontal && _config._legendsOrientation == Orientation.Vertical) {
				x += _config._innerSpacing + 1;
			} else if (_config._sliderOrientation == Orientation.Vertical && _config._legendsOrientation == Orientation.Horizontal) {
				y += _config._innerSpacing + 1;
			}
		}
	}

	#endregion

	#region Keys and Mouse


	Point? _dragPosition;
	Point? _moveRenderPosition;

	/// <inheritdoc/>
	public override bool MouseEvent (MouseEvent mouseEvent)
	{
		// Note(jmperricone): Maybe we click to focus the cursor, and on next click we set the option.
		//                    That will makes OptionFocused Event more relevant.
		// TODO(jmperricone): Make Range Type work with mouse.

		if (!(mouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked) ||
			mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed) ||
			mouseEvent.Flags.HasFlag (MouseFlags.ReportMousePosition) ||
			mouseEvent.Flags.HasFlag (MouseFlags.Button1Released))) {
			return false;
		}

		Point ClampMovePosition (Point position)
		{
			int Clamp (int value, int min, int max) => Math.Max (min, Math.Min (max, value));

			if (SliderOrientation == Orientation.Horizontal) {
				var left = _config._startSpacing;
				var width = _options.Count + (_options.Count - 1) * _config._innerSpacing;
				var right = (left + width - 1);
				var clampedX = Clamp (position.X, left, right);
				position = new Point (clampedX, 0);
			} else {
				var top = _config._startSpacing;
				var height = _options.Count + (_options.Count - 1) * _config._innerSpacing;
				var bottom = (top + height - 1);
				var clampedY = Clamp (position.Y, top, bottom);
				position = new Point (0, clampedY);
			}
			return position;
		}

		SetFocus ();

		if (!_dragPosition.HasValue && (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed))) {

			if (mouseEvent.Flags.HasFlag (MouseFlags.ReportMousePosition)) {
				_dragPosition = new Point (mouseEvent.X, mouseEvent.Y);
				_moveRenderPosition = ClampMovePosition ((Point)_dragPosition);
				Application.GrabMouse (this);
			}
			SetNeedsDisplay ();
			return true;
		}

		if (_dragPosition.HasValue && mouseEvent.Flags.HasFlag (MouseFlags.ReportMousePosition) && mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed)) {

			// Continue Drag
			_dragPosition = new Point (mouseEvent.X, mouseEvent.Y);
			_moveRenderPosition = ClampMovePosition ((Point)_dragPosition);

			var success = false;
			var option = 0;
			// how far has user dragged from original location?						
			if (SliderOrientation == Orientation.Horizontal) {
				success = TryGetOptionByPosition (mouseEvent.X, 0, Math.Max (0, _config._innerSpacing / 2), out option);
			} else {
				success = TryGetOptionByPosition (0, mouseEvent.Y, Math.Max (0, _config._innerSpacing / 2), out option);
			}
			if (!_config._allowEmpty && success) {
				_currentOption = option;
				OptionFocused?.Invoke (_currentOption, _options [_currentOption]);
				SetCurrentOption ();
			}

			SetNeedsDisplay ();
			return true;
		}

		if ((_dragPosition.HasValue && mouseEvent.Flags.HasFlag (MouseFlags.Button1Released)) || mouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked)) {

			// End Drag
			Application.UngrabMouse ();
			_dragPosition = null;
			_moveRenderPosition = null;

			// TODO: Add func to calc distance between options to use as the MouseClickXOptionThreshold
			var success = false;
			var option = 0;
			if (SliderOrientation == Orientation.Horizontal) {
				success = TryGetOptionByPosition (mouseEvent.X, 0, Math.Max (0, _config._innerSpacing / 2), out option);
			} else {
				success = TryGetOptionByPosition (0, mouseEvent.Y, Math.Max (0, _config._innerSpacing / 2), out option);
			}
			if (success) {
				_currentOption = option;
				OptionFocused?.Invoke (_currentOption, _options [_currentOption]);
				SetCurrentOption ();
			}

			SetNeedsDisplay ();
			return true;
		}
		return false;
	}

	// TODO: Use Commands instead
	/// <inheritdoc/>
	public override bool ProcessKey (KeyEvent keyEvent)
	{
		switch (keyEvent.Key) {
		case Key.CursorLeft | Key.CtrlMask:
			if (_currentOptions.Contains (_currentOption)) {
				var prev = _currentOption > 0 ? _currentOption - 1 : _currentOption;
				if (!_currentOptions.Contains (prev) || (_config._type == SliderType.Range && _config._rangeAllowSingle)) {
					_currentOptions.Remove (_currentOption);
					_currentOption = prev;
					// Note(jmperricone): We are setting the option here, do we send the OptionFocused Event too ?
					OptionFocused?.Invoke (_currentOption, _options [_currentOption]);
					_currentOptions.Add (_currentOption);
					_currentOptions.Sort (); // Range Type
					OptionsChanged?.Invoke (_currentOptions.ToDictionary (e => e, e => _options [e]));
				} else {
					if (_settingRange == true || !AllowEmpty) {
						SetCurrentOption ();
					}
				}
			}
			break;
		case Key.CursorRight | Key.CtrlMask:
			if (_currentOptions.Contains (_currentOption)) {
				var next = _currentOption < _options.Count - 1 ? _currentOption + 1 : _currentOption;
				if (!_currentOptions.Contains (next) || (_config._type == SliderType.Range && _config._rangeAllowSingle)) {
					_currentOptions.Remove (_currentOption);
					_currentOption = next;
					// Note(jmperricone): We are setting the option here, do we send the OptionFocused Event too ?
					OptionFocused?.Invoke (_currentOption, _options [_currentOption]);
					_currentOptions.Add (_currentOption);
					_currentOptions.Sort (); // Range Type
					OptionsChanged?.Invoke (_currentOptions.ToDictionary (e => e, e => _options [e]));
				} else {
					if (_settingRange == true || !AllowEmpty) {
						SetCurrentOption ();
					}
				}
			}
			break;
		case Key.Home:
			_currentOption = 0;
			OptionFocused?.Invoke (_currentOption, _options [_currentOption]);
			if (_settingRange == true || !AllowEmpty) {
				SetCurrentOption ();
			}
			break;
		case Key.End:
			_currentOption = _options.Count - 1;
			OptionFocused?.Invoke (_currentOption, _options [_currentOption]);
			if (_settingRange == true || !AllowEmpty) {
				SetCurrentOption ();
			}
			break;
		case Key.CursorUp:
		case Key.CursorLeft:

			if (keyEvent.Key == Key.CursorUp && _config._sliderOrientation == Orientation.Horizontal) return false;
			if (keyEvent.Key == Key.CursorLeft && _config._sliderOrientation == Orientation.Vertical) return false;

			_currentOption = _currentOption > 0 ? _currentOption - 1 : _currentOption;
			OptionFocused?.Invoke (_currentOption, _options [_currentOption]);

			if (_settingRange == true || !AllowEmpty) {
				SetCurrentOption ();
			}
			break;
		case Key.CursorDown:
		case Key.CursorRight:

			if (keyEvent.Key == Key.CursorDown && _config._sliderOrientation == Orientation.Horizontal) return false;
			if (keyEvent.Key == Key.CursorRight && _config._sliderOrientation == Orientation.Vertical) return false;

			_currentOption = _currentOption < _options.Count - 1 ? _currentOption + 1 : _currentOption;
			OptionFocused?.Invoke (_currentOption, _options [_currentOption]);

			if (_settingRange == true || !AllowEmpty) {
				SetCurrentOption ();
			}
			break;

		case Key.Enter:
		case Key.Space:
			if (_settingRange == true) {
				_settingRange = false;
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
