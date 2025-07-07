using System.Text;
using UnitTests;

// ReSharper disable HeuristicUnreachableCode

namespace Terminal.Gui.InputTests;

public class EscSeqUtilsTests
{
    private bool _actionStarted;
    private MouseFlags _arg1;
    private Point _arg2;
    private string _c1Control, _code, _terminating;
    private ConsoleKeyInfo [] _cki;
    private bool _isKeyMouse;
    private bool _isResponse;
    private ConsoleKey _key;
    private ConsoleModifiers _mod;
    private List<MouseFlags> _mouseFlags;
    private ConsoleKeyInfo _newConsoleKeyInfo;
    private Point _pos;
    private string [] _values;

    [Fact]
    [AutoInitShutdown]
    public void DecodeEscSeq_Multiple_Tests ()
    {
        // ESC
        _cki = new ConsoleKeyInfo [] { new ('\u001b', 0, false, false, false) };
        var expectedCki = new ConsoleKeyInfo ('\u001b', ConsoleKey.Escape, false, false, false);

        EscSeqUtils.DecodeEscSeq (
                                                     ref _newConsoleKeyInfo,
                                                     ref _key,
                                                     _cki,
                                                     ref _mod,
                                                     out _c1Control,
                                                     out _code,
                                                     out _values,
                                                     out _terminating,
                                                     out _isKeyMouse,
                                                     out _mouseFlags,
                                                     out _pos,
                                                     out _isResponse,
                                                     ProcessContinuousButtonPressed
                                                    );
        Assert.Empty (EscSeqRequests.Statuses);
        Assert.Equal (expectedCki, _newConsoleKeyInfo);
        Assert.Equal (ConsoleKey.Escape, _key);
        Assert.Equal (0, (int)_mod);
        Assert.Equal ("ESC", _c1Control);
        Assert.Null (_code);
        Assert.Null (_values);
        Assert.Null (_terminating);
        Assert.False (_isKeyMouse);
        Assert.Equal (new () { 0 }, _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.False (_isResponse);
        Assert.Equal (0, (int)_arg1);
        Assert.Equal (Point.Empty, _arg2);

        ClearAll ();
        _cki = new ConsoleKeyInfo [] { new ('\u001b', 0, false, false, false), new ('\u0012', 0, false, false, false) };
        expectedCki = new ('\u0012', ConsoleKey.R, false, true, true);

        EscSeqUtils.DecodeEscSeq (
                                                     ref _newConsoleKeyInfo,
                                                     ref _key,
                                                     _cki,
                                                     ref _mod,
                                                     out _c1Control,
                                                     out _code,
                                                     out _values,
                                                     out _terminating,
                                                     out _isKeyMouse,
                                                     out _mouseFlags,
                                                     out _pos,
                                                     out _isResponse,
                                                     ProcessContinuousButtonPressed
                                                    );
        Assert.Empty (EscSeqRequests.Statuses);
        Assert.Equal (expectedCki, _newConsoleKeyInfo);
        Assert.Equal (ConsoleKey.R, _key);
        Assert.Equal (ConsoleModifiers.Alt | ConsoleModifiers.Control, _mod);
        Assert.Equal ("ESC", _c1Control);
        Assert.Null (_code);
        Assert.Null (_values);
        Assert.Equal ("\u0012", _terminating);
        Assert.False (_isKeyMouse);
        Assert.Equal (new () { 0 }, _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.False (_isResponse);
        Assert.Equal (0, (int)_arg1);
        Assert.Equal (Point.Empty, _arg2);

        ClearAll ();
        _cki = new ConsoleKeyInfo [] { new ('\u001b', 0, false, false, false), new ('r', 0, false, false, false) };
        expectedCki = new ('r', ConsoleKey.R, false, true, false);

        EscSeqUtils.DecodeEscSeq (
                                                     ref _newConsoleKeyInfo,
                                                     ref _key,
                                                     _cki,
                                                     ref _mod,
                                                     out _c1Control,
                                                     out _code,
                                                     out _values,
                                                     out _terminating,
                                                     out _isKeyMouse,
                                                     out _mouseFlags,
                                                     out _pos,
                                                     out _isResponse,
                                                     ProcessContinuousButtonPressed
                                                    );
        Assert.Empty (EscSeqRequests.Statuses);
        Assert.Equal (expectedCki, _newConsoleKeyInfo);
        Assert.Equal (ConsoleKey.R, _key);
        Assert.Equal (ConsoleModifiers.Alt, _mod);
        Assert.Equal ("ESC", _c1Control);
        Assert.Null (_code);
        Assert.Null (_values);
        Assert.Equal ("r", _terminating);
        Assert.False (_isKeyMouse);
        Assert.Equal (new () { 0 }, _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.False (_isResponse);
        Assert.Equal (0, (int)_arg1);
        Assert.Equal (Point.Empty, _arg2);

        // SS3
        ClearAll ();

        _cki = new ConsoleKeyInfo []
        {
            new ('\u001b', 0, false, false, false), new ('O', 0, false, false, false), new ('R', 0, false, false, false)
        };
        expectedCki = new ('\0', ConsoleKey.F3, false, false, false);

        EscSeqUtils.DecodeEscSeq (
                                                     ref _newConsoleKeyInfo,
                                                     ref _key,
                                                     _cki,
                                                     ref _mod,
                                                     out _c1Control,
                                                     out _code,
                                                     out _values,
                                                     out _terminating,
                                                     out _isKeyMouse,
                                                     out _mouseFlags,
                                                     out _pos,
                                                     out _isResponse,
                                                     ProcessContinuousButtonPressed
                                                    );
        Assert.Empty (EscSeqRequests.Statuses);
        Assert.Equal (expectedCki, _newConsoleKeyInfo);
        Assert.Equal (ConsoleKey.F3, _key);
        Assert.Equal (0, (int)_mod);
        Assert.Equal ("SS3", _c1Control);
        Assert.Null (_code);
        Assert.Single (_values);
        Assert.Null (_values [0]);
        Assert.Equal ("R", _terminating);
        Assert.False (_isKeyMouse);
        Assert.Equal (new () { 0 }, _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.False (_isResponse);
        Assert.Equal (0, (int)_arg1);
        Assert.Equal (Point.Empty, _arg2);

        // CSI
        ClearAll ();

        _cki = new ConsoleKeyInfo []
        {
            new ('\u001b', 0, false, false, false),
            new ('[', 0, false, false, false),
            new ('1', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('2', 0, false, false, false),
            new ('R', 0, false, false, false)
        };
        expectedCki = new ('\0', ConsoleKey.F3, true, false, false);

        EscSeqUtils.DecodeEscSeq (
                                                     ref _newConsoleKeyInfo,
                                                     ref _key,
                                                     _cki,
                                                     ref _mod,
                                                     out _c1Control,
                                                     out _code,
                                                     out _values,
                                                     out _terminating,
                                                     out _isKeyMouse,
                                                     out _mouseFlags,
                                                     out _pos,
                                                     out _isResponse,
                                                     ProcessContinuousButtonPressed
                                                    );
        Assert.Empty (EscSeqRequests.Statuses);
        Assert.Equal (expectedCki, _newConsoleKeyInfo);
        Assert.Equal (ConsoleKey.F3, _key);
        Assert.Equal (ConsoleModifiers.Shift, _mod);
        Assert.Equal ("CSI", _c1Control);
        Assert.Null (_code);
        Assert.Equal (2, _values.Length);
        Assert.Equal ("1", _values [0]);
        Assert.Equal ("2", _values [^1]);
        Assert.Equal ("R", _terminating);
        Assert.False (_isKeyMouse);
        Assert.Equal (new () { 0 }, _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.False (_isResponse);
        Assert.Equal (0, (int)_arg1);
        Assert.Equal (Point.Empty, _arg2);

        ClearAll ();

        _cki = new ConsoleKeyInfo []
        {
            new ('\u001b', 0, false, false, false),
            new ('[', 0, false, false, false),
            new ('1', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('3', 0, false, false, false),
            new ('R', 0, false, false, false)
        };
        expectedCki = new ('\0', ConsoleKey.F3, false, true, false);

        EscSeqUtils.DecodeEscSeq (
                                                     ref _newConsoleKeyInfo,
                                                     ref _key,
                                                     _cki,
                                                     ref _mod,
                                                     out _c1Control,
                                                     out _code,
                                                     out _values,
                                                     out _terminating,
                                                     out _isKeyMouse,
                                                     out _mouseFlags,
                                                     out _pos,
                                                     out _isResponse,
                                                     ProcessContinuousButtonPressed
                                                    );
        Assert.Empty (EscSeqRequests.Statuses);
        Assert.Equal (expectedCki, _newConsoleKeyInfo);
        Assert.Equal (ConsoleKey.F3, _key);
        Assert.Equal (ConsoleModifiers.Alt, _mod);
        Assert.Equal ("CSI", _c1Control);
        Assert.Null (_code);
        Assert.Equal (2, _values.Length);
        Assert.Equal ("1", _values [0]);
        Assert.Equal ("3", _values [^1]);
        Assert.Equal ("R", _terminating);
        Assert.False (_isKeyMouse);
        Assert.Equal (new () { 0 }, _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.False (_isResponse);
        Assert.Equal (0, (int)_arg1);
        Assert.Equal (Point.Empty, _arg2);

        ClearAll ();

        _cki = new ConsoleKeyInfo []
        {
            new ('\u001b', 0, false, false, false),
            new ('[', 0, false, false, false),
            new ('1', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('4', 0, false, false, false),
            new ('R', 0, false, false, false)
        };
        expectedCki = new ('\0', ConsoleKey.F3, true, true, false);

        EscSeqUtils.DecodeEscSeq (
                                                     ref _newConsoleKeyInfo,
                                                     ref _key,
                                                     _cki,
                                                     ref _mod,
                                                     out _c1Control,
                                                     out _code,
                                                     out _values,
                                                     out _terminating,
                                                     out _isKeyMouse,
                                                     out _mouseFlags,
                                                     out _pos,
                                                     out _isResponse,
                                                     ProcessContinuousButtonPressed
                                                    );
        Assert.Empty (EscSeqRequests.Statuses);
        Assert.Equal (expectedCki, _newConsoleKeyInfo);
        Assert.Equal (ConsoleKey.F3, _key);
        Assert.Equal (ConsoleModifiers.Shift | ConsoleModifiers.Alt, _mod);
        Assert.Equal ("CSI", _c1Control);
        Assert.Null (_code);
        Assert.Equal (2, _values.Length);
        Assert.Equal ("1", _values [0]);
        Assert.Equal ("4", _values [^1]);
        Assert.Equal ("R", _terminating);
        Assert.False (_isKeyMouse);
        Assert.Equal (new () { 0 }, _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.False (_isResponse);
        Assert.Equal (0, (int)_arg1);
        Assert.Equal (Point.Empty, _arg2);

        ClearAll ();

        _cki = new ConsoleKeyInfo []
        {
            new ('\u001b', 0, false, false, false),
            new ('[', 0, false, false, false),
            new ('1', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('5', 0, false, false, false),
            new ('R', 0, false, false, false)
        };
        expectedCki = new ('\0', ConsoleKey.F3, false, false, true);

        EscSeqUtils.DecodeEscSeq (
                                                     ref _newConsoleKeyInfo,
                                                     ref _key,
                                                     _cki,
                                                     ref _mod,
                                                     out _c1Control,
                                                     out _code,
                                                     out _values,
                                                     out _terminating,
                                                     out _isKeyMouse,
                                                     out _mouseFlags,
                                                     out _pos,
                                                     out _isResponse,
                                                     ProcessContinuousButtonPressed
                                                    );
        Assert.Empty (EscSeqRequests.Statuses);
        Assert.Equal (expectedCki, _newConsoleKeyInfo);
        Assert.Equal (ConsoleKey.F3, _key);
        Assert.Equal (ConsoleModifiers.Control, _mod);
        Assert.Equal ("CSI", _c1Control);
        Assert.Null (_code);
        Assert.Equal (2, _values.Length);
        Assert.Equal ("1", _values [0]);
        Assert.Equal ("5", _values [^1]);
        Assert.Equal ("R", _terminating);
        Assert.False (_isKeyMouse);
        Assert.Equal (new () { 0 }, _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.False (_isResponse);
        Assert.Equal (0, (int)_arg1);
        Assert.Equal (Point.Empty, _arg2);

        ClearAll ();

        _cki = new ConsoleKeyInfo []
        {
            new ('\u001b', 0, false, false, false),
            new ('[', 0, false, false, false),
            new ('1', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('6', 0, false, false, false),
            new ('R', 0, false, false, false)
        };
        expectedCki = new ('\0', ConsoleKey.F3, true, false, true);

        EscSeqUtils.DecodeEscSeq (
                                                     ref _newConsoleKeyInfo,
                                                     ref _key,
                                                     _cki,
                                                     ref _mod,
                                                     out _c1Control,
                                                     out _code,
                                                     out _values,
                                                     out _terminating,
                                                     out _isKeyMouse,
                                                     out _mouseFlags,
                                                     out _pos,
                                                     out _isResponse,
                                                     ProcessContinuousButtonPressed
                                                    );
        Assert.Empty (EscSeqRequests.Statuses);
        Assert.Equal (expectedCki, _newConsoleKeyInfo);
        Assert.Equal (ConsoleKey.F3, _key);
        Assert.Equal (ConsoleModifiers.Shift | ConsoleModifiers.Control, _mod);
        Assert.Equal ("CSI", _c1Control);
        Assert.Null (_code);
        Assert.Equal (2, _values.Length);
        Assert.Equal ("1", _values [0]);
        Assert.Equal ("6", _values [^1]);
        Assert.Equal ("R", _terminating);
        Assert.False (_isKeyMouse);
        Assert.Equal (new () { 0 }, _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.False (_isResponse);
        Assert.Equal (0, (int)_arg1);
        Assert.Equal (Point.Empty, _arg2);

        ClearAll ();

        _cki = new ConsoleKeyInfo []
        {
            new ('\u001b', 0, false, false, false),
            new ('[', 0, false, false, false),
            new ('1', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('7', 0, false, false, false),
            new ('R', 0, false, false, false)
        };
        expectedCki = new ('\0', ConsoleKey.F3, false, true, true);

        EscSeqUtils.DecodeEscSeq (
                                                     ref _newConsoleKeyInfo,
                                                     ref _key,
                                                     _cki,
                                                     ref _mod,
                                                     out _c1Control,
                                                     out _code,
                                                     out _values,
                                                     out _terminating,
                                                     out _isKeyMouse,
                                                     out _mouseFlags,
                                                     out _pos,
                                                     out _isResponse,
                                                     ProcessContinuousButtonPressed
                                                    );
        Assert.Empty (EscSeqRequests.Statuses);
        Assert.Equal (expectedCki, _newConsoleKeyInfo);
        Assert.Equal (ConsoleKey.F3, _key);
        Assert.Equal (ConsoleModifiers.Alt | ConsoleModifiers.Control, _mod);
        Assert.Equal ("CSI", _c1Control);
        Assert.Null (_code);
        Assert.Equal (2, _values.Length);
        Assert.Equal ("1", _values [0]);
        Assert.Equal ("7", _values [^1]);
        Assert.Equal ("R", _terminating);
        Assert.False (_isKeyMouse);
        Assert.Equal (new () { 0 }, _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.False (_isResponse);
        Assert.Equal (0, (int)_arg1);
        Assert.Equal (Point.Empty, _arg2);

        ClearAll ();

        _cki = new ConsoleKeyInfo []
        {
            new ('\u001b', 0, false, false, false),
            new ('[', 0, false, false, false),
            new ('1', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('8', 0, false, false, false),
            new ('R', 0, false, false, false)
        };
        expectedCki = new ('\0', ConsoleKey.F3, true, true, true);

        EscSeqUtils.DecodeEscSeq (
                                                     ref _newConsoleKeyInfo,
                                                     ref _key,
                                                     _cki,
                                                     ref _mod,
                                                     out _c1Control,
                                                     out _code,
                                                     out _values,
                                                     out _terminating,
                                                     out _isKeyMouse,
                                                     out _mouseFlags,
                                                     out _pos,
                                                     out _isResponse,
                                                     ProcessContinuousButtonPressed
                                                    );
        Assert.Empty (EscSeqRequests.Statuses);
        Assert.Equal (expectedCki, _newConsoleKeyInfo);
        Assert.Equal (ConsoleKey.F3, _key);
        Assert.Equal (ConsoleModifiers.Shift | ConsoleModifiers.Alt | ConsoleModifiers.Control, _mod);
        Assert.Equal ("CSI", _c1Control);
        Assert.Null (_code);
        Assert.Equal (2, _values.Length);
        Assert.Equal ("1", _values [0]);
        Assert.Equal ("8", _values [^1]);
        Assert.Equal ("R", _terminating);
        Assert.False (_isKeyMouse);
        Assert.Equal (new () { 0 }, _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.False (_isResponse);
        Assert.Equal (0, (int)_arg1);
        Assert.Equal (Point.Empty, _arg2);

        ClearAll ();

        _cki = new ConsoleKeyInfo []
        {
            new ('\u001b', 0, false, false, false),
            new ('[', 0, false, false, false),
            new ('<', 0, false, false, false),
            new ('0', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('2', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('3', 0, false, false, false),
            new ('M', 0, false, false, false)
        };
        expectedCki = default (ConsoleKeyInfo);

        EscSeqUtils.DecodeEscSeq (
                                                     ref _newConsoleKeyInfo,
                                                     ref _key,
                                                     _cki,
                                                     ref _mod,
                                                     out _c1Control,
                                                     out _code,
                                                     out _values,
                                                     out _terminating,
                                                     out _isKeyMouse,
                                                     out _mouseFlags,
                                                     out _pos,
                                                     out _isResponse,
                                                     ProcessContinuousButtonPressed
                                                    );
        Assert.Empty (EscSeqRequests.Statuses);
        Assert.Equal (expectedCki, _newConsoleKeyInfo);
        Assert.Equal (0, (int)_key);
        Assert.Equal (0, (int)_mod);
        Assert.Equal ("CSI", _c1Control);
        Assert.Equal ("<", _code);
        Assert.Equal (3, _values.Length);
        Assert.Equal ("0", _values [0]);
        Assert.Equal ("2", _values [1]);
        Assert.Equal ("3", _values [^1]);
        Assert.Equal ("M", _terminating);
        Assert.True (_isKeyMouse);
        Assert.Equal (new () { MouseFlags.Button1Pressed }, _mouseFlags);
        Assert.Equal (new (1, 2), _pos);
        Assert.False (_isResponse);
        Assert.Equal (0, (int)_arg1);
        Assert.Equal (Point.Empty, _arg2);

        ClearAll ();

        _cki = new ConsoleKeyInfo []
        {
            new ('\u001b', 0, false, false, false),
            new ('[', 0, false, false, false),
            new ('<', 0, false, false, false),
            new ('0', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('2', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('3', 0, false, false, false),
            new ('m', 0, false, false, false)
        };
        expectedCki = default (ConsoleKeyInfo);

        EscSeqUtils.DecodeEscSeq (
                                                     ref _newConsoleKeyInfo,
                                                     ref _key,
                                                     _cki,
                                                     ref _mod,
                                                     out _c1Control,
                                                     out _code,
                                                     out _values,
                                                     out _terminating,
                                                     out _isKeyMouse,
                                                     out _mouseFlags,
                                                     out _pos,
                                                     out _isResponse,
                                                     ProcessContinuousButtonPressed
                                                    );
        Assert.Empty (EscSeqRequests.Statuses);
        Assert.Equal (expectedCki, _newConsoleKeyInfo);
        Assert.Equal (0, (int)_key);
        Assert.Equal (0, (int)_mod);
        Assert.Equal ("CSI", _c1Control);
        Assert.Equal ("<", _code);
        Assert.Equal (3, _values.Length);
        Assert.Equal ("0", _values [0]);
        Assert.Equal ("2", _values [1]);
        Assert.Equal ("3", _values [^1]);
        Assert.Equal ("m", _terminating);
        Assert.True (_isKeyMouse);
        Assert.Equal (2, _mouseFlags.Count);

        Assert.Equal (
                      new () { MouseFlags.Button1Released, MouseFlags.Button1Clicked },
                      _mouseFlags
                     );
        Assert.Equal (new (1, 2), _pos);
        Assert.False (_isResponse);
        Assert.Equal (0, (int)_arg1);
        Assert.Equal (Point.Empty, _arg2);

        ClearAll ();

        _cki = new ConsoleKeyInfo []
        {
            new ('\u001b', 0, false, false, false),
            new ('[', 0, false, false, false),
            new ('<', 0, false, false, false),
            new ('0', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('2', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('3', 0, false, false, false),
            new ('M', 0, false, false, false)
        };
        expectedCki = default (ConsoleKeyInfo);

        EscSeqUtils.DecodeEscSeq (
                                                     ref _newConsoleKeyInfo,
                                                     ref _key,
                                                     _cki,
                                                     ref _mod,
                                                     out _c1Control,
                                                     out _code,
                                                     out _values,
                                                     out _terminating,
                                                     out _isKeyMouse,
                                                     out _mouseFlags,
                                                     out _pos,
                                                     out _isResponse,
                                                     ProcessContinuousButtonPressed
                                                    );
        Assert.Empty (EscSeqRequests.Statuses);
        Assert.Equal (expectedCki, _newConsoleKeyInfo);
        Assert.Equal (0, (int)_key);
        Assert.Equal (0, (int)_mod);
        Assert.Equal ("CSI", _c1Control);
        Assert.Equal ("<", _code);
        Assert.Equal (3, _values.Length);
        Assert.Equal ("0", _values [0]);
        Assert.Equal ("2", _values [1]);
        Assert.Equal ("3", _values [^1]);
        Assert.Equal ("M", _terminating);
        Assert.True (_isKeyMouse);
        Assert.Equal (new () { MouseFlags.Button1DoubleClicked }, _mouseFlags);
        Assert.Equal (new (1, 2), _pos);
        Assert.False (_isResponse);

        ClearAll ();

        _cki = new ConsoleKeyInfo []
        {
            new ('\u001b', 0, false, false, false),
            new ('[', 0, false, false, false),
            new ('<', 0, false, false, false),
            new ('0', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('2', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('3', 0, false, false, false),
            new ('M', 0, false, false, false)
        };
        expectedCki = default (ConsoleKeyInfo);

        EscSeqUtils.DecodeEscSeq (
                                                     ref _newConsoleKeyInfo,
                                                     ref _key,
                                                     _cki,
                                                     ref _mod,
                                                     out _c1Control,
                                                     out _code,
                                                     out _values,
                                                     out _terminating,
                                                     out _isKeyMouse,
                                                     out _mouseFlags,
                                                     out _pos,
                                                     out _isResponse,
                                                     ProcessContinuousButtonPressed
                                                    );
        Assert.Empty (EscSeqRequests.Statuses);
        Assert.Equal (expectedCki, _newConsoleKeyInfo);
        Assert.Equal (0, (int)_key);
        Assert.Equal (0, (int)_mod);
        Assert.Equal ("CSI", _c1Control);
        Assert.Equal ("<", _code);
        Assert.Equal (3, _values.Length);
        Assert.Equal ("0", _values [0]);
        Assert.Equal ("2", _values [1]);
        Assert.Equal ("3", _values [^1]);
        Assert.Equal ("M", _terminating);
        Assert.True (_isKeyMouse);
        Assert.Equal (new () { MouseFlags.Button1TripleClicked }, _mouseFlags);
        Assert.Equal (new (1, 2), _pos);
        Assert.False (_isResponse);
        
        ClearAll ();

        _cki = new ConsoleKeyInfo []
        {
            new ('\u001b', 0, false, false, false),
            new ('[', 0, false, false, false),
            new ('<', 0, false, false, false),
            new ('0', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('2', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('3', 0, false, false, false),
            new ('M', 0, false, false, false)
        };
        expectedCki = default (ConsoleKeyInfo);

        EscSeqUtils.DecodeEscSeq (
                                                     ref _newConsoleKeyInfo,
                                                     ref _key,
                                                     _cki,
                                                     ref _mod,
                                                     out _c1Control,
                                                     out _code,
                                                     out _values,
                                                     out _terminating,
                                                     out _isKeyMouse,
                                                     out _mouseFlags,
                                                     out _pos,
                                                     out _isResponse,
                                                     ProcessContinuousButtonPressed
                                                    );
        Assert.Empty (EscSeqRequests.Statuses);
        Assert.Equal (expectedCki, _newConsoleKeyInfo);
        Assert.Equal (0, (int)_key);
        Assert.Equal (0, (int)_mod);
        Assert.Equal ("CSI", _c1Control);
        Assert.Equal ("<", _code);
        Assert.Equal (3, _values.Length);
        Assert.Equal ("0", _values [0]);
        Assert.Equal ("2", _values [1]);
        Assert.Equal ("3", _values [^1]);
        Assert.Equal ("M", _terminating);
        Assert.True (_isKeyMouse);
        Assert.Equal (new () { MouseFlags.Button1Pressed }, _mouseFlags);
        Assert.Equal (new (1, 2), _pos);
        Assert.False (_isResponse);

        Assert.Equal (MouseFlags.None, _arg1);
        Assert.Equal (new (0, 0), _arg2);

        ClearAll ();

        _cki = new ConsoleKeyInfo []
        {
            new ('\u001b', 0, false, false, false),
            new ('[', 0, false, false, false),
            new ('<', 0, false, false, false),
            new ('0', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('2', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('3', 0, false, false, false),
            new ('m', 0, false, false, false)
        };
        expectedCki = default (ConsoleKeyInfo);

        EscSeqUtils.DecodeEscSeq (
                                                     ref _newConsoleKeyInfo,
                                                     ref _key,
                                                     _cki,
                                                     ref _mod,
                                                     out _c1Control,
                                                     out _code,
                                                     out _values,
                                                     out _terminating,
                                                     out _isKeyMouse,
                                                     out _mouseFlags,
                                                     out _pos,
                                                     out _isResponse,
                                                     ProcessContinuousButtonPressed
                                                    );
        Assert.Empty (EscSeqRequests.Statuses);
        Assert.Equal (expectedCki, _newConsoleKeyInfo);
        Assert.Equal (0, (int)_key);
        Assert.Equal (0, (int)_mod);
        Assert.Equal ("CSI", _c1Control);
        Assert.Equal ("<", _code);
        Assert.Equal (3, _values.Length);
        Assert.Equal ("0", _values [0]);
        Assert.Equal ("2", _values [1]);
        Assert.Equal ("3", _values [^1]);
        Assert.Equal ("m", _terminating);
        Assert.True (_isKeyMouse);
        Assert.Equal (new () { MouseFlags.Button1Released }, _mouseFlags);
        Assert.Equal (new (1, 2), _pos);
        Assert.False (_isResponse);
        Assert.Equal (0, (int)_arg1);
        Assert.Equal (Point.Empty, _arg2);

        ClearAll ();

        Assert.Empty (EscSeqRequests.Statuses);
        EscSeqRequests.Clear ();
        EscSeqRequests.Add ("t");

        _cki = new ConsoleKeyInfo []
        {
            new ('\u001b', 0, false, false, false),
            new ('[', 0, false, false, false),
            new ('8', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('1', 0, false, false, false),
            new ('0', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('2', 0, false, false, false),
            new ('0', 0, false, false, false),
            new ('t', 0, false, false, false)
        };
        expectedCki = default (ConsoleKeyInfo);
        Assert.Single (EscSeqRequests.Statuses);
        Assert.Equal ("t", EscSeqRequests.Statuses.ToArray () [^1].Terminator);

        EscSeqUtils.DecodeEscSeq (
                                                     ref _newConsoleKeyInfo,
                                                     ref _key,
                                                     _cki,
                                                     ref _mod,
                                                     out _c1Control,
                                                     out _code,
                                                     out _values,
                                                     out _terminating,
                                                     out _isKeyMouse,
                                                     out _mouseFlags,
                                                     out _pos,
                                                     out _isResponse,
                                                     ProcessContinuousButtonPressed
                                                    );
        Assert.Empty (EscSeqRequests.Statuses);
        Assert.Equal (expectedCki, _newConsoleKeyInfo);
        Assert.Equal (0, (int)_key);
        Assert.Equal (0, (int)_mod);
        Assert.Equal ("CSI", _c1Control);
        Assert.Null (_code);
        Assert.Equal (3, _values.Length);
        Assert.Equal ("8", _values [0]);
        Assert.Equal ("10", _values [1]);
        Assert.Equal ("20", _values [^1]);
        Assert.Equal ("t", _terminating);
        Assert.False (_isKeyMouse);
        Assert.Equal (new () { 0 }, _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.True (_isResponse);
        Assert.Equal (0, (int)_arg1);
        Assert.Equal (Point.Empty, _arg2);

        ClearAll ();
    }

    [Theory]
    [InlineData ('A', ConsoleKey.A, true, true, false, "ESC", '\u001b', 'A')]
    [InlineData ('a', ConsoleKey.A, false, true, false, "ESC", '\u001b', 'a')]
    [InlineData ('\0', ConsoleKey.Spacebar, false, true, true, "ESC", '\u001b', '\0')]
    [InlineData (' ', ConsoleKey.Spacebar, true, true, false, "ESC", '\u001b', ' ')]
    [InlineData ('\n', ConsoleKey.Enter, false, true, true, "ESC", '\u001b', '\n')]
    [InlineData ('\r', ConsoleKey.Enter, true, true, false, "ESC", '\u001b', '\r')]
    public void DecodeEscSeq_More_Multiple_Tests (
        char keyChar,
        ConsoleKey consoleKey,
        bool shift,
        bool alt,
        bool control,
        string c1Control,
        params char [] kChars
    )
    {
        _cki = new ConsoleKeyInfo [kChars.Length];

        for (var i = 0; i < kChars.Length; i++)
        {
            char kChar = kChars [i];
            _cki [i] = new (kChar, 0, false, false, false);
        }

        var expectedCki = new ConsoleKeyInfo (keyChar, consoleKey, shift, alt, control);

        EscSeqUtils.DecodeEscSeq (
                                                     ref _newConsoleKeyInfo,
                                                     ref _key,
                                                     _cki,
                                                     ref _mod,
                                                     out _c1Control,
                                                     out _code,
                                                     out _values,
                                                     out _terminating,
                                                     out _isKeyMouse,
                                                     out _mouseFlags,
                                                     out _pos,
                                                     out _isResponse,
                                                     ProcessContinuousButtonPressed
                                                    );
        Assert.Empty (EscSeqRequests.Statuses);
        Assert.Equal (expectedCki, _newConsoleKeyInfo);
        Assert.Equal (consoleKey, _key);

        ConsoleModifiers mods = new ();

        if (shift)
        {
            mods = ConsoleModifiers.Shift;
        }

        if (alt)
        {
            mods |= ConsoleModifiers.Alt;
        }

        if (control)
        {
            mods |= ConsoleModifiers.Control;
        }

        Assert.Equal (mods, _mod);
        Assert.Equal (c1Control, _c1Control);
        Assert.Null (_code);
        Assert.Null (_values);
        Assert.Equal (keyChar.ToString (), _terminating);
        Assert.False (_isKeyMouse);
        Assert.Equal (new () { 0 }, _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.False (_isResponse);
        Assert.Equal (0, (int)_arg1);
        Assert.Equal (Point.Empty, _arg2);

        ClearAll ();
    }

    [Fact]
    public void DecodeEscSeq_IncompleteCKInfos ()
    {
        // This is simulated response from a CSI_ReportTerminalSizeInChars
        _cki =
        [
            new ('\u001b', 0, false, false, false),
            new ('[', 0, false, false, false),
            new ('8', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('1', 0, false, false, false),
        ];

        ConsoleKeyInfo expectedCki = default;

        Assert.Null (EscSeqUtils.IncompleteCkInfos);

        EscSeqUtils.DecodeEscSeq (
                                                     ref _newConsoleKeyInfo,
                                                     ref _key,
                                                     _cki,
                                                     ref _mod,
                                                     out _c1Control,
                                                     out _code,
                                                     out _values,
                                                     out _terminating,
                                                     out _isKeyMouse,
                                                     out _mouseFlags,
                                                     out _pos,
                                                     out _isResponse,
                                                     ProcessContinuousButtonPressed
                                                    );
        Assert.Empty (EscSeqRequests.Statuses);
        Assert.Equal (expectedCki, _newConsoleKeyInfo);
        Assert.Equal (ConsoleKey.None, _key);
        Assert.Equal (ConsoleModifiers.None, _mod);
        Assert.Equal ("CSI", _c1Control);
        Assert.Null (_code);
        Assert.Equal (2, _values.Length);
        Assert.Equal ("", _terminating);
        Assert.False (_isKeyMouse);
        Assert.Equal ([0], _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.False (_isResponse);
        Assert.Equal (0, (int)_arg1);
        Assert.Equal (Point.Empty, _arg2);
        Assert.NotNull (EscSeqUtils.IncompleteCkInfos);
        Assert.Equal (_cki, EscSeqUtils.IncompleteCkInfos);
        Assert.Contains (EscSeqUtils.ToString (EscSeqUtils.IncompleteCkInfos), EscSeqUtils.ToString (_cki));

        _cki = EscSeqUtils.InsertArray (
                                        EscSeqUtils.IncompleteCkInfos,
                                        [
                                            new ('0', 0, false, false, false),
                                            new (';', 0, false, false, false),
                                            new ('2', 0, false, false, false),
                                            new ('0', 0, false, false, false),
                                            new ('t', 0, false, false, false)
                                        ]);

        expectedCki = default;

        // Add a request to avoid assert failure in the DecodeEscSeq method
        EscSeqRequests.Add ("t");
        Assert.Single (EscSeqRequests.Statuses);

        EscSeqUtils.DecodeEscSeq (
                                                     ref _newConsoleKeyInfo,
                                                     ref _key,
                                                     _cki,
                                                     ref _mod,
                                                     out _c1Control,
                                                     out _code,
                                                     out _values,
                                                     out _terminating,
                                                     out _isKeyMouse,
                                                     out _mouseFlags,
                                                     out _pos,
                                                     out _isResponse,
                                                     ProcessContinuousButtonPressed
                                                    );
        Assert.Empty (EscSeqRequests.Statuses);
        Assert.Equal (expectedCki, _newConsoleKeyInfo);
        Assert.Equal (ConsoleKey.None, _key);

        Assert.Equal (ConsoleModifiers.None, _mod);
        Assert.Equal ("CSI", _c1Control);
        Assert.Null (_code);
        Assert.Equal (3, _values.Length);
        Assert.Equal ("t", _terminating);
        Assert.False (_isKeyMouse);
        Assert.Equal ([0], _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.False (EscSeqRequests.HasResponse ("t"));
        Assert.True (_isResponse);
        Assert.Equal (0, (int)_arg1);
        Assert.Equal (Point.Empty, _arg2);
        Assert.Null (EscSeqUtils.IncompleteCkInfos);
        Assert.NotEqual (_cki, EscSeqUtils.IncompleteCkInfos);

        ClearAll ();
    }

    [Theory]
    [InlineData ('\u001B', ConsoleKey.Escape, false, false, false)]
    [InlineData ('\r', ConsoleKey.Enter, false, false, false)]
    [InlineData ('1', ConsoleKey.D1, false, false, false)]
    [InlineData ('!', ConsoleKey.None, false, false, false)]
    [InlineData ('a', ConsoleKey.A, false, false, false)]
    [InlineData ('A', ConsoleKey.A, true, false, false)]
    [InlineData ('\u0001', ConsoleKey.A, false, false, true)]
    [InlineData ('\0', ConsoleKey.Spacebar, false, false, true)]
    [InlineData ('\n', ConsoleKey.Enter, false, false, true)]
    [InlineData ('\t', ConsoleKey.Tab, false, false, false)]
    public void DecodeEscSeq_Single_Tests (char keyChar, ConsoleKey consoleKey, bool shift, bool alt, bool control)
    {
        _cki = [new (keyChar, 0, false, false, false)];
        var expectedCki = new ConsoleKeyInfo (keyChar, consoleKey, shift, alt, control);

        EscSeqUtils.DecodeEscSeq (
                                                     ref _newConsoleKeyInfo,
                                                     ref _key,
                                                     _cki,
                                                     ref _mod,
                                                     out _c1Control,
                                                     out _code,
                                                     out _values,
                                                     out _terminating,
                                                     out _isKeyMouse,
                                                     out _mouseFlags,
                                                     out _pos,
                                                     out _isResponse,
                                                     ProcessContinuousButtonPressed
                                                    );
        Assert.Empty (EscSeqRequests.Statuses);
        Assert.Equal (expectedCki, _newConsoleKeyInfo);
        Assert.Equal (consoleKey, _key);

        ConsoleModifiers mods = new ();

        if (shift)
        {
            mods = ConsoleModifiers.Shift;
        }

        if (alt)
        {
            mods |= ConsoleModifiers.Alt;
        }

        if (control)
        {
            mods |= ConsoleModifiers.Control;
        }

        Assert.Equal (mods, _mod);

        if (keyChar == '\u001B')
        {
            Assert.Equal ("ESC", _c1Control);
        }
        else
        {
            Assert.Null (_c1Control);
        }

        Assert.Null (_code);
        Assert.Null (_values);
        Assert.Null (_terminating);
        Assert.False (_isKeyMouse);
        Assert.Equal (new () { 0 }, _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.False (_isResponse);
        Assert.Equal (0, (int)_arg1);
        Assert.Equal (Point.Empty, _arg2);

        ClearAll ();
    }

    [Fact]
    public void Defaults_Values ()
    {
        Assert.Equal ('\x1b', EscSeqUtils.KeyEsc);
        Assert.Equal ("\x1b[", EscSeqUtils.CSI);
        Assert.Equal ("\x1b[?1003h", EscSeqUtils.CSI_EnableAnyEventMouse);
        Assert.Equal ("\x1b[?1006h", EscSeqUtils.CSI_EnableSgrExtModeMouse);
        Assert.Equal ("\x1b[?1015h", EscSeqUtils.CSI_EnableUrxvtExtModeMouse);
        Assert.Equal ("\x1b[?1003l", EscSeqUtils.CSI_DisableAnyEventMouse);
        Assert.Equal ("\x1b[?1006l", EscSeqUtils.CSI_DisableSgrExtModeMouse);
        Assert.Equal ("\x1b[?1015l", EscSeqUtils.CSI_DisableUrxvtExtModeMouse);
        Assert.Equal ("\x1b[?1003h\x1b[?1015h\u001b[?1006h", EscSeqUtils.CSI_EnableMouseEvents);
        Assert.Equal ("\x1b[?1003l\x1b[?1015l\u001b[?1006l", EscSeqUtils.CSI_DisableMouseEvents);
    }

    [Fact]
    public void GetC1ControlChar_Tests ()
    {
        Assert.Equal ("IND", EscSeqUtils.GetC1ControlChar ('D'));
        Assert.Equal ("NEL", EscSeqUtils.GetC1ControlChar ('E'));
        Assert.Equal ("HTS", EscSeqUtils.GetC1ControlChar ('H'));
        Assert.Equal ("RI", EscSeqUtils.GetC1ControlChar ('M'));
        Assert.Equal ("SS2", EscSeqUtils.GetC1ControlChar ('N'));
        Assert.Equal ("SS3", EscSeqUtils.GetC1ControlChar ('O'));
        Assert.Equal ("DCS", EscSeqUtils.GetC1ControlChar ('P'));
        Assert.Equal ("SPA", EscSeqUtils.GetC1ControlChar ('V'));
        Assert.Equal ("EPA", EscSeqUtils.GetC1ControlChar ('W'));
        Assert.Equal ("SOS", EscSeqUtils.GetC1ControlChar ('X'));
        Assert.Equal ("DECID", EscSeqUtils.GetC1ControlChar ('Z'));
        Assert.Equal ("CSI", EscSeqUtils.GetC1ControlChar ('['));
        Assert.Equal ("ST", EscSeqUtils.GetC1ControlChar ('\\'));
        Assert.Equal ("OSC", EscSeqUtils.GetC1ControlChar (']'));
        Assert.Equal ("PM", EscSeqUtils.GetC1ControlChar ('^'));
        Assert.Equal ("APC", EscSeqUtils.GetC1ControlChar ('_'));
        Assert.Equal ("", EscSeqUtils.GetC1ControlChar ('\0'));
    }

    [Fact]
    public void GetConsoleInputKey_ConsoleKeyInfo ()
    {
        var cki = new ConsoleKeyInfo ('r', 0, false, false, false);
        var expectedCki = new ConsoleKeyInfo ('r', ConsoleKey.R, false, false, false);
        Assert.Equal (expectedCki, EscSeqUtils.MapConsoleKeyInfo (cki));

        cki = new ('r', 0, true, false, false);
        expectedCki = new ('r', ConsoleKey.R, true, false, false);
        Assert.Equal (expectedCki, EscSeqUtils.MapConsoleKeyInfo (cki));

        cki = new ('r', 0, false, true, false);
        expectedCki = new ('r', ConsoleKey.R, false, true, false);
        Assert.Equal (expectedCki, EscSeqUtils.MapConsoleKeyInfo (cki));

        cki = new ('r', 0, false, false, true);
        expectedCki = new ('r', ConsoleKey.R, false, false, true);
        Assert.Equal (expectedCki, EscSeqUtils.MapConsoleKeyInfo (cki));

        cki = new ('r', 0, true, true, false);
        expectedCki = new ('r', ConsoleKey.R, true, true, false);
        Assert.Equal (expectedCki, EscSeqUtils.MapConsoleKeyInfo (cki));

        cki = new ('r', 0, false, true, true);
        expectedCki = new ('r', ConsoleKey.R, false, true, true);
        Assert.Equal (expectedCki, EscSeqUtils.MapConsoleKeyInfo (cki));

        cki = new ('r', 0, true, true, true);
        expectedCki = new ('r', ConsoleKey.R, true, true, true);
        Assert.Equal (expectedCki, EscSeqUtils.MapConsoleKeyInfo (cki));

        cki = new ('\u0012', 0, false, false, false);
        expectedCki = new ('\u0012', ConsoleKey.R, false, false, true);
        Assert.Equal (expectedCki, EscSeqUtils.MapConsoleKeyInfo (cki));

        cki = new ('\0', (ConsoleKey)64, false, false, true);
        expectedCki = new ('\0', ConsoleKey.Spacebar, false, false, true);
        Assert.Equal (expectedCki, EscSeqUtils.MapConsoleKeyInfo (cki));

        cki = new ('\r', 0, false, false, false);
        expectedCki = new ('\r', ConsoleKey.Enter, false, false, false);
        Assert.Equal (expectedCki, EscSeqUtils.MapConsoleKeyInfo (cki));

        cki = new ('\u007f', 0, false, false, false);
        expectedCki = new ('\u007f', ConsoleKey.Backspace, false, false, false);
        Assert.Equal (expectedCki, EscSeqUtils.MapConsoleKeyInfo (cki));

        cki = new ('R', 0, false, false, false);
        expectedCki = new ('R', ConsoleKey.R, true, false, false);
        Assert.Equal (expectedCki, EscSeqUtils.MapConsoleKeyInfo (cki));
    }

    [Fact]
    public void GetConsoleKey_Tests ()
    {
        ConsoleModifiers mod = 0;
        char keyChar = '\0';
        Assert.Equal (ConsoleKey.UpArrow, EscSeqUtils.GetConsoleKey ('A', "", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.DownArrow, EscSeqUtils.GetConsoleKey ('B', "", ref mod, ref keyChar));
        Assert.Equal (_key = ConsoleKey.RightArrow, EscSeqUtils.GetConsoleKey ('C', "", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.LeftArrow, EscSeqUtils.GetConsoleKey ('D', "", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.End, EscSeqUtils.GetConsoleKey ('F', "", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.Home, EscSeqUtils.GetConsoleKey ('H', "", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.F1, EscSeqUtils.GetConsoleKey ('P', "", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.F2, EscSeqUtils.GetConsoleKey ('Q', "", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.F3, EscSeqUtils.GetConsoleKey ('R', "", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.F4, EscSeqUtils.GetConsoleKey ('S', "", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.Tab, EscSeqUtils.GetConsoleKey ('Z', "", ref mod, ref keyChar));
        Assert.Equal (ConsoleModifiers.Shift, mod);
        Assert.Equal (0, (int)EscSeqUtils.GetConsoleKey ('\0', "", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.Insert, EscSeqUtils.GetConsoleKey ('~', "2", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.Delete, EscSeqUtils.GetConsoleKey ('~', "3", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.PageUp, EscSeqUtils.GetConsoleKey ('~', "5", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.PageDown, EscSeqUtils.GetConsoleKey ('~', "6", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.F5, EscSeqUtils.GetConsoleKey ('~', "15", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.F6, EscSeqUtils.GetConsoleKey ('~', "17", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.F7, EscSeqUtils.GetConsoleKey ('~', "18", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.F8, EscSeqUtils.GetConsoleKey ('~', "19", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.F9, EscSeqUtils.GetConsoleKey ('~', "20", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.F10, EscSeqUtils.GetConsoleKey ('~', "21", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.F11, EscSeqUtils.GetConsoleKey ('~', "23", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.F12, EscSeqUtils.GetConsoleKey ('~', "24", ref mod, ref keyChar));
        Assert.Equal (0, (int)EscSeqUtils.GetConsoleKey ('~', "", ref mod, ref keyChar));
        // These terminators are used by macOS on a numeric keypad without keys modifiers
        Assert.Equal (ConsoleKey.Add, EscSeqUtils.GetConsoleKey ('l', null, ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.Subtract, EscSeqUtils.GetConsoleKey ('m', null, ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.Insert, EscSeqUtils.GetConsoleKey ('p', null, ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.End, EscSeqUtils.GetConsoleKey ('q', null, ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.DownArrow, EscSeqUtils.GetConsoleKey ('r', null, ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.PageDown, EscSeqUtils.GetConsoleKey ('s', null, ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.LeftArrow, EscSeqUtils.GetConsoleKey ('t', null, ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.Clear, EscSeqUtils.GetConsoleKey ('u', null, ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.RightArrow, EscSeqUtils.GetConsoleKey ('v', null, ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.Home, EscSeqUtils.GetConsoleKey ('w', null, ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.UpArrow, EscSeqUtils.GetConsoleKey ('x', null, ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.PageUp, EscSeqUtils.GetConsoleKey ('y', null, ref mod, ref keyChar));
    }

    [Fact]
    public void GetConsoleModifiers_Tests ()
    {
        Assert.Equal (ConsoleModifiers.Shift, EscSeqUtils.GetConsoleModifiers ("2"));
        Assert.Equal (ConsoleModifiers.Alt, EscSeqUtils.GetConsoleModifiers ("3"));
        Assert.Equal (ConsoleModifiers.Shift | ConsoleModifiers.Alt, EscSeqUtils.GetConsoleModifiers ("4"));
        Assert.Equal (ConsoleModifiers.Control, EscSeqUtils.GetConsoleModifiers ("5"));
        Assert.Equal (ConsoleModifiers.Shift | ConsoleModifiers.Control, EscSeqUtils.GetConsoleModifiers ("6"));
        Assert.Equal (ConsoleModifiers.Alt | ConsoleModifiers.Control, EscSeqUtils.GetConsoleModifiers ("7"));

        Assert.Equal (
                      ConsoleModifiers.Shift | ConsoleModifiers.Alt | ConsoleModifiers.Control,
                      EscSeqUtils.GetConsoleModifiers ("8")
                     );
        Assert.Equal (0, (int)EscSeqUtils.GetConsoleModifiers (""));
    }

    [Fact]
    public void GetEscapeResult_Multiple_Tests ()
    {
        char [] kChars = ['\u001b', '[', '5', ';', '1', '0', 'r'];
        (_c1Control, _code, _values, _terminating) = EscSeqUtils.GetEscapeResult (kChars);
        Assert.Equal ("CSI", _c1Control);
        Assert.Null (_code);
        Assert.Equal (2, _values.Length);
        Assert.Equal ("5", _values [0]);
        Assert.Equal ("10", _values [^1]);
        Assert.Equal ("r", _terminating);

        ClearAll ();
    }

    [Theory]
    [InlineData ('\u001B')]
    [InlineData (['\r'])]
    [InlineData (['1'])]
    [InlineData (['!'])]
    [InlineData (['a'])]
    [InlineData (['A'])]
    public void GetEscapeResult_Single_Tests (params char [] kChars)
    {
        (_c1Control, _code, _values, _terminating) = EscSeqUtils.GetEscapeResult (kChars);

        if (kChars [0] == '\u001B')
        {
            Assert.Equal ("ESC", _c1Control);
        }
        else
        {
            Assert.Null (_c1Control);
        }

        Assert.Null (_code);
        Assert.Null (_values);
        Assert.Null (_terminating);

        ClearAll ();
    }

    [Fact]
    public void GetKeyCharArray_Tests ()
    {
        ConsoleKeyInfo [] cki =
        {
            new ('\u001b', 0, false, false, false),
            new ('[', 0, false, false, false),
            new ('5', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('1', 0, false, false, false),
            new ('0', 0, false, false, false),
            new ('r', 0, false, false, false)
        };

        Assert.Equal (new [] { '\u001b', '[', '5', ';', '1', '0', 'r' }, EscSeqUtils.GetKeyCharArray (cki));
    }

    [Fact]
    [AutoInitShutdown]
    public void GetMouse_Tests ()
    {
        ConsoleKeyInfo [] cki =
        {
            new ('\u001b', 0, false, false, false),
            new ('[', 0, false, false, false),
            new ('<', 0, false, false, false),
            new ('0', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('2', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('3', 0, false, false, false),
            new ('M', 0, false, false, false)
        };
        EscSeqUtils.GetMouse (cki, out List<MouseFlags> mouseFlags, out Point pos, ProcessContinuousButtonPressed);
        Assert.Equal (new () { MouseFlags.Button1Pressed }, mouseFlags);
        Assert.Equal (new (1, 2), pos);

        cki = new ConsoleKeyInfo []
        {
            new ('\u001b', 0, false, false, false),
            new ('[', 0, false, false, false),
            new ('<', 0, false, false, false),
            new ('0', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('2', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('3', 0, false, false, false),
            new ('m', 0, false, false, false)
        };
        EscSeqUtils.GetMouse (cki, out mouseFlags, out pos, ProcessContinuousButtonPressed);
        Assert.Equal (2, mouseFlags.Count);

        Assert.Equal (
                      new () { MouseFlags.Button1Released, MouseFlags.Button1Clicked },
                      mouseFlags
                     );
        Assert.Equal (new (1, 2), pos);

        cki = new ConsoleKeyInfo []
        {
            new ('\u001b', 0, false, false, false),
            new ('[', 0, false, false, false),
            new ('<', 0, false, false, false),
            new ('0', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('2', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('3', 0, false, false, false),
            new ('M', 0, false, false, false)
        };
        EscSeqUtils.GetMouse (cki, out mouseFlags, out pos, ProcessContinuousButtonPressed);
        Assert.Equal (new () { MouseFlags.Button1DoubleClicked }, mouseFlags);
        Assert.Equal (new (1, 2), pos);

        cki = new ConsoleKeyInfo []
        {
            new ('\u001b', 0, false, false, false),
            new ('[', 0, false, false, false),
            new ('<', 0, false, false, false),
            new ('0', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('2', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('3', 0, false, false, false),
            new ('M', 0, false, false, false)
        };
        EscSeqUtils.GetMouse (cki, out mouseFlags, out pos, ProcessContinuousButtonPressed);
        Assert.Equal (new () { MouseFlags.Button1TripleClicked }, mouseFlags);
        Assert.Equal (new (1, 2), pos);

        cki = new ConsoleKeyInfo []
        {
            new ('\u001b', 0, false, false, false),
            new ('[', 0, false, false, false),
            new ('<', 0, false, false, false),
            new ('0', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('2', 0, false, false, false),
            new (';', 0, false, false, false),
            new ('3', 0, false, false, false),
            new ('m', 0, false, false, false)
        };
        EscSeqUtils.GetMouse (cki, out mouseFlags, out pos, ProcessContinuousButtonPressed);
        Assert.Equal (new () { MouseFlags.Button1Released }, mouseFlags);
        Assert.Equal (new (1, 2), pos);
    }

    [Fact]
    public void ResizeArray_ConsoleKeyInfo ()
    {
        ConsoleKeyInfo [] expectedCkInfos = null;
        var cki = new ConsoleKeyInfo ('\u001b', ConsoleKey.Escape, false, false, false);
        expectedCkInfos = EscSeqUtils.ResizeArray (cki, expectedCkInfos);
        Assert.Single (expectedCkInfos);
        Assert.Equal (cki, expectedCkInfos [0]);
    }

    [Theory]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC\b", "\b")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC\t", "\t")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC\n", "\n")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC\r", "\r")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOCe", "e")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOCV", "V")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC\u007f", "\u007f")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC ", " ")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC\\", "\\")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC|", "|")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC1", "1")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC!", "!")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC\"", "\"")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC@", "@")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC#", "#")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC£", "£")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC$", "$")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC§", "§")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC%", "%")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC€", "€")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC&", "&")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC/", "/")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC{", "{")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC(", "(")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC[", "[")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC)", ")")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC]", "]")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC=", "=")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC}", "}")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC'", "'")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC?", "?")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC«", "«")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC»", "»")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC+", "+")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC*", "*")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC¨", "¨")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC´", "´")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC`", "`")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOCç", "ç")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOCº", "º")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOCª", "ª")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC~", "~")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC^", "^")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC<", "<")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC>", ">")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC,", ",")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC;", ";")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC.", ".")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC:", ":")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC-", "-")]
    [InlineData ("\r[<35;50;1m[<35;49;1m[<35;47;1m[<35;46;1m[<35;45;2m[<35;44;2m[<35;43;3m[<35;42;3m[<35;41;4m[<35;40;5m[<35;39;6m[<35;49;1m[<35;48;2m[<0;33;6M[<0;33;6mOC_", "_")]
    public void SplitEscapeRawString_Multiple_Tests (string rawData, string expectedLast)
    {
        List<string> splitList = EscSeqUtils.SplitEscapeRawString (rawData);
        Assert.Equal (18, splitList.Count);
        Assert.Equal ("\r", splitList [0]);
        Assert.Equal ("\u001b[<35;50;1m", splitList [1]);
        Assert.Equal ("\u001b[<35;49;1m", splitList [2]);
        Assert.Equal ("\u001b[<35;47;1m", splitList [3]);
        Assert.Equal ("\u001b[<35;46;1m", splitList [4]);
        Assert.Equal ("\u001b[<35;45;2m", splitList [5]);
        Assert.Equal ("\u001b[<35;44;2m", splitList [6]);
        Assert.Equal ("\u001b[<35;43;3m", splitList [7]);
        Assert.Equal ("\u001b[<35;42;3m", splitList [8]);
        Assert.Equal ("\u001b[<35;41;4m", splitList [9]);
        Assert.Equal ("\u001b[<35;40;5m", splitList [10]);
        Assert.Equal ("\u001b[<35;39;6m", splitList [11]);
        Assert.Equal ("\u001b[<35;49;1m", splitList [12]);
        Assert.Equal ("\u001b[<35;48;2m", splitList [13]);
        Assert.Equal ("\u001b[<0;33;6M", splitList [14]);
        Assert.Equal ("\u001b[<0;33;6m", splitList [15]);
        Assert.Equal ("\u001bOC", splitList [16]);
        Assert.Equal (expectedLast, splitList [^1]);
    }

    [Theory]
    [InlineData ("[<35;50;1m")]
    [InlineData ("\r")]
    [InlineData ("1")]
    [InlineData ("!")]
    [InlineData ("a")]
    [InlineData ("A")]
    public void SplitEscapeRawString_Single_Tests (string rawData)
    {
        List<string> splitList = EscSeqUtils.SplitEscapeRawString (rawData);
        Assert.Single (splitList);
        Assert.Equal (rawData, splitList [0]);
    }

    [Theory]
    [InlineData (null, null, null, null)]
    [InlineData ("\u001b[8;1", null, null, "\u001b[8;1")]
    [InlineData (null, "\u001b[8;1", 5, "\u001b[8;1")]
    [InlineData ("\u001b[8;1", null, 5, "\u001b[8;1")]
    [InlineData ("\u001b[8;1", "0;20t", -1, "\u001b[8;10;20t")]
    [InlineData ("\u001b[8;1", "0;20t", 0, "\u001b[8;10;20t")]
    [InlineData ("0;20t", "\u001b[8;1", 5, "\u001b[8;10;20t")]
    [InlineData ("0;20t", "\u001b[8;1", 3, "\u001b[80;20t;1")]
    public void InsertArray_Tests (string toInsert, string current, int? index, string expected)
    {
        ConsoleKeyInfo [] toIns = EscSeqUtils.ToConsoleKeyInfoArray (toInsert);
        ConsoleKeyInfo [] cki = EscSeqUtils.ToConsoleKeyInfoArray (current);
        ConsoleKeyInfo [] result = EscSeqUtils.ToConsoleKeyInfoArray (expected);

        if (index is null)
        {
            cki = EscSeqUtils.InsertArray (toIns, cki);
        }
        else
        {
            cki = EscSeqUtils.InsertArray (toIns, cki, (int)index);
        }

        Assert.Equal (result, cki);
    }

    [Theory]
    [InlineData (0, 0, $"{EscSeqUtils.CSI}0;0H")]
    [InlineData (int.MaxValue, int.MaxValue, $"{EscSeqUtils.CSI}2147483647;2147483647H")]
    [InlineData (int.MinValue, int.MinValue, $"{EscSeqUtils.CSI}-2147483648;-2147483648H")]
    public void CSI_WriteCursorPosition_ReturnsCorrectEscSeq (int row, int col, string expected)
    {
        StringBuilder builder = new();
        using StringWriter writer = new(builder);

        EscSeqUtils.CSI_WriteCursorPosition (writer, row, col);

        string actual = builder.ToString();
        Assert.Equal (expected, actual);
    }

    private void ClearAll ()
    {
        EscSeqRequests.Clear ();
        _newConsoleKeyInfo = default (ConsoleKeyInfo);
        _key = default (ConsoleKey);
        _cki = default (ConsoleKeyInfo []);
        _mod = default (ConsoleModifiers);
        _c1Control = default (string);
        _code = default (string);
        _terminating = default (string);
        _values = default (string []);
        _isKeyMouse = default (bool);
        _isResponse = false;
        _mouseFlags = default (List<MouseFlags>);
        _pos = default (Point);
        _arg1 = default (MouseFlags);
        _arg2 = default (Point);
    }

    private void ProcessContinuousButtonPressed (MouseFlags arg1, Point arg2)
    {
        _arg1 = arg1;
        _arg2 = arg2;
        _actionStarted = true;
    }
}
