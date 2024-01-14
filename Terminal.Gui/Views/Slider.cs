using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Terminal.Gui;

/// <summary>
/// <see cref="EventArgs"/> for <see cref="Slider{T}"/> <see cref="SliderOption{T}"/> events.
/// </summary>
public class SliderOptionEventArgs : EventArgs {
	/// <summary>
	/// Initializes a new instance of <see cref="SliderOptionEventArgs"/>
	/// </summary>
	/// <param name="isSet"> indicates whether the option is set</param>
	public SliderOptionEventArgs (bool isSet) => IsSet = isSet;

	/// <summary>
	/// Gets whether the option is set or not.
	/// </summary>
	public bool IsSet { get; }
}

/// <summary>
/// Represents an option in a <see cref="Slider{T}"/> .
/// </summary>
/// <typeparam name="T">Data type of the option.</typeparam>
public class SliderOption<T> {
	/// <summary>
	/// Creates a new empty instance of the <see cref="SliderOption{T}"/> class.
	/// </summary>
	public SliderOption () { }

	/// <summary>
	/// Creates a new instance of the <see cref="SliderOption{T}"/> class with values for
	/// each property.
	/// </summary>
	public SliderOption (string legend, Rune legendAbbr, T data)
	{
		Legend = legend;
		LegendAbbr = legendAbbr;
		Data = data;
	}

	/// <summary>
	/// Legend of the option.
	/// </summary>
	public string Legend { get; set; }

	/// <summary>
	/// Abbreviation of the Legend. When the <see cref="Slider{T}.InnerSpacing"/> too small to fit
	/// <see cref="Legend"/>.
	/// </summary>
	public Rune LegendAbbr { get; set; }

	/// <summary>
	/// Custom data of the option.
	/// </summary>
	public T Data { get; set; }

	/// <summary>
	/// To Raise the <see cref="Set"/> event from the Slider.
	/// </summary>
	internal void OnSet () => Set?.Invoke (this, new SliderOptionEventArgs (true));

	/// <summary>
	/// To Raise the <see cref="UnSet"/> event from the Slider.
	/// </summary>
	internal void OnUnSet () => UnSet?.Invoke (this, new SliderOptionEventArgs (false));

	/// <summary>
	/// To Raise the <see cref="Changed"/> event from the Slider.
	/// </summary>
	internal void OnChanged (bool isSet) => Changed?.Invoke (this, new SliderOptionEventArgs (isSet));

	/// <summary>
	/// Event Raised when this option is set.
	/// </summary>
	public event EventHandler<SliderOptionEventArgs> Set;

	/// <summary>
	/// Event Raised when this option is unset.
	/// </summary>
	public event EventHandler<SliderOptionEventArgs> UnSet;

	/// <summary>
	/// Event fired when the an option has changed.
	/// </summary>
	public event EventHandler<SliderOptionEventArgs> Changed;

	/// <summary>
	/// Creates a human-readable string that represents this <see cref="SliderOption{T}"/>.
	/// </summary>
	public override string ToString () => "{Legend=" + Legend + ", LegendAbbr=" + LegendAbbr + ", Data=" + Data + "}";
}

/// <summary>
/// <see cref="Slider{T}"/>  Types
/// </summary>
public enum SliderType {
	/// <summary>
	///         <code>
	/// ├─┼─┼─┼─┼─█─┼─┼─┼─┼─┼─┼─┤
	/// </code>
	/// </summary>
	Single,

	/// <summary>
	///         <code>
	/// ├─┼─█─┼─┼─█─┼─┼─┼─┼─█─┼─┤
	/// </code>
	/// </summary>
	Multiple,

	/// <summary>
	///         <code>
	/// ├▒▒▒▒▒▒▒▒▒█─┼─┼─┼─┼─┼─┼─┤
	/// </code>
	/// </summary>
	LeftRange,

	/// <summary>
	///         <code>
	/// ├─┼─┼─┼─┼─█▒▒▒▒▒▒▒▒▒▒▒▒▒┤
	/// </code>
	/// </summary>
	RightRange,

	/// <summary>
	///         <code>
	/// ├─┼─┼─┼─┼─█▒▒▒▒▒▒▒█─┼─┼─┤
	/// </code>
	/// </summary>
	Range
}

/// <summary>
/// <see cref="Slider{T}"/> Legend Style
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
/// <see cref="Slider{T}"/> Style
/// </summary>
public class SliderStyle {
	/// <summary>
	/// Constructs a new instance.
	/// </summary>
	public SliderStyle () => LegendAttributes = new SliderAttributes ();

	/// <summary>
	/// Legend attributes
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
}

/// <summary>
/// All <see cref="Slider{T}"/> configuration are grouped in this class.
/// </summary>
class SliderConfiguration {
	internal bool _allowEmpty;

	internal bool _autoSize;
	internal int _endSpacing;
	internal int _innerSpacing;
	internal Orientation _legendsOrientation = Orientation.Horizontal;
	internal bool _rangeAllowSingle;

	internal bool _showEndSpacing;
	internal bool _showLegends;
	internal bool _showLegendsAbbr;
	internal Orientation _sliderOrientation = Orientation.Horizontal;

	internal int _startSpacing;

	internal SliderType _type = SliderType.Single;
}

/// <summary>
/// <see cref="EventArgs"/> for <see cref="Slider{T}"/> events.
/// </summary>
public class SliderEventArgs<T> : EventArgs {
	/// <summary>
	/// Initializes a new instance of <see cref="SliderEventArgs{T}"/>
	/// </summary>
	/// <param name="options">The current options.</param>
	/// <param name="focused">Index of the option that is focused. -1 if no option has the focus.</param>
	public SliderEventArgs (Dictionary<int, SliderOption<T>> options, int focused = -1)
	{
		Options = options;
		Focused = focused;
		Cancel = false;
	}

	/// <summary>
	/// Gets/sets whether the option is set or not.
	/// </summary>
	public Dictionary<int, SliderOption<T>> Options { get; set; }

