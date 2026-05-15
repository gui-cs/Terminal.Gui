// Native AOT test application for Terminal.Gui.
//
// This is an AOT-safe equivalent of UICatalog's AllViewsTester. It statically instantiates
// every IDesignable view and calls EnableForDesign() to populate demo data — exercising the
// config-property deep-cloning, JSON serialization, and dictionary construction paths that
// are most sensitive to AOT trimming.
//
// Unlike AllViewsView (which uses Activator.CreateInstance and MakeGenericType), this file
// constructs every view explicitly, making it safe for native AOT compilation.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Terminal.Gui.Configuration;
using UICatalog.Scenarios;

namespace NativeAot;

public static class Program
{
    private static async Task Main (string [] args)
    {
#pragma warning disable IL2026, IL3050
        await RunAsync (args);
#pragma warning restore IL2026, IL3050
    }

    [RequiresUnreferencedCode ("Calls Terminal.Gui.Application.Init(IDriver, String)")]
    [RequiresDynamicCode ("Calls Terminal.Gui.Application.Init(IDriver, String)")]
    private static async Task RunAsync (string [] args)
    {
        bool smokeTest = args.Length > 0 && args [0] == "--smoke-test";

        ConfigurationManager.Enable (ConfigLocations.All);

        IApplication app = Application.Create ().Init ();

        #region Localization sanity check

        if (Equals (Thread.CurrentThread.CurrentUICulture, CultureInfo.InvariantCulture) && Application.SupportedCultures!.Count == 0)
        {
            Debug.Assert (Application.SupportedCultures.Count == 0);
        }
        else
        {
            Debug.Assert (Application.SupportedCultures!.Count > 0);
            Debug.Assert (Equals (CultureInfo.CurrentCulture, Thread.CurrentThread.CurrentUICulture));
        }

        #endregion

        if (smokeTest)
        {
            ExerciseDictionaryDeepCloning ();

            // CI smoke test: run the full app lifecycle with a timeout
            using CancellationTokenSource cts = new (TimeSpan.FromSeconds (5));
            await app.RunAsync<AotAllViewsWindow> (cts.Token);
            app.Dispose ();
            Console.WriteLine ("AOT smoke test passed: full app lifecycle completed successfully.");

            return;
        }

        app.Run<AotAllViewsWindow> ();
        app.Dispose ();
    }

    /// <summary>
    ///     To validate AOT compatibility of dictionary deep cloning, call DeepClone on key dictionaries.
    /// </summary>
    private static void ExerciseDictionaryDeepCloning ()
    {
        _ = DeepCloner.DeepClone (Color.Colors16);
        _ = DeepCloner.DeepClone (Application.DefaultKeyBindings);
        _ = DeepCloner.DeepClone (View.DefaultKeyBindings);
        _ = DeepCloner.DeepClone (View.ViewKeyBindings);
        _ = DeepCloner.DeepClone (ThemeManager.Themes);
        _ = DeepCloner.DeepClone (SchemeManager.Schemes);
    }
}

/// <summary>
///     AOT-safe mini AllViewsTester. Statically constructs every <see cref="IDesignable"/> view
///     and calls <see cref="IDesignable.EnableForDesign()"/> to populate demo data, then displays
///     them in a scrollable list of titled frames.
/// </summary>
public sealed class AotAllViewsWindow : Runnable
{
    private const int MAX_HEIGHT = 12;

