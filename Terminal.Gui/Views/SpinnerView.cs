//------------------------------------------------------------------------------
// SpinnerStyles below are derived from <https://github.com/sindresorhus/cli-spinners/blob/master/spinners.json>
// MIT License
// Copyright (c) Sindre Sorhus <sindresorhus@gmail.com> (https://sindresorhus.com)
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// Windows Terminal supports Unicode and Emoji characters, but by default conhost shells (e.g., PowerShell and cmd.exe) do not. See <https://spectreconsole.net/best-practices>.
//------------------------------------------------------------------------------

using System;
using System.Text;

namespace Terminal.Gui {
	/// <summary>
	/// Spinner styles used in a <see cref="SpinnerView"/>.
	/// </summary>
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
	public enum SpinnerStyle {
		Custom,
		Dots,
		Dots2,
		Dots3,
		Dots4,
		Dots5,
		Dots6,
		Dots7,
		Dots8,
		Dots9,
		Dots10,
		Dots11,
		Dots12,
		Dots8Bit,
		Line,
		Line2,
		Pipe,
		SimpleDots,
		SimpleDotsScrolling,
		Star,
		Star2,
		Flip,
		Hamburger,
		GrowVertical,
		GrowHorizontal,
		Balloon,
		Balloon2,
		Noise,
		Bounce,
		BoxBounce,
		BoxBounce2,
		Triangle,
		Arc,
		Circle,
		SquareCorners,
		CircleQuarters,
		CircleHalves,
		Squish,
		Toggle,
		Toggle2,
		Toggle3,
		Toggle4,
		Toggle5,
		Toggle6,
		Toggle7,
		Toggle8,
		Toggle9,
		Toggle10,
		Toggle11,
		Toggle12,
		Toggle13,
		Arrow,
		Arrow2,
		Arrow3,
		BouncingBar,
		BouncingBall,
		Smiley,
		Monkey,
		Hearts,
		Clock,
		Earth,
		Material,
		Moon,
		Runner,
		Pong,
		Shark,
		Dqpb,
		Weather,
		Christmas,
		Grenade,
		Point,
		Layer,
		BetaWave,
		FingerDance,
		FistBump,
		SoccerHeader,
		MindBlown,
		Speaker,
		OrangePulse,
		BluePulse,
		OrangeBluePulse,
		TimeTravelClock,
		Aesthetic,
		Aesthetic2,
	}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

	/// <summary>
	/// A <see cref="View"/> based on <see cref="Label"/> which displays (by default) a
	/// spinning line character.
	/// </summary>
	/// <remarks>
	/// By default animation only occurs when you call <see cref="View.SetNeedsDisplay()"/>.
	/// Use <see cref="AutoSpin"/> to make the automate calls to <see cref="View.SetNeedsDisplay()"/>.
	/// </remarks>
	public class SpinnerView : Label {

		private const SpinnerStyle DEFAULT_STYLE = SpinnerStyle.Line;
		private const int DEFAULT_DELAY = 130;

		private bool _hasSpecial = false;
		private bool _bounceReverse = false;
		private SpinnerStyle _style;
		private int _currentIdx = 0;
		private DateTime _lastRender = DateTime.MinValue;
		private object _timeout;

		/// <summary>
		/// Gets or sets the frames used to animate the spinner.
		/// </summary>
		public string [] Frames { get; set; }

		/// <summary>
		/// Gets or sets the number of milliseconds to wait between characters
		/// in the spin.  Defaults to the SpinnerStyle's Interval value.
		/// </summary>
		/// <remarks>This is the maximum speed the spinner will rotate at.  You still need to
		/// call <see cref="View.SetNeedsDisplay()"/> or <see cref="SpinnerView.AutoSpin"/> to
		/// advance/start animation.</remarks>
		public int SpinDelayInMilliseconds { get; set; } = DEFAULT_DELAY;

		/// <summary>
		/// Gets or sets whether spinner should go through frames in reverse order.
		/// If SpinBounce is true, this sets the starting order.
		/// </summary>
		public bool SpinReverse { get; set; } = false;

		/// <summary>
		/// Gets or sets whether spinner should go back and forth through frames rather than
		/// going to the end and starting again at the beginning.
		/// </summary>
		public bool SpinBounce { get; set; } = false;

		/// <summary>
		/// Gets the size in characters of the spinner.
		/// </summary>
		public int SpinnerWidth { get => GetSpinnerWidth (); }

		/// <summary>
		/// Gets whether the current spinner style contains only ASCII characters.
		/// </summary>
		public bool IsAsciiOnly { get => GetIsAsciiOnly (); }

		/// <summary>
		/// Gets whether the current spinner style contains emoji or other special characters.
		/// </summary>
		public bool HasSpecialCharacters { get => GetHasSpecial (); }

		/// <summary>
		/// Creates a new instance of the <see cref="SpinnerView"/> class.
		/// </summary>
		public SpinnerView ()
		{
			SpinnerStyle = DEFAULT_STYLE;
			Width = GetSpinnerWidth ();
			Height = 1;
		}

		/// <summary>
		/// Creates a new instance of the <see cref="SpinnerView"/> class.
		/// </summary>
		public SpinnerView (SpinnerStyle style)
		{
			SpinnerStyle = style;
			Width = GetSpinnerWidth ();
			Height = 1;
		}