	/// <summary>
	/// Gets or sets the index of the option that is focused.
	/// </summary>
	public int Focused { get; set; }

	/// <summary>
	/// If set to true, the focus operation will be canceled, if applicable.
	/// </summary>
	public bool Cancel { get; set; }
}

/// <summary>
/// <see cref="EventArgs"/> for <see cref="Orientation"/> events.
/// </summary>
public class OrientationEventArgs : EventArgs {
	/// <summary>
	/// Constructs a new instance.
	/// </summary>
	/// <param name="orientation">the new orientation</param>
	public OrientationEventArgs (Orientation orientation)
	{
		Orientation = orientation;
		Cancel = false;
	}

	/// <summary>
	/// The new orientation.
	/// </summary>
	public Orientation Orientation { get; set; }

	/// <summary>
	/// If set to true, the orientation change operation will be canceled, if applicable.
	/// </summary>
	public bool Cancel { get; set; }
}

/// <summary>
/// Slider control.
/// </summary>
public class Slider : Slider<object> {
	/// <summary>
	/// Initializes a new instance of the <see cref="Slider"/> class.
	/// </summary>
	public Slider () { }

	/// <summary>
	/// Initializes a new instance of the <see cref="Slider"/> class.
	/// </summary>
	/// <param name="options">Initial slider options.</param>
	/// <param name="orientation">Initial slider options.</param>
	public Slider (List<object> options, Orientation orientation = Orientation.Horizontal) : base (options, orientation) { }
}

/// <summary>
/// Provides a slider control letting the user navigate from a set of typed options in a linear manner
/// using the keyboard or mouse.
/// </summary>
/// <typeparam name="T"></typeparam>
public class Slider<T> : View {
	readonly SliderConfiguration _config = new ();

	// List of the current set options.
	readonly List<int> _setOptions = new ();

	// Options
	List<SliderOption<T>> _options;

	/// <summary>
	/// The focused option (has the cursor).
	/// </summary>
	public int FocusedOption { get; set; }

	#region Initialize
	void SetInitialProperties (List<SliderOption<T>> options, Orientation orientation = Orientation.Horizontal)
	{
		CanFocus = true;

		_options = options ?? new List<SliderOption<T>> ();

		_config._sliderOrientation = orientation;

		_config._showLegends = true;

		SetDefaultStyle ();
		SetCommands ();

		// When we lose focus of the View(Slider), if we are range selecting we stop it.
		Leave += (s, e) => {
			//if (_settingRange == true) {
			//	_settingRange = false;
			//}
			Driver.SetCursorVisibility (CursorVisibility.Invisible);
		};


		Enter += (s, e) => { };

		LayoutComplete += (s, e) => {
			CalcSpacingConfig ();
			SetBoundsBestFit ();
		};
	}
	#endregion

	#region Events
	/// <summary>
	/// Event raised when the slider option/s changed.
	/// The dictionary contains: key = option index, value = T
	/// </summary>
	public event EventHandler<SliderEventArgs<T>> OptionsChanged;

	/// <summary>
	/// Overridable method called when the slider options have changed. Raises the
	/// <see cref="OptionsChanged"/> event.
	/// </summary>
	public virtual void OnOptionsChanged ()
	{
		OptionsChanged?.Invoke (this, new SliderEventArgs<T> (GetSetOptionDictionary ()));
		SetNeedsDisplay ();
	}

	/// <summary>
	/// Event raised When the option is hovered with the keys or the mouse.
	/// </summary>
	public event EventHandler<SliderEventArgs<T>> OptionFocused;

	int _lastFocusedOption; // for Range type; the most recently focused option. Used to determine shrink direction

