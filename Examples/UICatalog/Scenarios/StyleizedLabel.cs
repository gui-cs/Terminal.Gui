#nullable enable
using Microsoft.DotNet.PlatformAbstractions;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("StyleizedLabel", "Demos the new Scheme API")]
[ScenarioCategory ("Controls")]
public sealed class StyleizedLabel : Scenario
{
    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Window appWindow = new TestWindow
        {
            Title = GetQuitKeyAndName (),
            BorderStyle = LineStyle.None
        };

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }
}

public class TestWindow : Window
{
    private readonly Shortcut _shVersion;

    public TestWindow ()
    {
        BorderStyle = LineStyle.None;

        TextStyle [] allStyles = Enum.GetValues (typeof (TextStyle))
                                     .Cast<TextStyle> ()
                                     .ToArray ();

        View? previousView = null;

        foreach (TextStyle style in allStyles)
        {
            string styleName = Enum.GetName (typeof (TextStyle), style)!;

            var styleLabelWithScheme = new Label
            {
                X = previousView is null ? 0 : Pos.Right (previousView) + 1,
                Y = 1,
                Width = Dim.Auto (DimAutoStyle.Text),
                Height = 1,
                SchemeName = "Dialog",
                Text = styleName + "+SchemeName"
            };

            styleLabelWithScheme.SetScheme (
                                            styleLabelWithScheme.GetScheme () with
                                            {
                                                Normal = styleLabelWithScheme.GetScheme ().Normal with { Foreground = Color.Yellow, Style = style }
                                            });

            var styleLabel = new Label
            {
                X = previousView is null ? 0 : Pos.Right (previousView) + 1,
                Y = 0,
                Width = Dim.Auto (DimAutoStyle.Text),
                Height = 1,
                Text = styleName
            };
            styleLabel.SetScheme (styleLabel.GetScheme () with { Normal = styleLabel.GetScheme ().Normal with { Style = style } });

            Add (styleLabel, styleLabelWithScheme);
            previousView = styleLabelWithScheme;
        }

        // Create StatusBar
        StatusBar statusBar = new ()
        {
            Visible = true,
            AlignmentModes = AlignmentModes.IgnoreFirstOrLast,
            CanFocus = false
        };

        _shVersion = new ()
        {
            Title = "Version Info",
            CanFocus = false
        };
        _shVersion.SetScheme (_shVersion.GetScheme () with { Normal = _shVersion.GetScheme ().Normal with { Style = TextStyle.Italic } });
        statusBar.Add (_shVersion); // always add it as the last one
        Add (statusBar);

        Height = Dim.Fill ();
        Width = Dim.Fill ();

        // Need to manage Loaded event
        Loaded += LoadedHandler;
    }

    private void LoadedHandler (object? sender, EventArgs? args)
    {
        if (_shVersion is { })
        {
            _shVersion.Title = $"{RuntimeEnvironment.OperatingSystem} {RuntimeEnvironment.OperatingSystemVersion}, {Application.Driver.GetVersionInfo ()}";
        }
    }
}