		/// <summary>
		/// Creates a new instance of the <see cref="SpinnerView"/> class with
		/// a custom spinner.
		/// </summary>
		public SpinnerView (string [] frames, int delay = DEFAULT_DELAY)
		{
			if (frames is not null &&
				frames.Length > 0) {
				_style = SpinnerStyle.Custom;
				Frames = frames;
				Width = GetSpinnerWidth ();
				SpinDelayInMilliseconds = delay;
			} else {
				Width = 1;
			}
			Height = 1;
		}

		private int GetSpinnerWidth ()
		{
			int max = 0;
			foreach (string frame in Frames) {
				if (frame.Length > max)
					max = frame.Length;
			}
			return max;
		}

		private bool GetIsAsciiOnly ()
		{
			foreach (string frame in Frames) {
				foreach (char c in frame) {
					if (!char.IsAscii (c))
						return false;
				}
			}
			return true;
		}

		private bool GetHasSpecial ()
		{
			if (_hasSpecial) return true;
			return false;
		}

		/// <inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			if (DateTime.Now - _lastRender > TimeSpan.FromMilliseconds (SpinDelayInMilliseconds)) {
				//_currentIdx = (_currentIdx + 1) % Frames.Length;
				if (Frames.Length > 1) {
					int d = 1;
					if ((_bounceReverse && !SpinReverse) || (!_bounceReverse && SpinReverse))
						d = -1;
					_currentIdx += d;

					if (_currentIdx >= Frames.Length) {
						if (SpinBounce) {
							if (SpinReverse)
								_bounceReverse = false;
							else
								_bounceReverse = true;
							_currentIdx = Frames.Length - 1;
						} else {
							_currentIdx = 0;
						}
					}
					if (_currentIdx < 0) {
						if (SpinBounce) {
							if (SpinReverse)
								_bounceReverse = true;
							else
								_bounceReverse = false;
							_currentIdx = 1;
						}
						else
							_currentIdx = Frames.Length - 1;
					}
					Text = "" + Frames [_currentIdx]; //.EnumerateRunes;
				}
				_lastRender = DateTime.Now;
			}