    public AotAllViewsWindow ()
    {
        Title = $"AOT All Views ({Application.GetDefaultKey (Command.Quit)} to quit)";

        // ── MenuBar (config host: MenuBar, Menu) ─────────────────────
        MenuBar menuBar = new ()
        {
            Menus =
            [
                new MenuBarItem ("_File", [new MenuItem ("_Quit", "", () => App?.RequestStop ())]),
                new MenuBarItem ("_Help", [new MenuItem ("_About", "", () => MessageBox.Query (App!, "About", "AOT All Views Tester", "OK"))])
            ]
        };

        // ── StatusBar (config host: StatusBar) ───────────────────────
        StatusBar statusBar = new ();

        // ── ViewPropertiesEditor (linked from UICatalog — no reflection, AOT-safe) ──
        ViewPropertiesEditor propsEditor = new ()
        {
            Title = "Properties",
            Width = Dim.Percent (30),
            Height = Dim.Fill (statusBar),
            X = Pos.AnchorEnd (),
            AutoSelectViewToEdit = true,
            AutoSelectAdornments = false,
            BorderStyle = LineStyle.Dotted
        };

        // ── Scrollable container for all views ───────────────────────
        View container = new () { Y = Pos.Bottom (menuBar), Width = Dim.Fill (propsEditor), Height = Dim.Fill (statusBar), CanFocus = true };

        container.ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;

        container.ViewportChanged += (sender, _) =>
                                     {
                                         if (sender is View sendingView)
                                         {
                                             sendingView.SetContentHeight (sendingView.GetHeightRequiredForSubViews ());
                                         }
                                     };

        // Scope auto-select to views within the container
        propsEditor.AutoSelectSuperView = container;

        View? previous = null;

        foreach ((string name, View view) in CreateAllViews ())
        {
            FrameView frame = new ()
            {
                CanFocus = true,
                Title = name,
                Y = previous is { } ? Pos.Bottom (previous) : 0,
                Width = Dim.Fill (),
                Height = Dim.Auto (DimAutoStyle.Content, maximumContentDim: MAX_HEIGHT)
            };

            if (view.Width == Dim.Absolute (0))
            {
                view.Width = Dim.Fill ();
            }

            if (view.Height == Dim.Absolute (0))
            {
                view.Height = MAX_HEIGHT - 2;
            }

            frame.Add (view);
            container.Add (frame);
            previous = frame;
        }

        Shortcut quitShortcut = new () { Text = "Quit", Key = Application.GetDefaultKey(Command.Quit), Action = RequestStop };

        statusBar.Add (quitShortcut);

        Add (menuBar, container, propsEditor, statusBar);
    }

    /// <summary>
    ///     Statically creates every <see cref="IDesignable"/> view. No reflection, no
    ///     CreateInstance — fully AOT-safe.
    /// </summary>
    private static IEnumerable<(string Name, View View)> CreateAllViews ()
    {
        // ── Core controls (IDesignable — EnableForDesign populates demo data) ──
        yield return Design (new Button ());
        yield return Design (new ColorPicker ());
        yield return Design (new DropDownList ());
        yield return Design (new FlagSelector ());
        yield return Design (new GraphView ());
        yield return Design (new HexView ());
        yield return Design (new Link { Text = "Terminal.Gui on GitHub", Url = "https://github.com/gui-cs/Terminal.Gui" });
        yield return Design (new ListView ());
        yield return Design (new OptionSelector ());
        yield return Design (new ProgressBar ());
        yield return Design (new ScrollBar { Orientation = Orientation.Horizontal });
        yield return Design (new Shortcut ());
        yield return Design (new SpinnerView ());
        yield return Design (new Tabs ());
        yield return Design (new TextField ());
        yield return Design (new TextValidateField ());
        yield return Design (new TextView ());
        yield return Design (new TreeView ());

        // ── Views without IDesignable (still AOT-relevant as config hosts or common views) ──
        yield return (nameof (CharMap), new CharMap ());
        yield return (nameof (CheckBox), new CheckBox { Text = "Check me" });
        yield return (nameof (DatePicker), new DatePicker ());
        yield return (nameof (FrameView), new FrameView { Title = "FrameView", Text = "Content" });
        yield return (nameof (Label), new Label { Text = "This is a Label." });
        yield return (nameof (Line), new Line ());
        yield return (nameof (NumericUpDown), new NumericUpDown ());
    }

    private static (string Name, View View) Design<T> (T view) where T : View, IDesignable
    {
        view.EnableForDesign ();

        return (typeof (T).Name, view);
    }
}
