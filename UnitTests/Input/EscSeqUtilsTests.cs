namespace Terminal.Gui.InputTests;

public class EscSeqUtilsTests
{
    private bool _actionStarted;
    private MouseFlags _arg1;
    private Point _arg2;
    private string _c1Control, _code, _terminating;
    private ConsoleKeyInfo [] _cki;
    private EscSeqRequests _escSeqReqProc;
    private bool _isKeyMouse;
    private bool _isReq;
    private ConsoleKey _key;
    private ConsoleModifiers _mod;
    private List<MouseFlags> _mouseFlags;
    private ConsoleKeyInfo _newConsoleKeyInfo;
    private Point _pos;
    private string [] _values;

    [Fact]
    [AutoInitShutdown]
    public void DecodeEscSeq_Tests ()
    {
        // ESC
        _cki = new ConsoleKeyInfo [] { new ('\u001b', 0, false, false, false) };
        var expectedCki = new ConsoleKeyInfo ('\u001b', ConsoleKey.Escape, false, false, false);

        EscSeqUtils.DecodeEscSeq (
                                  _escSeqReqProc,
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
                                  out _isReq,
                                  ProcessContinuousButtonPressed
                                 );
        Assert.Null (_escSeqReqProc);
        Assert.Equal (expectedCki, _newConsoleKeyInfo);
        Assert.Equal (ConsoleKey.Escape, _key);
        Assert.Equal (0, (int)_mod);
        Assert.Equal ("ESC", _c1Control);
        Assert.Null (_code);
        Assert.Null (_values);
        Assert.Null (_terminating);
        Assert.False (_isKeyMouse);
        Assert.Equal (new() { 0 }, _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.False (_isReq);
        Assert.Equal (0, (int)_arg1);
        Assert.Equal (Point.Empty, _arg2);

        ClearAll ();
        _cki = new ConsoleKeyInfo [] { new ('\u001b', 0, false, false, false), new ('\u0012', 0, false, false, false) };
        expectedCki = new ('\u0012', ConsoleKey.R, false, true, true);

        EscSeqUtils.DecodeEscSeq (
                                  _escSeqReqProc,
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
                                  out _isReq,
                                  ProcessContinuousButtonPressed
                                 );
        Assert.Null (_escSeqReqProc);
        Assert.Equal (expectedCki, _newConsoleKeyInfo);
        Assert.Equal (ConsoleKey.R, _key);
        Assert.Equal (0, (int)_mod);
        Assert.Equal ("ESC", _c1Control);
        Assert.Null (_code);
        Assert.Null (_values);
        Assert.Equal ("\u0012", _terminating);
        Assert.False (_isKeyMouse);
        Assert.Equal (new() { 0 }, _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.False (_isReq);
        Assert.Equal (0, (int)_arg1);
        Assert.Equal (Point.Empty, _arg2);

        ClearAll ();
        _cki = new ConsoleKeyInfo [] { new ('\u001b', 0, false, false, false), new ('r', 0, false, false, false) };
        expectedCki = new ('R', ConsoleKey.R, false, true, false);

        EscSeqUtils.DecodeEscSeq (
                                  _escSeqReqProc,
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
                                  out _isReq,
                                  ProcessContinuousButtonPressed
                                 );
        Assert.Null (_escSeqReqProc);
        Assert.Equal (expectedCki, _newConsoleKeyInfo);
        Assert.Equal (ConsoleKey.R, _key);
        Assert.Equal (0, (int)_mod);
        Assert.Equal ("ESC", _c1Control);
        Assert.Null (_code);
        Assert.Null (_values);
        Assert.Equal ("r", _terminating);
        Assert.False (_isKeyMouse);
        Assert.Equal (new() { 0 }, _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.False (_isReq);
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
                                  _escSeqReqProc,
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
                                  out _isReq,
                                  ProcessContinuousButtonPressed
                                 );
        Assert.Null (_escSeqReqProc);
        Assert.Equal (expectedCki, _newConsoleKeyInfo);
        Assert.Equal (ConsoleKey.F3, _key);
        Assert.Equal (0, (int)_mod);
        Assert.Equal ("SS3", _c1Control);
        Assert.Null (_code);
        Assert.Single (_values);
        Assert.Null (_values [0]);
        Assert.Equal ("R", _terminating);
        Assert.False (_isKeyMouse);
        Assert.Equal (new() { 0 }, _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.False (_isReq);
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
                                  _escSeqReqProc,
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
                                  out _isReq,
                                  ProcessContinuousButtonPressed
                                 );
        Assert.Null (_escSeqReqProc);
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
        Assert.Equal (new() { 0 }, _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.False (_isReq);
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
                                  _escSeqReqProc,
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
                                  out _isReq,
                                  ProcessContinuousButtonPressed
                                 );
        Assert.Null (_escSeqReqProc);
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
        Assert.Equal (new() { 0 }, _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.False (_isReq);
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
                                  _escSeqReqProc,
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
                                  out _isReq,
                                  ProcessContinuousButtonPressed
                                 );
        Assert.Null (_escSeqReqProc);
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
        Assert.Equal (new() { 0 }, _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.False (_isReq);
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
                                  _escSeqReqProc,
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
                                  out _isReq,
                                  ProcessContinuousButtonPressed
                                 );
        Assert.Null (_escSeqReqProc);
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
        Assert.Equal (new() { 0 }, _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.False (_isReq);
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
                                  _escSeqReqProc,
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
                                  out _isReq,
                                  ProcessContinuousButtonPressed
                                 );
        Assert.Null (_escSeqReqProc);
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
        Assert.Equal (new() { 0 }, _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.False (_isReq);
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
                                  _escSeqReqProc,
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
                                  out _isReq,
                                  ProcessContinuousButtonPressed
                                 );
        Assert.Null (_escSeqReqProc);
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
        Assert.Equal (new() { 0 }, _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.False (_isReq);
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
                                  _escSeqReqProc,
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
                                  out _isReq,
                                  ProcessContinuousButtonPressed
                                 );
        Assert.Null (_escSeqReqProc);
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
        Assert.Equal (new() { 0 }, _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.False (_isReq);
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
                                  _escSeqReqProc,
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
                                  out _isReq,
                                  ProcessContinuousButtonPressed
                                 );
        Assert.Null (_escSeqReqProc);
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
        Assert.Equal (new() { MouseFlags.Button1Pressed }, _mouseFlags);
        Assert.Equal (new (1, 2), _pos);
        Assert.False (_isReq);
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
                                  _escSeqReqProc,
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
                                  out _isReq,
                                  ProcessContinuousButtonPressed
                                 );
        Assert.Null (_escSeqReqProc);
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
                      new() { MouseFlags.Button1Released, MouseFlags.Button1Clicked },
                      _mouseFlags
                     );
        Assert.Equal (new (1, 2), _pos);
        Assert.False (_isReq);
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
                                  _escSeqReqProc,
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
                                  out _isReq,
                                  ProcessContinuousButtonPressed
                                 );
        Assert.Null (_escSeqReqProc);
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
        Assert.Equal (new() { MouseFlags.Button1DoubleClicked }, _mouseFlags);
        Assert.Equal (new (1, 2), _pos);
        Assert.False (_isReq);

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
                                  _escSeqReqProc,
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
                                  out _isReq,
                                  ProcessContinuousButtonPressed
                                 );
        Assert.Null (_escSeqReqProc);
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
        Assert.Equal (new() { MouseFlags.Button1TripleClicked }, _mouseFlags);
        Assert.Equal (new (1, 2), _pos);
        Assert.False (_isReq);

        var view = new View { Width = Dim.Fill (), Height = Dim.Fill (), WantContinuousButtonPressed = true };
        var top = new Toplevel ();
        top.Add (view);
        Application.Begin (top);

        Application.RaiseMouseEvent (new() { Position = new (0, 0), Flags = 0 });

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
                                  _escSeqReqProc,
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
                                  out _isReq,
                                  ProcessContinuousButtonPressed
                                 );
        Assert.Null (_escSeqReqProc);
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
        Assert.Equal (new() { MouseFlags.Button1Pressed }, _mouseFlags);
        Assert.Equal (new (1, 2), _pos);
        Assert.False (_isReq);

        Application.Iteration += (s, a) =>
                                 {
                                     if (_actionStarted)
                                     {
                                         // set Application.WantContinuousButtonPressedView to null
                                         view.WantContinuousButtonPressed = false;

                                         Application.RaiseMouseEvent (new() { Position = new (0, 0), Flags = 0 });

                                         Application.RequestStop ();
                                     }
                                 };

        Application.Run (top);
        top.Dispose ();

        Assert.Null (Application.WantContinuousButtonPressedView);

        Assert.Equal (MouseFlags.Button1Pressed, _arg1);
        Assert.Equal (new (1, 2), _arg2);

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
                                  _escSeqReqProc,
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
                                  out _isReq,
                                  ProcessContinuousButtonPressed
                                 );
        Assert.Null (_escSeqReqProc);
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
        Assert.Equal (new() { MouseFlags.Button1Released }, _mouseFlags);
        Assert.Equal (new (1, 2), _pos);
        Assert.False (_isReq);
        Assert.Equal (0, (int)_arg1);
        Assert.Equal (Point.Empty, _arg2);

        ClearAll ();

        Assert.Null (_escSeqReqProc);
        _escSeqReqProc = new ();
        _escSeqReqProc.Add ("t");

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
        Assert.Single (_escSeqReqProc.Statuses);
        Assert.Equal ("t", _escSeqReqProc.Statuses [^1].Terminator);

        EscSeqUtils.DecodeEscSeq (
                                  _escSeqReqProc,
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
                                  out _isReq,
                                  ProcessContinuousButtonPressed
                                 );
        Assert.Empty (_escSeqReqProc.Statuses);
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
        Assert.Equal (new() { 0 }, _mouseFlags);
        Assert.Equal (Point.Empty, _pos);
        Assert.True (_isReq);
        Assert.Equal (0, (int)_arg1);
        Assert.Equal (Point.Empty, _arg2);
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
        var expectedCki = new ConsoleKeyInfo ('r', 0, false, false, false);
        Assert.Equal (expectedCki, EscSeqUtils.MapConsoleKeyInfo (cki));

        cki = new ('r', 0, true, false, false);
        expectedCki = new ('r', 0, true, false, false);
        Assert.Equal (expectedCki, EscSeqUtils.MapConsoleKeyInfo (cki));

        cki = new ('r', 0, false, true, false);
        expectedCki = new ('r', 0, false, true, false);
        Assert.Equal (expectedCki, EscSeqUtils.MapConsoleKeyInfo (cki));

        cki = new ('r', 0, false, false, true);
        expectedCki = new ('r', 0, false, false, true);
        Assert.Equal (expectedCki, EscSeqUtils.MapConsoleKeyInfo (cki));

        cki = new ('r', 0, true, true, false);
        expectedCki = new ('r', 0, true, true, false);
        Assert.Equal (expectedCki, EscSeqUtils.MapConsoleKeyInfo (cki));

        cki = new ('r', 0, false, true, true);
        expectedCki = new ('r', 0, false, true, true);
        Assert.Equal (expectedCki, EscSeqUtils.MapConsoleKeyInfo (cki));

        cki = new ('r', 0, true, true, true);
        expectedCki = new ('r', 0, true, true, true);
        Assert.Equal (expectedCki, EscSeqUtils.MapConsoleKeyInfo (cki));

        cki = new ('\u0012', 0, false, false, false);
        expectedCki = new ('R', ConsoleKey.R, false, false, true);
        Assert.Equal (expectedCki, EscSeqUtils.MapConsoleKeyInfo (cki));

        cki = new ('\0', (ConsoleKey)64, false, false, true);
        expectedCki = new (' ', ConsoleKey.Spacebar, false, false, true);
        Assert.Equal (expectedCki, EscSeqUtils.MapConsoleKeyInfo (cki));

        cki = new ('\r', 0, false, false, false);
        expectedCki = new ('\r', ConsoleKey.Enter, false, false, false);
        Assert.Equal (expectedCki, EscSeqUtils.MapConsoleKeyInfo (cki));

        cki = new ('\u007f', 0, false, false, false);
        expectedCki = new ('\u007f', ConsoleKey.Backspace, false, false, false);
        Assert.Equal (expectedCki, EscSeqUtils.MapConsoleKeyInfo (cki));

        cki = new ('R', 0, false, false, false);
        expectedCki = new ('R', 0, false, false, false);
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
        Assert.Equal (ConsoleKey.Add, EscSeqUtils.GetConsoleKey ('l', "", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.Subtract, EscSeqUtils.GetConsoleKey ('m', "", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.Insert, EscSeqUtils.GetConsoleKey ('p', "", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.End, EscSeqUtils.GetConsoleKey ('q', "", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.DownArrow, EscSeqUtils.GetConsoleKey ('r', "", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.PageDown, EscSeqUtils.GetConsoleKey ('s', "", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.LeftArrow, EscSeqUtils.GetConsoleKey ('t', "", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.Clear, EscSeqUtils.GetConsoleKey ('u', "", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.RightArrow, EscSeqUtils.GetConsoleKey ('v', "", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.Home, EscSeqUtils.GetConsoleKey ('w', "", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.UpArrow, EscSeqUtils.GetConsoleKey ('x', "", ref mod, ref keyChar));
        Assert.Equal (ConsoleKey.PageUp, EscSeqUtils.GetConsoleKey ('y', "", ref mod, ref keyChar));
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
    public void GetEscapeResult_Tests ()
    {
        char [] kChars = { '\u001b', '[', '5', ';', '1', '0', 'r' };
        (_c1Control, _code, _values, _terminating) = EscSeqUtils.GetEscapeResult (kChars);
        Assert.Equal ("CSI", _c1Control);
        Assert.Null (_code);
        Assert.Equal (2, _values.Length);
        Assert.Equal ("5", _values [0]);
        Assert.Equal ("10", _values [^1]);
        Assert.Equal ("r", _terminating);
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
        Assert.Equal (new() { MouseFlags.Button1Pressed }, mouseFlags);
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
                      new() { MouseFlags.Button1Released, MouseFlags.Button1Clicked },
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
        Assert.Equal (new() { MouseFlags.Button1DoubleClicked }, mouseFlags);
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
        Assert.Equal (new() { MouseFlags.Button1TripleClicked }, mouseFlags);
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
        Assert.Equal (new() { MouseFlags.Button1Released }, mouseFlags);
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

    private void ClearAll ()
    {
        _escSeqReqProc = default (EscSeqRequests);
        _newConsoleKeyInfo = default (ConsoleKeyInfo);
        _key = default (ConsoleKey);
        _cki = default (ConsoleKeyInfo []);
        _mod = default (ConsoleModifiers);
        _c1Control = default (string);
        _code = default (string);
        _terminating = default (string);
        _values = default (string []);
        _isKeyMouse = default (bool);
        _isReq = default (bool);
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