			base.Redraw (bounds);
		}

		/// <summary>
		/// Automates spinning
		/// </summary>
		public void AutoSpin ()
		{
			if (_timeout != null) {
				return;
			}

			_timeout = Application.MainLoop.AddTimeout (
				TimeSpan.FromMilliseconds (SpinDelayInMilliseconds), (m) => {
					Application.MainLoop.Invoke (this.SetNeedsDisplay);
					return true;
				});
		}

		/// <inheritdoc/>
		protected override void Dispose (bool disposing)
		{
			if (_timeout != null) {
				Application.MainLoop.RemoveTimeout (_timeout);
				_timeout = null;
			}

			base.Dispose (disposing);
		}

		/// <summary>
		/// Gets/Sets the spinner view style based on the <see cref="Terminal.Gui.SpinnerStyle"/>
		/// </summary>
		public SpinnerStyle SpinnerStyle {
			get => _style;
			set {
				_style = value;
				_bounceReverse = false;
				SpinDelayInMilliseconds = 80;
				SpinBounce = false;
				SpinReverse = false;
				_hasSpecial = false;
				switch (value) {
				case SpinnerStyle.Custom: // Placeholder when user has specified delay and frames manually
					SpinDelayInMilliseconds = 0;
					Frames = Array.Empty<string> ();
					break;
				case SpinnerStyle.Dots:
					Frames = new string []
					{
						"⠋",
						"⠙",
						"⠹",
						"⠸",
						"⠼",
						"⠴",
						"⠦",
						"⠧",
						"⠇",
						"⠏",
					};
					break;
				case SpinnerStyle.Dots2:
					Frames = new string []
					{
						"⣾",
						"⣽",
						"⣻",
						"⢿",
						"⡿",
						"⣟",
						"⣯",
						"⣷",
					};
					break;
				case SpinnerStyle.Dots3:
					Frames = new string []
					{
						"⠋",
						"⠙",
						"⠚",
						"⠞",
						"⠖",
						"⠦",
						"⠴",
						"⠲",
						"⠳",
						"⠓",
					};
					break;
				case SpinnerStyle.Dots4:
					SpinBounce = true;
					Frames = new string []
					{
						"⠄",
						"⠆",
						"⠇",
						"⠋",
						"⠙",
						"⠸",
						"⠰",
						"⠠",
					};
					break;
				case SpinnerStyle.Dots5:
					Frames = new string []
					{
						"⠋",
						"⠙",
						"⠚",
						"⠒",
						"⠂",
						"⠂",
						"⠒",
						"⠲",
						"⠴",
						"⠦",
						"⠖",
						"⠒",
						"⠐",
						"⠐",
						"⠒",
						"⠓",
						"⠋",
					};
					break;
				case SpinnerStyle.Dots6:
					SpinBounce = true;
					Frames = new string []
					{
						"⠁",
						"⠁",
						"⠉",
						"⠙",
						"⠚",
						"⠒",
						"⠂",
						"⠂",
						"⠒",
						"⠲",
						"⠴",
						"⠤",
						"⠄",
						"⠄",
					};
					break;
				case SpinnerStyle.Dots7:
					SpinBounce = true;
					Frames = new string []
					{
						"⠈",
						"⠈",
						"⠉",
						"⠋",
						"⠓",
						"⠒",
						"⠐",
						"⠐",
						"⠒",
						"⠖",
						"⠦",
						"⠤",
						"⠠",
						"⠠",
					};
					break;
				case SpinnerStyle.Dots8:
					Frames = new string []
					{
						"⠁",
						"⠁",
						"⠉",
						"⠙",
						"⠚",
						"⠒",
						"⠂",
						"⠂",
						"⠒",
						"⠲",
						"⠴",
						"⠤",
						"⠄",
						"⠄",
						"⠤",
						"⠠",
						"⠠",
						"⠤",
						"⠦",
						"⠖",
						"⠒",
						"⠐",
						"⠐",
						"⠒",
						"⠓",
						"⠋",
						"⠉",
						"⠈",
						"⠈",
					};
					break;
				case SpinnerStyle.Dots9:
					Frames = new string []
					{
						"⢹",
						"⢺",
						"⢼",
						"⣸",
						"⣇",
						"⡧",
						"⡗",
						"⡏",
					};
					break;
				case SpinnerStyle.Dots10:
					Frames = new string []
					{
						"⢄",
						"⢂",
						"⢁",
						"⡁",
						"⡈",
						"⡐",
						"⡠",
					};
					break;
				case SpinnerStyle.Dots11:
					SpinDelayInMilliseconds = 100;
					Frames = new string []
					{
						"⠁",
						"⠂",
						"⠄",
						"⡀",
						"⢀",
						"⠠",
						"⠐",
						"⠈",
					};
					break;
				case SpinnerStyle.Dots12:
					Frames = new string []
					{
						"⢀⠀",
						"⡀⠀",
						"⠄⠀",
						"⢂⠀",
						"⡂⠀",
						"⠅⠀",
						"⢃⠀",
						"⡃⠀",
						"⠍⠀",
						"⢋⠀",
						"⡋⠀",
						"⠍⠁",
						"⢋⠁",
						"⡋⠁",
						"⠍⠉",
						"⠋⠉",
						"⠋⠉",
						"⠉⠙",
						"⠉⠙",
						"⠉⠩",
						"⠈⢙",
						"⠈⡙",
						"⢈⠩",
						"⡀⢙",
						"⠄⡙",
						"⢂⠩",
						"⡂⢘",
						"⠅⡘",
						"⢃⠨",
						"⡃⢐",
						"⠍⡐",
						"⢋⠠",
						"⡋⢀",
						"⠍⡁",
						"⢋⠁",
						"⡋⠁",
						"⠍⠉",
						"⠋⠉",
						"⠋⠉",
						"⠉⠙",
						"⠉⠙",
						"⠉⠩",
						"⠈⢙",
						"⠈⡙",
						"⠈⠩",
						"⠀⢙",
						"⠀⡙",
						"⠀⠩",
						"⠀⢘",
						"⠀⡘",
						"⠀⠨",
						"⠀⢐",
						"⠀⡐",
						"⠀⠠",
						"⠀⢀",
						"⠀⡀",
					};
					break;
				case SpinnerStyle.Dots8Bit:
					Frames = new string []
					{
						"⠀",
						"⠁",
						"⠂",
						"⠃",
						"⠄",
						"⠅",
						"⠆",
						"⠇",
						"⡀",
						"⡁",
						"⡂",
						"⡃",
						"⡄",
						"⡅",
						"⡆",
						"⡇",
						"⠈",
						"⠉",
						"⠊",
						"⠋",
						"⠌",
						"⠍",
						"⠎",
						"⠏",
						"⡈",
						"⡉",
						"⡊",
						"⡋",
						"⡌",
						"⡍",
						"⡎",
						"⡏",
						"⠐",
						"⠑",
						"⠒",
						"⠓",
						"⠔",
						"⠕",
						"⠖",
						"⠗",
						"⡐",
						"⡑",
						"⡒",
						"⡓",
						"⡔",
						"⡕",
						"⡖",
						"⡗",
						"⠘",
						"⠙",
						"⠚",
						"⠛",
						"⠜",
						"⠝",
						"⠞",
						"⠟",
						"⡘",
						"⡙",
						"⡚",
						"⡛",
						"⡜",
						"⡝",
						"⡞",
						"⡟",
						"⠠",
						"⠡",
						"⠢",
						"⠣",
						"⠤",
						"⠥",
						"⠦",
						"⠧",
						"⡠",
						"⡡",
						"⡢",
						"⡣",
						"⡤",
						"⡥",
						"⡦",
						"⡧",
						"⠨",
						"⠩",
						"⠪",
						"⠫",
						"⠬",
						"⠭",
						"⠮",
						"⠯",
						"⡨",
						"⡩",
						"⡪",
						"⡫",
						"⡬",
						"⡭",
						"⡮",
						"⡯",
						"⠰",
						"⠱",
						"⠲",
						"⠳",
						"⠴",
						"⠵",
						"⠶",
						"⠷",
						"⡰",
						"⡱",
						"⡲",
						"⡳",
						"⡴",
						"⡵",
						"⡶",
						"⡷",
						"⠸",
						"⠹",
						"⠺",
						"⠻",
						"⠼",
						"⠽",
						"⠾",
						"⠿",
						"⡸",
						"⡹",
						"⡺",
						"⡻",
						"⡼",
						"⡽",
						"⡾",
						"⡿",
						"⢀",
						"⢁",
						"⢂",
						"⢃",
						"⢄",
						"⢅",
						"⢆",
						"⢇",
						"⣀",
						"⣁",
						"⣂",
						"⣃",
						"⣄",
						"⣅",
						"⣆",
						"⣇",
						"⢈",
						"⢉",
						"⢊",
						"⢋",
						"⢌",
						"⢍",
						"⢎",
						"⢏",
						"⣈",
						"⣉",
						"⣊",
						"⣋",
						"⣌",
						"⣍",
						"⣎",
						"⣏",
						"⢐",
						"⢑",
						"⢒",
						"⢓",
						"⢔",
						"⢕",
						"⢖",
						"⢗",
						"⣐",
						"⣑",
						"⣒",
						"⣓",
						"⣔",
						"⣕",
						"⣖",
						"⣗",
						"⢘",
						"⢙",
						"⢚",
						"⢛",
						"⢜",
						"⢝",
						"⢞",
						"⢟",
						"⣘",
						"⣙",
						"⣚",
						"⣛",
						"⣜",
						"⣝",
						"⣞",
						"⣟",
						"⢠",
						"⢡",
						"⢢",
						"⢣",
						"⢤",
						"⢥",
						"⢦",
						"⢧",
						"⣠",
						"⣡",
						"⣢",
						"⣣",
						"⣤",
						"⣥",
						"⣦",
						"⣧",
						"⢨",
						"⢩",
						"⢪",
						"⢫",
						"⢬",
						"⢭",
						"⢮",
						"⢯",
						"⣨",
						"⣩",
						"⣪",
						"⣫",
						"⣬",
						"⣭",
						"⣮",
						"⣯",
						"⢰",
						"⢱",
						"⢲",
						"⢳",
						"⢴",
						"⢵",
						"⢶",
						"⢷",
						"⣰",
						"⣱",
						"⣲",
						"⣳",
						"⣴",
						"⣵",
						"⣶",
						"⣷",
						"⢸",
						"⢹",
						"⢺",
						"⢻",
						"⢼",
						"⢽",
						"⢾",
						"⢿",
						"⣸",
						"⣹",
						"⣺",
						"⣻",
						"⣼",
						"⣽",
						"⣾",
						"⣿",
					};
					break;
				case SpinnerStyle.Line:
					SpinDelayInMilliseconds = 130;
					Frames = new string []
					{
						 "-",
						@"\",
						 "|",
						 "/",
					};
					break;
				case SpinnerStyle.Line2:
					SpinDelayInMilliseconds = 100;
					SpinBounce = true;
					Frames = new string []
					{
						"⠂",
						"-",
						"–",
						"—",
					};
					break;
				case SpinnerStyle.Pipe:
					SpinDelayInMilliseconds = 100;
					Frames = new string []
					{
						"┤",
						"┘",
						"┴",
						"└",
						"├",
						"┌",
						"┬",
						"┐",
					};
					break;
				case SpinnerStyle.SimpleDots:
					SpinDelayInMilliseconds = 400;
					Frames = new string []
					{
						".  ",
						".. ",
						"...",
						"   ",
					};
					break;
				case SpinnerStyle.SimpleDotsScrolling:
					SpinDelayInMilliseconds = 200;
					Frames = new string []
					{
						".  ",
						".. ",
						"...",
						" ..",
						"  .",
						"   ",
					};
					break;
				case SpinnerStyle.Star:
					SpinDelayInMilliseconds = 70;
					Frames = new string []
					{
						"✶",
						"✸",
						"✹",
						"✺",
						"✹",
						"✷",
					};
					break;
				case SpinnerStyle.Star2:
					Frames = new string []
					{
						"+",
						"x",
						"*",
					};
					break;
				case SpinnerStyle.Flip:
					SpinDelayInMilliseconds = 70;
					Frames = new string []
					{
						"_",
						"_",
						"_",
						"-",
						"`",
						"`",
						"'",
						"´",
						"-",
						"_",
						"_",
						"_",
					};
					break;
				case SpinnerStyle.Hamburger:
					SpinDelayInMilliseconds = 100;
					Frames = new string []
					{
						"☱",
						"☲",
						"☴",
					};
					break;
				case SpinnerStyle.GrowVertical:
					SpinDelayInMilliseconds = 120;
					SpinBounce = true;
					Frames = new string []
					{
						"▁",
						"▃",
						"▄",
						"▅",
						"▆",
						"▇",
					};
					break;
				case SpinnerStyle.GrowHorizontal:
					SpinDelayInMilliseconds = 120;
					SpinBounce = true;
					Frames = new string []
					{
						"▏",
						"▎",
						"▍",
						"▌",
						"▋",
						"▊",
						"▉",
					};
					break;
				case SpinnerStyle.Balloon:
					SpinDelayInMilliseconds = 140;
					Frames = new string []
					{
						" ",
						".",
						"o",
						"O",
						"@",
						"*",
						" ",
					};
					break;
				case SpinnerStyle.Balloon2:
					SpinDelayInMilliseconds = 120;
					SpinBounce = true;
					Frames = new string []
					{
						".",
						".",
						"o",
						"O",
						"°",
					};
					break;
				case SpinnerStyle.Noise:
					SpinDelayInMilliseconds = 100;
					Frames = new string []
					{
						"▓",
						"▒",
						"░",
					};
					break;
				case SpinnerStyle.Bounce:
					SpinDelayInMilliseconds = 120;
					SpinBounce = true;
					Frames = new string []
					{
						"⠁",
						"⠂",
						"⠄",
					};
					break;
				case SpinnerStyle.BoxBounce:
					SpinDelayInMilliseconds = 120;
					Frames = new string []
					{
						"▖",
						"▘",
						"▝",
						"▗",
					};
					break;
				case SpinnerStyle.BoxBounce2:
					SpinDelayInMilliseconds = 100;
					Frames = new string []
					{
						"▌",
						"▀",
						"▐",
						"▄",
					};
					break;
				case SpinnerStyle.Triangle:
					SpinDelayInMilliseconds = 50;
					Frames = new string []
					{
						"◢",
						"◣",
						"◤",
						"◥",
					};
					break;
				case SpinnerStyle.Arc:
					SpinDelayInMilliseconds = 100;
					Frames = new string []
					{
						"◜",
						"◠",
						"◝",
						"◞",
						"◡",
						"◟",
					};
					break;
				case SpinnerStyle.Circle:
					SpinDelayInMilliseconds = 120;
					Frames = new string []
					{
						"◡",
						"⊙",
						"◠",
					};
					break;
				case SpinnerStyle.SquareCorners:
					SpinDelayInMilliseconds = 180;
					Frames = new string []
					{
						"◰",
						"◳",
						"◲",
						"◱",
					};
					break;
				case SpinnerStyle.CircleQuarters:
					SpinDelayInMilliseconds = 120;
					Frames = new string []
					{
						"◴",
						"◷",
						"◶",
						"◵",
					};
					break;
				case SpinnerStyle.CircleHalves:
					SpinDelayInMilliseconds = 50;
					Frames = new string []
					{
						"◐",
						"◓",
						"◑",
						"◒",
					};
					break;
				case SpinnerStyle.Squish:
					SpinDelayInMilliseconds = 100;
					Frames = new string []
					{
						"╫",
						"╪",
					};
					break;
				case SpinnerStyle.Toggle:
					SpinDelayInMilliseconds = 250;
					Frames = new string []
					{
						"⊶",
						"⊷",
					};
					break;
				case SpinnerStyle.Toggle2:
					Frames = new string []
					{
						"▫",
						"▪",
					};
					break;
				case SpinnerStyle.Toggle3:
					SpinDelayInMilliseconds = 120;
					Frames = new string []
					{
						"□",
						"■",
					};
					break;
				case SpinnerStyle.Toggle4:
					SpinDelayInMilliseconds = 100;
					Frames = new string []
					{
						"■",
						"□",
						"▪",
						"▫",
					};
					break;
				case SpinnerStyle.Toggle5:
					SpinDelayInMilliseconds = 100;
					Frames = new string []
					{
						"▮",
						"▯",
					};
					break;
				case SpinnerStyle.Toggle6:
					SpinDelayInMilliseconds = 300;
					Frames = new string []
					{
						"ဝ",
						"၀",
					};
					break;
				case SpinnerStyle.Toggle7:
					Frames = new string []
					{
						"⦾",
						"⦿",
					};
					break;
				case SpinnerStyle.Toggle8:
					SpinDelayInMilliseconds = 100;
					Frames = new string []
					{
						"◍",
						"◌",
					};
					break;
				case SpinnerStyle.Toggle9:
					SpinDelayInMilliseconds = 100;
					Frames = new string []
					{
						"◉",
						"◎",
					};
					break;
				case SpinnerStyle.Toggle10:
					SpinDelayInMilliseconds = 100;
					Frames = new string []
					{
						"㊂",
						"㊀",
						"㊁",
					};
					break;
				case SpinnerStyle.Toggle11:
					SpinDelayInMilliseconds = 50;
					Frames = new string []
					{
						"⧇",
						"⧆",
					};
					break;
				case SpinnerStyle.Toggle12:
					SpinDelayInMilliseconds = 120;
					Frames = new string []
					{
						"☗",
						"☖",
					};
					break;
				case SpinnerStyle.Toggle13:
					Frames = new string []
					{
						"=",
						"*",
						"-",
					};
					break;
				case SpinnerStyle.Arrow:
					SpinDelayInMilliseconds = 100;
					Frames = new string []
					{
						"←",
						"↖",
						"↑",
						"↗",
						"→",
						"↘",
						"↓",
						"↙",
					};
					break;
				case SpinnerStyle.Arrow2:
					_hasSpecial = true;
					Frames = new string []
					{
						"⬆️ ",
						"↗️ ",
						"➡️ ",
						"↘️ ",
						"⬇️ ",
						"↙️ ",
						"⬅️ ",
						"↖️ ",
					};
					break;
				case SpinnerStyle.Arrow3:
					SpinDelayInMilliseconds = 120;
					Frames = new string []
					{
						"▹▹▹▹▹",
						"▸▹▹▹▹",
						"▹▸▹▹▹",
						"▹▹▸▹▹",
						"▹▹▹▸▹",
						"▹▹▹▹▸",
					};
					break;
				case SpinnerStyle.BouncingBar:
					SpinBounce = true;
					Frames = new string []
					{
						"[    ]",
						"[=   ]",
						"[==  ]",
						"[=== ]",
						"[ ===]",
						"[  ==]",
						"[   =]",
						"[    ]",
					};
					break;
				case SpinnerStyle.BouncingBall:
					SpinBounce = true;
					Frames = new string []
					{
						"(●     )",
						"( ●    )",
						"(  ●   )",
						"(   ●  )",
						"(    ● )",
						"(     ●)",
					};
					break;
				case SpinnerStyle.Smiley:
					SpinDelayInMilliseconds = 200;
					_hasSpecial = true;
					Frames = new string []
					{
						"😄 ",
						"😝 ",
					};
					break;
				case SpinnerStyle.Monkey:
					SpinDelayInMilliseconds = 300;
					_hasSpecial = true;
					Frames = new string []
					{
						"🙈 ",
						"🙈 ",
						"🙉 ",
						"🙊 ",
					};
					break;
				case SpinnerStyle.Hearts:
					SpinDelayInMilliseconds = 100;
					_hasSpecial = true;
					Frames = new string []
					{
						"💛 ",
						"💙 ",
						"💜 ",
						"💚 ",
						"❤️ ",
					};
					break;
				case SpinnerStyle.Clock:
					SpinDelayInMilliseconds = 100;
					_hasSpecial = true;
					Frames = new string []
					{
						"🕛 ",
						"🕐 ",
						"🕑 ",
						"🕒 ",
						"🕓 ",
						"🕔 ",
						"🕕 ",
						"🕖 ",
						"🕗 ",
						"🕘 ",
						"🕙 ",
						"🕚 ",
					};
					break;
				case SpinnerStyle.Earth:
					SpinDelayInMilliseconds = 180;
					_hasSpecial = true;
					Frames = new string []
					{
						"🌍 ",
						"🌎 ",
						"🌏 ",
					};
					break;
				case SpinnerStyle.Material:
					SpinDelayInMilliseconds = 17;
					Frames = new string []
					{
						"█▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁",
						"██▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁",
						"███▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁",
						"████▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁",
						"██████▁▁▁▁▁▁▁▁▁▁▁▁▁▁",
						"██████▁▁▁▁▁▁▁▁▁▁▁▁▁▁",
						"███████▁▁▁▁▁▁▁▁▁▁▁▁▁",
						"████████▁▁▁▁▁▁▁▁▁▁▁▁",
						"█████████▁▁▁▁▁▁▁▁▁▁▁",
						"█████████▁▁▁▁▁▁▁▁▁▁▁",
						"██████████▁▁▁▁▁▁▁▁▁▁",
						"███████████▁▁▁▁▁▁▁▁▁",
						"█████████████▁▁▁▁▁▁▁",
						"██████████████▁▁▁▁▁▁",
						"██████████████▁▁▁▁▁▁",
						"▁██████████████▁▁▁▁▁",
						"▁██████████████▁▁▁▁▁",
						"▁██████████████▁▁▁▁▁",
						"▁▁██████████████▁▁▁▁",
						"▁▁▁██████████████▁▁▁",
						"▁▁▁▁█████████████▁▁▁",
						"▁▁▁▁██████████████▁▁",
						"▁▁▁▁██████████████▁▁",
						"▁▁▁▁▁██████████████▁",
						"▁▁▁▁▁██████████████▁",
						"▁▁▁▁▁██████████████▁",
						"▁▁▁▁▁▁██████████████",
						"▁▁▁▁▁▁██████████████",
						"▁▁▁▁▁▁▁█████████████",
						"▁▁▁▁▁▁▁█████████████",
						"▁▁▁▁▁▁▁▁████████████",
						"▁▁▁▁▁▁▁▁████████████",
						"▁▁▁▁▁▁▁▁▁███████████",
						"▁▁▁▁▁▁▁▁▁███████████",
						"▁▁▁▁▁▁▁▁▁▁██████████",
						"▁▁▁▁▁▁▁▁▁▁██████████",
						"▁▁▁▁▁▁▁▁▁▁▁▁████████",
						"▁▁▁▁▁▁▁▁▁▁▁▁▁███████",
						"▁▁▁▁▁▁▁▁▁▁▁▁▁▁██████",
						"▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁█████",
						"▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁█████",
						"█▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁████",
						"██▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁███",
						"██▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁███",
						"███▁▁▁▁▁▁▁▁▁▁▁▁▁▁███",
						"████▁▁▁▁▁▁▁▁▁▁▁▁▁▁██",
						"█████▁▁▁▁▁▁▁▁▁▁▁▁▁▁█",
						"█████▁▁▁▁▁▁▁▁▁▁▁▁▁▁█",
						"██████▁▁▁▁▁▁▁▁▁▁▁▁▁█",
						"████████▁▁▁▁▁▁▁▁▁▁▁▁",
						"█████████▁▁▁▁▁▁▁▁▁▁▁",
						"█████████▁▁▁▁▁▁▁▁▁▁▁",
						"█████████▁▁▁▁▁▁▁▁▁▁▁",
						"█████████▁▁▁▁▁▁▁▁▁▁▁",
						"███████████▁▁▁▁▁▁▁▁▁",
						"████████████▁▁▁▁▁▁▁▁",
						"████████████▁▁▁▁▁▁▁▁",
						"██████████████▁▁▁▁▁▁",
						"██████████████▁▁▁▁▁▁",
						"▁██████████████▁▁▁▁▁",
						"▁██████████████▁▁▁▁▁",
						"▁▁▁█████████████▁▁▁▁",
						"▁▁▁▁▁████████████▁▁▁",
						"▁▁▁▁▁████████████▁▁▁",
						"▁▁▁▁▁▁███████████▁▁▁",
						"▁▁▁▁▁▁▁▁█████████▁▁▁",
						"▁▁▁▁▁▁▁▁█████████▁▁▁",
						"▁▁▁▁▁▁▁▁▁█████████▁▁",
						"▁▁▁▁▁▁▁▁▁█████████▁▁",
						"▁▁▁▁▁▁▁▁▁▁█████████▁",
						"▁▁▁▁▁▁▁▁▁▁▁████████▁",
						"▁▁▁▁▁▁▁▁▁▁▁████████▁",
						"▁▁▁▁▁▁▁▁▁▁▁▁███████▁",
						"▁▁▁▁▁▁▁▁▁▁▁▁███████▁",
						"▁▁▁▁▁▁▁▁▁▁▁▁▁███████",
						"▁▁▁▁▁▁▁▁▁▁▁▁▁███████",
						"▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁█████",
						"▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁████",
						"▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁████",
						"▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁████",
						"▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁███",
						"▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁███",
						"▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁██",
						"▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁██",
						"▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁██",
						"▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁█",
						"▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁█",
						"▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁█",
						"▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁",
						"▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁",
						"▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁",
						"▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁",
					};
					break;
				case SpinnerStyle.Moon:
					_hasSpecial = true;
					Frames = new string []
					{
						"🌑 ",
						"🌒 ",
						"🌓 ",
						"🌔 ",
						"🌕 ",
						"🌖 ",
						"🌗 ",
						"🌘 ",
					};
					break;
				case SpinnerStyle.Runner:
					_hasSpecial = true;
					SpinDelayInMilliseconds = 140;
					Frames = new string []
					{
						"🚶 ",
						"🏃 ",
					};
					break;
				case SpinnerStyle.Pong:
					SpinBounce = true;
					Frames = new string []
					{
						"▐⠂       ▌",
						"▐⠈       ▌",
						"▐ ⠂      ▌",
						"▐ ⠠      ▌",
						"▐  ⡀     ▌",
						"▐  ⠠     ▌",
						"▐   ⠂    ▌",
						"▐   ⠈    ▌",
						"▐    ⠂   ▌",
						"▐    ⠠   ▌",
						"▐     ⡀  ▌",
						"▐     ⠠  ▌",
						"▐      ⠂ ▌",
						"▐      ⠈ ▌",
						"▐       ⠂▌",
						"▐       ⠠▌",
						"▐       ⡀▌",
						"▐      ⠠ ▌",
						"▐      ⠂ ▌",
						"▐     ⠈  ▌",
						"▐     ⠂  ▌",
						"▐    ⠠   ▌",
						"▐    ⡀   ▌",
						"▐   ⠠    ▌",
						"▐   ⠂    ▌",
						"▐  ⠈     ▌",
						"▐  ⠂     ▌",
						"▐ ⠠      ▌",
						"▐ ⡀      ▌",
						"▐⠠       ▌",
					};
					break;
				case SpinnerStyle.Shark:
					SpinDelayInMilliseconds = 120;
					Frames = new string []
					{
						@"▐|\____________▌",
						@"▐_|\___________▌",
						@"▐__|\__________▌",
						@"▐___|\_________▌",
						@"▐____|\________▌",
						@"▐_____|\_______▌",
						@"▐______|\______▌",
						@"▐_______|\_____▌",
						@"▐________|\____▌",
						@"▐_________|\___▌",
						@"▐__________|\__▌",
						@"▐___________|\_▌",
						@"▐____________|\▌",
						 "▐____________/|▌",
						 "▐___________/|_▌",
						 "▐__________/|__▌",
						 "▐_________/|___▌",
						 "▐________/|____▌",
						 "▐_______/|_____▌",
						 "▐______/|______▌",
						 "▐_____/|_______▌",
						 "▐____/|________▌",
						 "▐___/|_________▌",
						 "▐__/|__________▌",
						 "▐_/|___________▌",
						 "▐/|____________▌",
					};
					break;
				case SpinnerStyle.Dqpb:
					SpinDelayInMilliseconds = 100;
					Frames = new string []
					{
						"d",
						"q",
						"p",
						"b",
					};
					break;
				case SpinnerStyle.Weather:
					SpinDelayInMilliseconds = 100;
					_hasSpecial = true;
					Frames = new string []
					{
						"☀️ ",
						"☀️ ",
						"☀️ ",
						"🌤 ",
						"⛅️ ",
						"🌥 ",
						"☁️ ",
						"🌧 ",
						"🌨 ",
						"🌧 ",
						"🌨 ",
						"🌧 ",
						"🌨 ",
						"⛈ ",
						"🌨 ",
						"🌧 ",
						"🌨 ",
						"☁️ ",
						"🌥 ",
						"⛅️ ",
						"🌤 ",
						"☀️ ",
						"☀️ ",
					};
					break;
				case SpinnerStyle.Christmas:
					SpinDelayInMilliseconds = 400;
					_hasSpecial = true;
					Frames = new string []
					{
						"🌲",
						"🎄",
					};
					break;
				case SpinnerStyle.Grenade:
					_hasSpecial = true;
					Frames = new string []
					{
						"،   ",
						"′   ",
						" ´ ",
						" ‾ ",
						"  ⸌",
						"  ⸊",
						"  |",
						"  ⁎",
						"  ⁕",
						" ෴ ",
						"  ⁓",
						"   ",
						"   ",
						"   ",
					};
					break;
				case SpinnerStyle.Point:
					SpinDelayInMilliseconds = 125;
					Frames = new string []
					{
						"∙∙∙",
						"●∙∙",
						"∙●∙",
						"∙∙●",
						"∙∙∙",
					};
					break;
				case SpinnerStyle.Layer:
					SpinDelayInMilliseconds = 150;
					Frames = new string []
					{
						"-",
						"=",
						"≡",
					};
					break;
				case SpinnerStyle.BetaWave:
					Frames = new string []
					{
						"ρββββββ",
						"βρβββββ",
						"ββρββββ",
						"βββρβββ",
						"ββββρββ",
						"βββββρβ",
						"ββββββρ",
					};
					break;
				case SpinnerStyle.FingerDance:
					SpinDelayInMilliseconds = 160;
					_hasSpecial = true;
					Frames = new string []
					{
						"🤘 ",
						"🤟 ",
						"🖖 ",
						"✋ ",
						"🤚 ",
						"👆 "
					};
					break;
				case SpinnerStyle.FistBump:
					_hasSpecial = true;
					Frames = new string []
					{
						"🤜\u3000\u3000\u3000\u3000🤛 ",
						"🤜\u3000\u3000\u3000\u3000🤛 ",
						"🤜\u3000\u3000\u3000\u3000🤛 ",
						"\u3000🤜\u3000\u3000🤛\u3000 ",
						"\u3000\u3000🤜🤛\u3000\u3000 ",
						"\u3000🤜✨🤛\u3000\u3000 ",
						"🤜\u3000✨\u3000🤛\u3000 "
					};
					break;
				case SpinnerStyle.SoccerHeader:
					SpinBounce = true;
					_hasSpecial = true;
					Frames = new string []
					{
						" 🧑⚽️       🧑 ",
						"🧑  ⚽️      🧑 ",
						"🧑   ⚽️     🧑 ",
						"🧑    ⚽️    🧑 ",
						"🧑     ⚽️   🧑 ",
						"🧑      ⚽️  🧑 ",
						"🧑       ⚽️🧑  ",
					};
					break;
				case SpinnerStyle.MindBlown:
					SpinDelayInMilliseconds = 160;
					_hasSpecial = true;
					Frames = new string []
					{
						"😐 ",
						"😐 ",
						"😮 ",
						"😮 ",
						"😦 ",
						"😦 ",
						"😧 ",
						"😧 ",
						"🤯 ",
						"💥 ",
						"✨ ",
						"\u3000 ",
						"\u3000 ",
						"\u3000 "
					};
					break;
				case SpinnerStyle.Speaker:
					SpinDelayInMilliseconds = 160;
					SpinBounce = true;
					_hasSpecial = true;
					Frames = new string []
					{
						"🔈 ",
						"🔉 ",
						"🔊 ",
					};
					break;
				case SpinnerStyle.OrangePulse:
					SpinDelayInMilliseconds = 100;
					_hasSpecial = true;
					Frames = new string []
					{
						"🔸 ",
						"🔶 ",
						"🟠 ",
						"🟠 ",
						"🔶 "
					};
					break;
				case SpinnerStyle.BluePulse:
					SpinDelayInMilliseconds = 100;
					_hasSpecial = true;
					Frames = new string []
					{
						"🔹 ",
						"🔷 ",
						"🔵 ",
						"🔵 ",
						"🔷 "
					};
					break;
				case SpinnerStyle.OrangeBluePulse:
					SpinDelayInMilliseconds = 100;
					_hasSpecial = true;
					Frames = new string []
					{
						"🔸 ",
						"🔶 ",
						"🟠 ",
						"🟠 ",
						"🔶 ",
						"🔹 ",
						"🔷 ",
						"🔵 ",
						"🔵 ",
						"🔷 "
					};
					break;
				case SpinnerStyle.TimeTravelClock:
					SpinDelayInMilliseconds = 100;
					_hasSpecial = true;
					Frames = new string []
					{
						"🕛 ",
						"🕚 ",
						"🕙 ",
						"🕘 ",
						"🕗 ",
						"🕖 ",
						"🕕 ",
						"🕔 ",
						"🕓 ",
						"🕒 ",
						"🕑 ",
						"🕐 "
					};
					break;
				case SpinnerStyle.Aesthetic:
					Frames = new string []
					{
						"▰▱▱▱▱▱▱",
						"▰▰▱▱▱▱▱",
						"▰▰▰▱▱▱▱",
						"▰▰▰▰▱▱▱",
						"▰▰▰▰▰▱▱",
						"▰▰▰▰▰▰▱",
						"▰▰▰▰▰▰▰",
						"▰▱▱▱▱▱▱",
					};
					break;
				case SpinnerStyle.Aesthetic2:
					Frames = new string []
					{
						"▰▱▱▱▱▱▱",
						"▰▰▱▱▱▱▱",
						"▰▰▰▱▱▱▱",
						"▰▰▰▰▱▱▱",
						"▰▰▰▰▰▱▱",
						"▰▰▰▰▰▰▱",
						"▰▰▰▰▰▰▰",
						"▱▰▰▰▰▰▰",
						"▱▱▰▰▰▰▰",
						"▱▱▱▰▰▰▰",
						"▱▱▱▱▰▰▰",
						"▱▱▱▱▱▰▰",
						"▱▱▱▱▱▱▰",
						"▱▱▱▱▱▱▱",
					};
					break;
				default:
					break;
				}
			}
		}
	}
}