	/// <summary>
	/// Overridable function that fires the <see cref="OptionFocused"/> event.
	/// </summary>
	/// <param name="args"></param>
	/// <returns><see langword="true"/> if the focus change was cancelled.</returns>
	/// <param name="newFocusedOption"></param>
	public virtual bool OnOptionFocused (int newFocusedOption, SliderEventArgs<T> args)
	{
		if (newFocusedOption > _options.Count - 1 || newFocusedOption < 0) {
			return true;
		}
		OptionFocused?.Invoke (this, args);
		if (!args.Cancel) {
			_lastFocusedOption = FocusedOption;
			FocusedOption = newFocusedOption;
			PositionCursor ();
		}
		return args.Cancel;
	}
	#endregion

	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="Slider"/> class.
	/// </summary>
	public Slider () : this (new List<T> ()) { }

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
					LegendAbbr = (Rune)(legend?.Length > 0 ? legend [0] : ' ')
				};
			}).ToList (), orientation);
		}
	}
	#endregion

	#region Properties
	/// <summary>
	/// Allow no selection.
	/// </summary>
	public bool AllowEmpty {
		get => _config._allowEmpty;
		set {
			_config._allowEmpty = value;
			if (!value && _options.Count > 0 && _setOptions.Count == 0) {
				SetOption (0);
			}
		}
	}

	/// <summary>
	/// If <see langword="true"/> the slider will be sized to fit the available space (the Bounds of the
	/// the SuperView).
	/// </summary>
	/// <remarks>
	/// For testing, if there is no SuperView, the slider will be sized based on what
	/// <see cref="InnerSpacing"/> is
	/// set to.
	/// </remarks>
	public override bool AutoSize {
		get => _config._autoSize;
		set {
			_config._autoSize = value;
			if (IsInitialized) {
				CalcSpacingConfig ();
				SetBoundsBestFit ();

			}
		}
	}

	/// <summary>
	/// Gets or sets the number of rows/columns between <see cref="Options"/>
	/// </summary>
	public int InnerSpacing {
		get => _config._innerSpacing;
		set {
			_config._innerSpacing = value;
			if (IsInitialized) {
				CalcSpacingConfig ();
				SetBoundsBestFit ();

			}
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
			_setOptions.Clear ();
			SetNeedsDisplay ();
		}
	}

	/// <summary>
	/// Slider Orientation. <see cref="Gui.Orientation"></see>
	/// </summary>
	public Orientation Orientation {
		get => _config._sliderOrientation;
		set => OnOrientationChanged (value);
	}

	/// <summary>
	/// Fired when the slider orientation has changed. Can be cancelled by setting
	/// <see cref="OrientationEventArgs.Cancel"/> to true.
	/// </summary>
	public event EventHandler<OrientationEventArgs> OrientationChanged;

	/// <summary>
	/// Called when the slider orientation has changed. Invokes the <see cref="OrientationChanged"/> event.
	/// </summary>
	/// <param name="newOrientation"></param>
	/// <returns>True of the event was cancelled.</returns>
	public virtual bool OnOrientationChanged (Orientation newOrientation)
	{
		var args = new OrientationEventArgs (newOrientation);
		OrientationChanged?.Invoke (this, args);
		if (!args.Cancel) {
			_config._sliderOrientation = newOrientation;
			SetKeyBindings ();
			if (IsInitialized) {
				CalcSpacingConfig ();
				SetBoundsBestFit ();

			}
		}
		return args.Cancel;
	}

	/// <summary>
	/// Legends Orientation. <see cref="Gui.Orientation"></see>
	/// </summary>
	public Orientation LegendsOrientation {
		get => _config._legendsOrientation;
		set {
			_config._legendsOrientation = value;
			if (IsInitialized) {
				CalcSpacingConfig ();
				SetBoundsBestFit ();

			}
		}
	}

	/// <summary>
	/// Slider styles. <see cref="SliderStyle"></see>
	/// </summary>
	public SliderStyle Style {
		// Note(jmperricone): Maybe SliderStyle should be a struct so we return a copy ???
		// Or SetStyle() and ( GetStyle() || Style getter copy )
		get;
		set;
	} = new ();

	/// <summary>
	/// Set the slider options.
	/// </summary>
	public List<SliderOption<T>> Options {
		get =>
			// Note(jmperricone): Maybe SliderOption should be a struct so we return a copy ???
			//                    Events will be preserved ? Need a test.
			// Or SetOptions() and ( GetOptions() || Options getter copy )
			_options;
		set {
			// _options should never be null
			_options = value ?? throw new ArgumentNullException (nameof (value));

			if (!IsInitialized || _options.Count == 0) {
				return;
			}

			CalcSpacingConfig ();
			SetBoundsBestFit ();
		}
	}

	/// <summary>
	/// Allow range start and end be in the same option, as a single option.
	/// </summary>
	public bool RangeAllowSingle {
		get => _config._rangeAllowSingle;
		set => _config._rangeAllowSingle = value;
	}

	/// <summary>
	/// Show/Hide spacing before and after the first and last option.
	/// </summary>
	public bool ShowEndSpacing {
		get => _config._showEndSpacing;
		set {
			_config._showEndSpacing = value;
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
			SetBoundsBestFit ();

		}
	}

	/// <summary>
	/// Causes the specified option to be set and be focused.
	/// </summary>
	public bool SetOption (int optionIndex)
	{
		// TODO: Handle range type.			
		// Note: Maybe return false only when optionIndex doesn't exist, otherwise true.

		if (!_setOptions.Contains (optionIndex) && optionIndex >= 0 && optionIndex < _options.Count) {
			FocusedOption = optionIndex;
			SetFocusedOption ();
			return true;
		}
		return false;
	}

	/// <summary>
	/// Causes the specified option to be un-set and be focused.
	/// </summary>
	public bool UnSetOption (int optionIndex)
	{
		// TODO: Handle range type.			
		if (!AllowEmpty && _setOptions.Count > 2 && _setOptions.Contains (optionIndex)) {
			FocusedOption = optionIndex;
			SetFocusedOption ();
			return true;
		}
		return false;
	}

	/// <summary>
	/// Get the indexes of the set options.
	/// </summary>
	public List<int> GetSetOptions () =>
		// Copy
		_setOptions.OrderBy (e => e).ToList ();
	#endregion

	#region Helpers
	void MoveAndAdd (int x, int y, Rune rune)
	{
		Move (x, y);
		Driver?.AddRune (rune);
	}

	void MoveAndAdd (int x, int y, string str)
	{
		Move (x, y);
		Driver?.AddStr (str);
	}

	// TODO: Make configurable via ConfigurationManager
	void SetDefaultStyle ()
	{
		switch (_config._sliderOrientation) {
		case Orientation.Horizontal:
			Style.SpaceChar = new Cell { Rune = Glyphs.HLine };        // '─'
			Style.OptionChar = new Cell { Rune = Glyphs.BlackCircle }; // '┼●🗹□⏹'
			break;
		case Orientation.Vertical:
			Style.SpaceChar = new Cell { Rune = Glyphs.VLine };
			Style.OptionChar = new Cell { Rune = Glyphs.BlackCircle };
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
		Style.EmptyChar = new Cell { Rune = new Rune (' ') };
		Style.SetChar = new Cell { Rune = Glyphs.ContinuousMeterSegment }; // ■
		Style.RangeChar = new Cell { Rune = Glyphs.Stipple };              // ░ ▒ ▓   // Medium shade not blinking on curses.
		Style.StartRangeChar = new Cell { Rune = Glyphs.ContinuousMeterSegment };
		Style.EndRangeChar = new Cell { Rune = Glyphs.ContinuousMeterSegment };
		Style.DragChar = new Cell { Rune = Glyphs.Diamond };

		// TODO: Support left & right (top/bottom)
		// First = '├',
		// Last = '┤',
	}

	/// <summary>
	/// Calculates the spacing configuration (start, inner, end) as well as turning on/off legend
	/// abbreviation if needed. Behaves differently based on <see cref="AutoSize"/> and
	/// <see cref="View.IsInitialized"/> .
	/// </summary>
	internal void CalcSpacingConfig ()
	{
		var size = 0;

		if (_options.Count == 0 || !IsInitialized) {
			return;
		}

		_config._innerSpacing = 0;
		_config._startSpacing = 0;
		_config._endSpacing = 0;

		if (AutoSize) {
			// Max size is SuperView's Bounds. Min Size is size that will fit.
			if (SuperView != null) {
				// Calculate the size of the slider based on the size of the SuperView's Bounds.
				if (_config._sliderOrientation == Orientation.Horizontal) {
					size = int.Min (SuperView.Bounds.Width, CalcBestLength ());
				} else {
					size = int.Min (SuperView.Bounds.Height, CalcBestLength ());
				}

			} else {
				// Use the config values
				size = CalcMinLength ();
				return;
			}
		} else {
			// Fit Slider to the Bounds
			if (_config._sliderOrientation == Orientation.Horizontal) {
				size = Bounds.Width;
			} else {
				size = Bounds.Height;
			}
		}

		int max_legend; // Because the legends are centered, the longest one determines inner spacing
		if (_config._sliderOrientation == _config._legendsOrientation) {
			max_legend = int.Max (_options.Max (s => s.Legend?.Length ?? 1), 1);
		} else {
			max_legend = 1;
		}

		var min_size_that_fits_legends = _options.Count == 1 ? max_legend : max_legend / (_options.Count - 1);

		string first;
		string last;

		if (max_legend >= size) {
			if (_config._sliderOrientation == _config._legendsOrientation) {
				_config._showLegendsAbbr = true;
				foreach (var o in _options.Where (op => op.LegendAbbr == default)) {
					o.LegendAbbr = (Rune)(o.Legend?.Length > 0 ? o.Legend [0] : ' ');
				}
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
		var last_right = last.Length / 2;        // Chars count of the last option to the right.

		if (_config._sliderOrientation != _config._legendsOrientation) {
			first_left = 0;
			last_right = 0;
		}

		// -1 because it's better to have an extra space at right than to clip
		var width = size - first_left - last_right - 1;

		_config._startSpacing = first_left;
		if (_options.Count == 1) {
			_config._innerSpacing = max_legend;
		} else {
			_config._innerSpacing = Math.Max (0, (int)Math.Floor ((double)width / (_options.Count - 1)) - 1);
		}
		_config._endSpacing = last_right;

	}

	/// <summary>
	///  Adjust the dimensions of the Slider to the best value if <see cref="AutoSize"/> is true.
	/// </summary>
	public void SetBoundsBestFit ()
	{
		if (!IsInitialized || AutoSize == false) {
			return;
		}
		Width = 0;
		Height = 0;
		if (_config._sliderOrientation == Orientation.Horizontal) {
			Bounds = new Rect (Bounds.Location,
				new Size (int.Min (SuperView.Bounds.Width - GetAdornmentsThickness ().Horizontal, CalcBestLength ()),
					int.Min (SuperView.Bounds.Height - GetAdornmentsThickness ().Vertical, CalcThickness ())));
		} else {
			Bounds = new Rect (Bounds.Location,
				new Size (int.Min (SuperView.Bounds.Width - GetAdornmentsThickness ().Horizontal, CalcThickness ()),
					int.Min (SuperView.Bounds.Height - GetAdornmentsThickness ().Vertical, CalcBestLength ())));
		}
	}

	/// <summary>
	/// Calculates the min dimension required for all options and inner spacing with abbreviated legends
	/// </summary>
	/// <returns></returns>
	int CalcMinLength ()
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

	/// <summary>
	/// Calculates the ideal dimension required for all options, inner spacing, and legends
	/// (non-abbreviated).
	/// </summary>
	/// <returns></returns>
	int CalcBestLength ()
	{
		if (_options.Count == 0) {
			return 0;
		}

		var length = 0;

		if (_config._showLegends) {
			var max_legend = 1;
			if (_config._legendsOrientation == _config._sliderOrientation && _options.Count > 0) {
				max_legend = int.Max (_options.Max (s => s.Legend?.Length + 1 ?? 1), 1);
				length += max_legend * _options.Count;
				//length += (max_legend / 2);
			} else {
				length += 1;
			}
		}
		return int.Max (length, CalcMinLength ());
	}

	/// <summary>
	/// Calculates the min dimension required for the slider and legends
	/// </summary>
	/// <returns></returns>
	int CalcThickness ()
	{
		var thickness = 1; // Always show the slider.

		if (_config._showLegends) {
			if (_config._legendsOrientation != _config._sliderOrientation && _options.Count > 0) {
				thickness += _options.Max (s => s.Legend?.Length ?? 0);
			} else {
				thickness += 1;
			}
		}

		return thickness;
	}

	internal bool TryGetPositionByOption (int option, out (int x, int y) position)
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

	/// <summary>
	/// Tries to get the option index by the position.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="threshold"></param>
	/// <param name="option_idx"></param>
	/// <returns></returns>
	internal bool TryGetOptionByPosition (int x, int y, int threshold, out int option_idx)
	{
		// Fix(jmperricone): Not working.
		option_idx = -1;
		if (Orientation == Orientation.Horizontal) {
			if (y != 0) {
				return false;
			}

			for (var xx = x - threshold; xx < x + threshold + 1; xx++) {
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

			for (var yy = y - threshold; yy < y + threshold + 1; yy++) {
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
	#endregion

	#region Cursor and Drawing
	/// <inheritdoc/>
	public override void PositionCursor ()
	{
		//base.PositionCursor ();

		if (HasFocus) {
			Driver?.SetCursorVisibility (CursorVisibility.Default);
		} else {
			Driver?.SetCursorVisibility (CursorVisibility.Invisible);
		}
		if (TryGetPositionByOption (FocusedOption, out var position)) {
			if (IsInitialized && Bounds.Contains (position.x, position.y)) {
				Move (position.x, position.y);
			}
		}
	}

	/// <inheritdoc/>
	public override void OnDrawContent (Rect contentArea)
	{
		// TODO: make this more surgical to reduce repaint

		if (_options == null && _options.Count > 0) {
			return;
		}

		// Debug
#if (DEBUG)
		Driver?.SetAttribute (new Attribute (Color.White, Color.Red));
		for (var y = 0; y < contentArea.Height; y++) {
			for (var x = 0; x < contentArea.Width; x++) {
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
			AddRune (_moveRenderPosition.Value.X, _moveRenderPosition.Value.Y, Style.DragChar.Rune);
		}
	}

	string AlignText (string text, int width, TextAlignment textAlignment)
	{
		if (text == null) {
			return "";
		}

		if (text.Length > width) {
			text = text [..width];
		}

		var w = width - text.Length;
		string s1 = new (' ', w / 2);
		string s2 = new (' ', w % 2);

		// Note: The formatter doesn't handle all of this ???
		switch (textAlignment) {
		case TextAlignment.Justified:
			return TextFormatter.Justify (text, width);
		case TextAlignment.Left:
			return text + s1 + s1 + s2;
		case TextAlignment.Centered:
			if (text.Length % 2 != 0) {
				return s1 + text + s1 + s2;
			}
			return s1 + s2 + text + s1;
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

		var normalAttr = new Attribute (Color.White, Color.Black);
		var setAtrr = new Attribute (Color.Black, Color.White);
		if (IsInitialized) {
			normalAttr = ColorScheme?.Normal ?? Application.Current.ColorScheme.Normal;
			setAtrr = Style.SetChar.Attribute ?? ColorScheme.HotNormal;
		}

		var isVertical = _config._sliderOrientation == Orientation.Vertical;
		var isLegendsVertical = _config._legendsOrientation == Orientation.Vertical;
		var isReverse = _config._sliderOrientation != _config._legendsOrientation;

		var x = 0;
		var y = 0;

		var isSet = _setOptions.Count > 0;

		// Left Spacing
		if (_config._showEndSpacing && _config._startSpacing > 0) {

			Driver?.SetAttribute (isSet && _config._type == SliderType.LeftRange ? Style.RangeChar.Attribute ?? normalAttr : Style.SpaceChar.Attribute ?? normalAttr);
			var rune = isSet && _config._type == SliderType.LeftRange ? Style.RangeChar.Rune : Style.SpaceChar.Rune;

			for (var i = 0; i < _config._startSpacing; i++) {
				MoveAndAdd (x, y, rune);
				if (isVertical) {
					y++;
				} else {
					x++;
				}
			}
		} else {
			Driver?.SetAttribute (Style.EmptyChar.Attribute ?? normalAttr);
			for (var i = 0; i < _config._startSpacing; i++) {
				MoveAndAdd (x, y, Style.EmptyChar.Rune);
				if (isVertical) {
					y++;
				} else {
					x++;
				}
			}
		}

		// Slider
		if (_options.Count > 0) {
			for (var i = 0; i < _options.Count; i++) {

				var drawRange = false;

				if (isSet) {
					switch (_config._type) {
					case SliderType.LeftRange when i <= _setOptions [0]:
						drawRange = i < _setOptions [0];
						break;
					case SliderType.RightRange when i >= _setOptions [0]:
						drawRange = i >= _setOptions [0];
						break;
					case SliderType.Range when _setOptions.Count == 1:
						drawRange = false;
						break;
					case SliderType.Range when _setOptions.Count == 2:
						if (i >= _setOptions [0] && i <= _setOptions [1] || i >= _setOptions [1] && i <= _setOptions [0]) {
							drawRange = i >= _setOptions [0] && i < _setOptions [1] || i >= _setOptions [1] && i < _setOptions [0];

						}
						break;
					}
				}

				// Draw Option
				Driver?.SetAttribute (isSet && _setOptions.Contains (i) ? Style.SetChar.Attribute ?? setAtrr : drawRange
					? Style.RangeChar.Attribute ?? setAtrr : Style.OptionChar.Attribute ?? normalAttr);

				// Note(jmperricone): Maybe only for curses, windows inverts actual colors, while curses inverts bg with fg.
				//if (Application.Driver is CursesDriver) {
				//	if (FocusedOption == i && HasFocus) {
				//		Driver.SetAttribute (ColorScheme.Focus);
				//	}
				//}
				var rune = drawRange ? Style.RangeChar.Rune : Style.OptionChar.Rune;
				if (isSet) {
					if (_setOptions [0] == i) {
						rune = Style.StartRangeChar.Rune;
					} else if (_setOptions.Count > 1 && _setOptions [1] == i) {
						rune = Style.EndRangeChar.Rune;
					} else if (_setOptions.Contains (i)) {
						rune = Style.SetChar.Rune;
					}
				}
				MoveAndAdd (x, y, rune);
				if (isVertical) {
					y++;
				} else {
					x++;
				}

				// Draw Spacing
				if (_config._showEndSpacing || i < _options.Count - 1) {
					// Skip if is the Last Spacing.
					Driver?.SetAttribute (drawRange && isSet ? Style.RangeChar.Attribute ?? setAtrr : Style.SpaceChar.Attribute ?? normalAttr);
					for (var s = 0; s < _config._innerSpacing; s++) {
						MoveAndAdd (x, y, drawRange && isSet ? Style.RangeChar.Rune : Style.SpaceChar.Rune);
						if (isVertical) {
							y++;
						} else {
							x++;
						}
					}
				}
			}
		}

		var remaining = isVertical ? Bounds.Height - y : Bounds.Width - x;
		// Right Spacing
		if (_config._showEndSpacing) {
			Driver?.SetAttribute (isSet && _config._type == SliderType.RightRange ? Style.RangeChar.Attribute ?? normalAttr : Style.SpaceChar.Attribute ?? normalAttr);
			var rune = isSet && _config._type == SliderType.RightRange ? Style.RangeChar.Rune : Style.SpaceChar.Rune;
			for (var i = 0; i < remaining; i++) {
				MoveAndAdd (x, y, rune);
				if (isVertical) {
					y++;
				} else {
					x++;
				}
			}
		} else {
			Driver?.SetAttribute (Style.EmptyChar.Attribute ?? normalAttr);
			for (var i = 0; i < remaining; i++) {
				MoveAndAdd (x, y, Style.EmptyChar.Rune);
				if (isVertical) {
					y++;
				} else {
					x++;
				}
			}
		}
	}

	void DrawLegends ()
	{
		// Attributes
		var normalAttr = new Attribute (Color.White, Color.Black);
		var setAttr = new Attribute (Color.Black, Color.White);
		var spaceAttr = normalAttr;
		if (IsInitialized) {
			normalAttr = Style.LegendAttributes.NormalAttribute ?? ColorScheme?.Normal ?? ColorScheme.Disabled;
			setAttr = Style.LegendAttributes.SetAttribute ?? ColorScheme?.HotNormal ?? ColorScheme.Normal;
			spaceAttr = Style.LegendAttributes.EmptyAttribute ?? normalAttr;
		}

		var isTextVertical = _config._legendsOrientation == Orientation.Vertical;
		var isSet = _setOptions.Count > 0;

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
		} else {
			// Vertical
			x += 1;
		}

		for (var i = 0; i < _options.Count; i++) {

			var isOptionSet = false;

			// Check if the Option is Set.
			switch (_config._type) {
			case SliderType.Single:
			case SliderType.Multiple:
				if (isSet && _setOptions.Contains (i)) {
					isOptionSet = true;
				}
				break;
			case SliderType.LeftRange:
				if (isSet && i <= _setOptions [0]) {
					isOptionSet = true;
				}
				break;
			case SliderType.RightRange:
				if (isSet && i >= _setOptions [0]) {
					isOptionSet = true;
				}
				break;
			case SliderType.Range when _setOptions.Count == 1:
				if (isSet && i == _setOptions [0]) {
					isOptionSet = true;
				}
				break;
			case SliderType.Range:
				if (isSet && (i >= _setOptions [0] && i <= _setOptions [1] || i >= _setOptions [1] && i <= _setOptions [0])) {
					isOptionSet = true;
				}
				break;
			}

			// Text || Abbreviation
			var text = string.Empty;
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
				if (isTextVertical) {
					y += legend_left_spaces_count;
				} else {
					x += legend_left_spaces_count;
				}
				//Move (x, y);
			}

			// Legend
			Driver?.SetAttribute (isOptionSet ? setAttr : normalAttr);
			foreach (var c in text.EnumerateRunes ()) {
				MoveAndAdd (x, y, c);
				//Driver.AddRune (c);
				if (isTextVertical) {
					y += 1;
				} else {
					x += 1;
				}
			}

			// Calculate End Spacing
			if (i == _options.Count () - 1) {
				// See Start Spacing explanation.
				var chars_right = text.Length / 2;
				legend_right_spaces_count = _config._endSpacing - chars_right;
			}

			// Option Right Spacing of Option
			Driver?.SetAttribute (spaceAttr);
			if (isTextVertical) {
				y += legend_right_spaces_count;
			} else {
				x += legend_right_spaces_count;
			}

			if (_config._sliderOrientation == Orientation.Horizontal && _config._legendsOrientation == Orientation.Vertical) {
				x += _config._innerSpacing + 1;
			} else if (_config._sliderOrientation == Orientation.Vertical && _config._legendsOrientation == Orientation.Horizontal) {
				y += _config._innerSpacing + 1;
			}
		}
	}
	#endregion

	#region Keys and Mouse
	// Mouse coordinates of current drag
	Point? _dragPosition;

	// Coordinates of where the "move cursor" is drawn (in OnDrawContent)
	Point? _moveRenderPosition;

	/// <inheritdoc/>
	public override bool MouseEvent (MouseEvent mouseEvent)
	{
		// Note(jmperricone): Maybe we click to focus the cursor, and on next click we set the option.
		//                    That will makes OptionFocused Event more relevant.
		// (tig: I don't think so. Maybe an option if someone really wants it, but for now that
		//       adss to much friction to UI.
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

			if (Orientation == Orientation.Horizontal) {
				var left = _config._startSpacing;
				var width = _options.Count + (_options.Count - 1) * _config._innerSpacing;
				var right = left + width - 1;
				var clampedX = Clamp (position.X, left, right);
				position = new Point (clampedX, 0);
			} else {
				var top = _config._startSpacing;
				var height = _options.Count + (_options.Count - 1) * _config._innerSpacing;
				var bottom = top + height - 1;
				var clampedY = Clamp (position.Y, top, bottom);
				position = new Point (0, clampedY);
			}
			return position;
		}

		SetFocus ();

		if (!_dragPosition.HasValue && mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed)) {

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
			if (Orientation == Orientation.Horizontal) {
				success = TryGetOptionByPosition (mouseEvent.X, 0, Math.Max (0, _config._innerSpacing / 2), out option);
			} else {
				success = TryGetOptionByPosition (0, mouseEvent.Y, Math.Max (0, _config._innerSpacing / 2), out option);
			}
			if (!_config._allowEmpty && success) {
				if (!OnOptionFocused (option, new SliderEventArgs<T> (GetSetOptionDictionary (), FocusedOption))) {
					SetFocusedOption ();
				}
			}

			SetNeedsDisplay ();
			return true;
		}

		if (_dragPosition.HasValue && mouseEvent.Flags.HasFlag (MouseFlags.Button1Released) || mouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked)) {

			// End Drag
			Application.UngrabMouse ();
			_dragPosition = null;
			_moveRenderPosition = null;

			// TODO: Add func to calc distance between options to use as the MouseClickXOptionThreshold
			var success = false;
			var option = 0;
			if (Orientation == Orientation.Horizontal) {
				success = TryGetOptionByPosition (mouseEvent.X, 0, Math.Max (0, _config._innerSpacing / 2), out option);
			} else {
				success = TryGetOptionByPosition (0, mouseEvent.Y, Math.Max (0, _config._innerSpacing / 2), out option);
			}
			if (success) {
				if (!OnOptionFocused (option, new SliderEventArgs<T> (GetSetOptionDictionary (), FocusedOption))) {
					SetFocusedOption ();
				}
			}

			SetNeedsDisplay ();
			return true;
		}
		return false;
	}

	void SetCommands ()
	{
		AddCommand (Command.Right, () => MovePlus ());
		AddCommand (Command.LineDown, () => MovePlus ());
		AddCommand (Command.Left, () => MoveMinus ());
		AddCommand (Command.LineUp, () => MoveMinus ());
		AddCommand (Command.LeftHome, () => MoveStart ());
		AddCommand (Command.RightEnd, () => MoveEnd ());
		AddCommand (Command.RightExtend, () => ExtendPlus ());
		AddCommand (Command.LeftExtend, () => ExtendMinus ());
		AddCommand (Command.Accept, () => Set ());

		SetKeyBindings ();
	}

	// This is called during initialization and anytime orientation changes
	void SetKeyBindings ()
	{
		if (_config._sliderOrientation == Orientation.Horizontal) {
			KeyBindings.Add (KeyCode.CursorRight, Command.Right);
			KeyBindings.Remove (KeyCode.CursorDown);
			KeyBindings.Add (KeyCode.CursorLeft, Command.Left);
			KeyBindings.Remove (KeyCode.CursorUp);

			KeyBindings.Add (KeyCode.CursorRight | KeyCode.CtrlMask, Command.RightExtend);
			KeyBindings.Remove (KeyCode.CursorDown | KeyCode.CtrlMask);
			KeyBindings.Add (KeyCode.CursorLeft | KeyCode.CtrlMask, Command.LeftExtend);
			KeyBindings.Remove (KeyCode.CursorUp | KeyCode.CtrlMask);
		} else {
			KeyBindings.Remove (KeyCode.CursorRight);
			KeyBindings.Add (KeyCode.CursorDown, Command.LineDown);
			KeyBindings.Remove (KeyCode.CursorLeft);
			KeyBindings.Add (KeyCode.CursorUp, Command.LineUp);

			KeyBindings.Remove (KeyCode.CursorRight | KeyCode.CtrlMask);
			KeyBindings.Add (KeyCode.CursorDown | KeyCode.CtrlMask, Command.RightExtend);
			KeyBindings.Remove (KeyCode.CursorLeft | KeyCode.CtrlMask);
			KeyBindings.Add (KeyCode.CursorUp | KeyCode.CtrlMask, Command.LeftExtend);

		}
		KeyBindings.Add (KeyCode.Home, Command.LeftHome);
		KeyBindings.Add (KeyCode.End, Command.RightEnd);
		KeyBindings.Add (KeyCode.Enter, Command.Accept);
		KeyBindings.Add (KeyCode.Space, Command.Accept);

	}

	Dictionary<int, SliderOption<T>> GetSetOptionDictionary () => _setOptions.ToDictionary (e => e, e => _options [e]);

	void SetFocusedOption ()
	{
		switch (_config._type) {
		case SliderType.Single:
		case SliderType.LeftRange:
		case SliderType.RightRange:

			if (_setOptions.Count == 1) {
				var prev = _setOptions [0];

				if (!_config._allowEmpty && prev == FocusedOption) {
					break;
				}

				_setOptions.Clear ();
				_options [FocusedOption].OnUnSet ();

				if (FocusedOption != prev) {
					_setOptions.Add (FocusedOption);
					_options [FocusedOption].OnSet ();
				}
			} else {
				_setOptions.Add (FocusedOption);
				_options [FocusedOption].OnSet ();
			}

			// Raise slider changed event.
			OnOptionsChanged ();

			break;
		case SliderType.Multiple:
			if (_setOptions.Contains (FocusedOption)) {
				if (!_config._allowEmpty && _setOptions.Count () == 1) {
					break;
				}
				_setOptions.Remove (FocusedOption);
				_options [FocusedOption].OnUnSet ();
			} else {
				_setOptions.Add (FocusedOption);
				_options [FocusedOption].OnSet ();
			}
			OnOptionsChanged ();
			break;

		case SliderType.Range:
			if (_config._rangeAllowSingle) {
				if (_setOptions.Count == 1) {
					var prev = _setOptions [0];

					if (!_config._allowEmpty && prev == FocusedOption) {
						break;
					}
					if (FocusedOption == prev) {
						// un-set
						_setOptions.Clear ();
						_options [FocusedOption].OnUnSet ();
					} else {
						_setOptions [0] = FocusedOption;
						_setOptions.Add (prev);
						_setOptions.Sort ();
						_options [FocusedOption].OnSet ();
					}
				} else if (_setOptions.Count == 0) {
					_setOptions.Add (FocusedOption);
					_options [FocusedOption].OnSet ();
				} else {
					// Extend/Shrink
					if (FocusedOption < _setOptions [0]) {
						// extend left
						_options [_setOptions [0]].OnUnSet ();
						_setOptions [0] = FocusedOption;
					} else if (FocusedOption > _setOptions [1]) {
						// extend right
						_options [_setOptions [1]].OnUnSet ();
						_setOptions [1] = FocusedOption;
					} else if (FocusedOption >= _setOptions [0] && FocusedOption <= _setOptions [1]) {
						if (FocusedOption < _lastFocusedOption) {
							// shrink to the left
							_options [_setOptions [1]].OnUnSet ();
							_setOptions [1] = FocusedOption;

						} else if (FocusedOption > _lastFocusedOption) {
							// shrink to the right
							_options [_setOptions [0]].OnUnSet ();
							_setOptions [0] = FocusedOption;
						}
						if (_setOptions.Count > 1 && _setOptions [0] == _setOptions [1]) {
							_setOptions.Clear ();
							_setOptions.Add (FocusedOption);
						}
					}
				}
			} else {
				if (_setOptions.Count == 1) {
					var prev = _setOptions [0];

					if (!_config._allowEmpty && prev == FocusedOption) {
						break;
					}
					_setOptions [0] = FocusedOption;
					_setOptions.Add (prev);
					_setOptions.Sort ();
					_options [FocusedOption].OnSet ();
				} else if (_setOptions.Count == 0) {
					_setOptions.Add (FocusedOption);
					_options [FocusedOption].OnSet ();
					var next = FocusedOption < _options.Count - 1 ? FocusedOption + 1 : FocusedOption - 1;
					_setOptions.Add (next);
					_options [next].OnSet ();
				} else {
					// Extend/Shrink
					if (FocusedOption < _setOptions [0]) {
						// extend left
						_options [_setOptions [0]].OnUnSet ();
						_setOptions [0] = FocusedOption;
					} else if (FocusedOption > _setOptions [1]) {
						// extend right
						_options [_setOptions [1]].OnUnSet ();
						_setOptions [1] = FocusedOption;
					} else if (FocusedOption >= _setOptions [0] && FocusedOption <= _setOptions [1] && _setOptions [1] - _setOptions [0] > 1) {
						if (FocusedOption < _lastFocusedOption) {
							// shrink to the left
							_options [_setOptions [1]].OnUnSet ();
							_setOptions [1] = FocusedOption;

						} else if (FocusedOption > _lastFocusedOption) {
							// shrink to the right
							_options [_setOptions [0]].OnUnSet ();
							_setOptions [0] = FocusedOption;
						}
					}
					//if (_setOptions.Count > 1 && _setOptions [0] == _setOptions [1]) {
					//	SetFocusedOption ();
					//}
				}
			}

			// Raise Slider Option Changed Event.
			OnOptionsChanged ();

			break;
		default:
			throw new ArgumentOutOfRangeException (_config._type.ToString ());
		}
	}

	internal bool ExtendPlus ()
	{
		var next = FocusedOption < _options.Count - 1 ? FocusedOption + 1 : FocusedOption;
		if (next != FocusedOption && !OnOptionFocused (next, new SliderEventArgs<T> (GetSetOptionDictionary (), FocusedOption))) {
			SetFocusedOption ();
		}
		return true;

		//// TODO: Support RangeMultiple
		//if (_setOptions.Contains (FocusedOption)) {
		//	var next = FocusedOption < _options.Count - 1 ? FocusedOption + 1 : FocusedOption;
		//	if (!_setOptions.Contains (next)) {
		//		if (_config._type == SliderType.Range) {
		//			if (_setOptions.Count == 1) {
		//				if (!OnOptionFocused (next, new SliderEventArgs<T> (GetSetOptionDictionary (), FocusedOption))) {
		//					_setOptions.Add (FocusedOption);
		//					_setOptions.Sort (); // Range Type
		//					OnOptionsChanged ();
		//				}
		//			} else if (_setOptions.Count == 2) {
		//				if (!OnOptionFocused (next, new SliderEventArgs<T> (GetSetOptionDictionary (), FocusedOption))) {
		//					_setOptions [1] = FocusedOption;
		//					_setOptions.Sort (); // Range Type
		//					OnOptionsChanged ();
		//				}
		//			}
		//		} else {
		//			_setOptions.Remove (FocusedOption);
		//			// Note(jmperricone): We are setting the option here, do we send the OptionFocused Event too ?


		//			if (!OnOptionFocused (next, new SliderEventArgs<T> (GetSetOptionDictionary (), FocusedOption))) {
		//				_setOptions.Add (FocusedOption);
		//				_setOptions.Sort (); // Range Type
		//				OnOptionsChanged ();
		//			}
		//		}
		//	} else {
		//		if (_config._type == SliderType.Range) {
		//			if (!OnOptionFocused (next, new SliderEventArgs<T> (GetSetOptionDictionary (), FocusedOption))) {
		//				_setOptions.Clear();
		//				_setOptions.Add (FocusedOption);
		//				OnOptionsChanged ();
		//			}
		//		} else if (/*_settingRange == true ||*/ !AllowEmpty) {
		//			SetFocusedOption ();
		//		}
		//	}
		//}
		//return true;
	}

	internal bool ExtendMinus ()
	{
		var prev = FocusedOption > 0 ? FocusedOption - 1 : FocusedOption;
		if (prev != FocusedOption && !OnOptionFocused (prev, new SliderEventArgs<T> (GetSetOptionDictionary (), FocusedOption))) {
			SetFocusedOption ();
		}
		return true;
	}

	internal bool Set ()
	{
		SetFocusedOption ();
		return true;
	}

	internal bool MovePlus ()
	{
		var cancelled = OnOptionFocused (FocusedOption + 1, new SliderEventArgs<T> (GetSetOptionDictionary (), FocusedOption));
		if (cancelled) {
			return false;
		}

		if (!AllowEmpty) {
			SetFocusedOption ();
		}
		return true;
	}

	internal bool MoveMinus ()
	{
		var cancelled = OnOptionFocused (FocusedOption - 1, new SliderEventArgs<T> (GetSetOptionDictionary (), FocusedOption));
		if (cancelled) {
			return false;
		}

		if (!AllowEmpty) {
			SetFocusedOption ();
		}
		return true;
	}

	internal bool MoveStart ()
	{
		if (OnOptionFocused (0, new SliderEventArgs<T> (GetSetOptionDictionary (), FocusedOption))) {
			return false;
		}

		if (!AllowEmpty) {
			SetFocusedOption ();
		}
		return true;
	}

	internal bool MoveEnd ()
	{
		if (OnOptionFocused (_options.Count - 1, new SliderEventArgs<T> (GetSetOptionDictionary (), FocusedOption))) {
			return false;
		}

		if (!AllowEmpty) {
			SetFocusedOption ();
		}
		return true;
	}
	#endregion
}