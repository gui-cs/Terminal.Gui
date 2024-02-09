//------------------------------------------------------------------------------
// Windows Terminal supports Unicode and Emoji characters, but by default
// conhost shells (e.g., PowerShell and cmd.exe) do not. See
// <https://spectreconsole.net/best-practices>.
//------------------------------------------------------------------------------

namespace Terminal.Gui;

/// <summary>A <see cref="View"/> which displays (by default) a spinning line character.</summary>
/// <remarks>
///     By default animation only occurs when you call <see cref="SpinnerView.AdvanceAnimation()"/>. Use
///     <see cref="AutoSpin"/> to make the automate calls to <see cref="SpinnerView.AdvanceAnimation()"/>.
/// </remarks>
public class SpinnerView : View {
    /// <summary>Creates a new instance of the <see cref="SpinnerView"/> class.</summary>
    public SpinnerView () {
        Width = 1;
        Height = 1;
        _delay = DEFAULT_DELAY;
        _bounce = false;
        SpinReverse = false;
        SetStyle (DEFAULT_STYLE);

        AdvanceAnimation ();
    }

    private const int DEFAULT_DELAY = 130;
    private static readonly SpinnerStyle DEFAULT_STYLE = new SpinnerStyle.Line ();
    private bool _bounce = DEFAULT_STYLE.SpinBounce;
    private bool _bounceReverse;
    private DateTime _lastRender = DateTime.MinValue;
    private int _currentIdx;
    private int _delay = DEFAULT_STYLE.SpinDelay;
    private object _timeout;
    private SpinnerStyle _style = DEFAULT_STYLE;
    private string[] _sequence = DEFAULT_STYLE.Sequence;

    /// <summary>
    ///     Gets or sets whether spinning should occur automatically or be manually triggered (e.g. from a background
    ///     task).
    /// </summary>
    public bool AutoSpin {
        get => _timeout != null;
        set {
            if (value) {
                AddAutoSpinTimeout ();
            } else {
                RemoveAutoSpinTimeout ();
            }
        }
    }

    /// <summary>
    ///     Gets whether the current spinner style contains emoji or other special characters. Does not check Custom
    ///     sequences.
    /// </summary>
    public bool HasSpecialCharacters => _style.HasSpecialCharacters;

    /// <summary>Gets whether the current spinner style contains only ASCII characters.  Also checks Custom sequences.</summary>
    public bool IsAsciiOnly => GetIsAsciiOnly ();

    /// <summary>
    ///     Gets or sets whether spinner should go back and forth through the frames rather than going to the end and starting
    ///     again at the beginning.
    /// </summary>
    public bool SpinBounce {
        get => _bounce;
        set => SetBounce (value);
    }

    /// <summary>
    ///     Gets or sets whether spinner should go through the frames in reverse order. If SpinBounce is true, this sets the
    ///     starting order.
    /// </summary>
    public bool SpinReverse { get; set; }

    /// <summary>Gets or sets the number of milliseconds to wait between characters in the animation.</summary>
    /// <remarks>
    ///     This is the maximum speed the spinner will rotate at.  You still need to call
    ///     <see cref="SpinnerView.AdvanceAnimation()"/> or <see cref="SpinnerView.AutoSpin"/> to advance/start animation.
    /// </remarks>
    public int SpinDelay {
        get => _delay;
        set => SetDelay (value);
    }

    /// <summary>Gets or sets the Style used to animate the spinner.</summary>
    public SpinnerStyle Style {
        get => _style;
        set => SetStyle (value);
    }

    /// <summary>Gets or sets the animation frames used to animate the spinner.</summary>
    public string[] Sequence {
        get => _sequence;
        set => SetSequence (value);
    }

    /// <summary>
    ///     Advances the animation frame and notifies main loop that repainting needs to happen. Repeated calls are ignored
    ///     based on <see cref="SpinDelay"/>.
    /// </summary>
    /// <remarks>Ensure this method is called on the main UI thread e.g. via <see cref="Application.Invoke"/></remarks>
    public void AdvanceAnimation () {
        if (DateTime.Now - _lastRender > TimeSpan.FromMilliseconds (SpinDelay)) {
            if (Sequence is { } && Sequence.Length > 1) {
                var d = 1;
                if ((_bounceReverse && !SpinReverse) || (!_bounceReverse && SpinReverse)) {
                    d = -1;
                }

                _currentIdx += d;

                if (_currentIdx >= Sequence.Length) {
                    if (SpinBounce) {
                        if (SpinReverse) {
                            _bounceReverse = false;
                        } else {
                            _bounceReverse = true;
                        }

                        _currentIdx = Sequence.Length - 1;
                    } else {
                        _currentIdx = 0;
                    }
                }

                if (_currentIdx < 0) {
                    if (SpinBounce) {
                        if (SpinReverse) {
                            _bounceReverse = true;
                        } else {
                            _bounceReverse = false;
                        }

                        _currentIdx = 1;
                    } else {
                        _currentIdx = Sequence.Length - 1;
                    }
                }

                Text = "" + Sequence[_currentIdx]; //.EnumerateRunes;
            }

            _lastRender = DateTime.Now;
        }

        SetNeedsDisplay ();
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing) {
        RemoveAutoSpinTimeout ();

        base.Dispose (disposing);
    }

    private void AddAutoSpinTimeout () {
        if (_timeout != null) {
            return;
        }

        _timeout = Application.AddTimeout (
            TimeSpan.FromMilliseconds (SpinDelay),
            () => {
                Application.Invoke (AdvanceAnimation);

                return true;
            }
        );
    }

    private bool GetIsAsciiOnly () {
        if (HasSpecialCharacters) {
            return false;
        }

        if (_sequence is { } && _sequence.Length > 0) {
            foreach (string frame in _sequence) {
                foreach (char c in frame) {
                    if (!char.IsAscii (c)) {
                        return false;
                    }
                }
            }

            return true;
        }

        return true;
    }

    private int GetSpinnerWidth () {
        var max = 0;
        if (_sequence is { } && _sequence.Length > 0) {
            foreach (string frame in _sequence) {
                if (frame.Length > max) {
                    max = frame.Length;
                }
            }
        }

        return max;
    }

    private void RemoveAutoSpinTimeout () {
        if (_timeout != null) {
            Application.RemoveTimeout (_timeout);
            _timeout = null;
        }
    }

    private void SetBounce (bool bounce) { _bounce = bounce; }

    private void SetDelay (int delay) {
        if (delay > -1) {
            _delay = delay;
        }
    }

    private void SetSequence (string[] frames) {
        if (frames is { } && frames.Length > 0) {
            _style = new SpinnerStyle.Custom ();
            _sequence = frames;
            Width = GetSpinnerWidth ();
        }
    }

    private void SetStyle (SpinnerStyle style) {
        if (style is { }) {
            _style = style;
            _sequence = style.Sequence;
            _delay = style.SpinDelay;
            _bounce = style.SpinBounce;
            Width = GetSpinnerWidth ();
        }
    }
}
