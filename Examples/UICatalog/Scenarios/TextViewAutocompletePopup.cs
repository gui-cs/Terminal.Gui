#nullable enable

using Terminal.Gui.Editor;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Editor Positions", "Shows five Editor controls in different positions with word-wrap toggle")]
[ScenarioCategory ("Editor")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Mouse and Keyboard")]
public class TextViewAutocompletePopup : Scenario
{
    private int _height = 10;
    private CheckBox? _miWrapCheckBox;
    private Shortcut? _siWrap;
    private Editor? _textViewBottomLeft;
    private Editor? _textViewBottomRight;
    private Editor? _textViewCentered;
    private Editor? _textViewTopLeft;
    private Editor? _textViewTopRight;

    private Window? _appWindow;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);

        using IApplication app = Application.Create ();
        app.Init ();

        _appWindow = new ()
        {
            BorderStyle = LineStyle.None
        };

        int width = 20;
        string text = " jamp jemp jimp jomp jump";

        // MenuBar
        MenuBar menu = new ();

        _textViewTopLeft = new ()
        {
            Y = Pos.Bottom (menu),
            Width = width,
            Height = _height,
            Text = text
        };
        _appWindow.Add (_textViewTopLeft);

        _textViewTopRight = new ()
        {
            X = Pos.AnchorEnd (width),
            Y = Pos.Bottom (menu),
            Width = width,
            Height = _height,
            Text = text
        };
        _appWindow.Add (_textViewTopRight);

        _textViewBottomLeft = new ()
        {
            Y = Pos.AnchorEnd (_height),
            Width = width,
            Height = _height,
            Text = text
        };
        _appWindow.Add (_textViewBottomLeft);

        _textViewBottomRight = new ()
        {
            X = Pos.AnchorEnd (width),
            Y = Pos.AnchorEnd (_height),
            Width = width,
            Height = _height,
            Text = text
        };
        _appWindow.Add (_textViewBottomRight);

        _textViewCentered = new ()
        {
            X = Pos.Center (),
            Y = Pos.Center (),
            Width = width,
            Height = _height,
            Text = text
        };
        _appWindow.Add (_textViewCentered);

        // Setup menu checkboxes
        _miWrapCheckBox = new ()
        {
            Title = "_Word Wrap",
            Value = _textViewTopLeft.WordWrap ? CheckState.Checked : CheckState.UnChecked
        };
        _miWrapCheckBox.ValueChanged += (_, _) => WordWrap ();

        menu.Add (
                  new MenuBarItem (
                                   Strings.menuFile,
                                   [
                                       new MenuItem
                                       {
                                           CommandView = _miWrapCheckBox
                                       },
                                       new MenuItem
                                       {
                                           Title = Strings.cmdQuit,
                                           Action = Quit
                                       }
                                   ]
                                  )
                 );

        // StatusBar
        _siWrap = new (Key.Empty, "", null);

        StatusBar statusBar = new (
                                   [
                                       new (
                                            Application.GetDefaultKey (Command.Quit),
                                            "Quit",
                                            () => Quit ()
                                           ),
                                       _siWrap
                                   ]
                                  );

        _appWindow.Add (menu, statusBar);

        _appWindow.SubViewLayout += Win_LayoutStarted;

        app.Run (_appWindow);
        _appWindow?.Dispose ();
    }

    private void Quit () { _appWindow?.RequestStop (); }

    private void SetWrapStatusText ()
    {
        if (_siWrap is not null && _miWrapCheckBox is not null)
        {
            _siWrap.Title = $"WordWrap: {_miWrapCheckBox.Value == CheckState.Checked}";
        }
    }

    private void Win_LayoutStarted (object? sender, LayoutEventArgs obj)
    {
        if (_textViewTopLeft is null || _miWrapCheckBox is null || _textViewBottomLeft is null || _textViewBottomRight is null)
        {
            return;
        }

        _miWrapCheckBox.Value = _textViewTopLeft.WordWrap ? CheckState.Checked : CheckState.UnChecked;
        SetWrapStatusText ();
    }

    private void WordWrap ()
    {
        if (_miWrapCheckBox is null
            || _textViewTopLeft is null
            || _textViewTopRight is null
            || _textViewBottomLeft is null
            || _textViewBottomRight is null
            || _textViewCentered is null)
        {
            return;
        }

        _textViewTopLeft.WordWrap = _miWrapCheckBox.Value == CheckState.Checked;
        _textViewTopRight.WordWrap = _miWrapCheckBox.Value == CheckState.Checked;
        _textViewBottomLeft.WordWrap = _miWrapCheckBox.Value == CheckState.Checked;
        _textViewBottomRight.WordWrap = _miWrapCheckBox.Value == CheckState.Checked;
        _textViewCentered.WordWrap = _miWrapCheckBox.Value == CheckState.Checked;
        _miWrapCheckBox.Value = _textViewTopLeft.WordWrap ? CheckState.Checked : CheckState.UnChecked;
        SetWrapStatusText ();
    }
}
