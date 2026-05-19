// Copilot

using UICatalog;
using UICatalog.Scenarios;

namespace StressTests;

/// <summary>
///     Verifies that scenarios ported from TextView to Editor properly dispose all Views on exit.
///     Requires DEBUG_IDISPOSABLE to be defined (Debug builds of StressTests project).
/// </summary>
public class EditorScenarioDisposalTests
{
    private readonly ITestOutputHelper _output;

    public EditorScenarioDisposalTests (ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [MemberData (nameof (EditorScenarioTypes))]
    public void Editor_Scenarios_Dispose_All_Views (Type scenarioType)
    {
#if DEBUG_IDISPOSABLE
        View.EnableDebugIDisposableAsserts = true;
        View.Instances.Clear ();
#endif

        ConfigurationManager.Disable (true);
        Application.ResetState (true);

        uint abortTime = 3000;
        object? timeout = null;
        IApplication? app = null;
        var iterationCount = 0;

        _output.WriteLine ($"Running Scenario '{scenarioType.Name}'");
        Scenario scenario = (Scenario)Activator.CreateInstance (scenarioType)!;

        Application.InstanceInitialized += OnInit;
        Application.InstanceDisposed += OnDisposed;
        Application.ForceDriver = DriverRegistry.Names.ANSI;

        scenario.Main ();
        scenario.Dispose ();

        Application.ForceDriver = string.Empty;
        Application.InstanceInitialized -= OnInit;
        Application.InstanceDisposed -= OnDisposed;

#if DEBUG_IDISPOSABLE
        // Verify all views were properly disposed
        List<View> leakedViews = View.Instances.Where (v => !v.WasDisposed).ToList ();

        foreach (View leaked in leakedViews)
        {
            _output.WriteLine ($"  NOT DISPOSED: {leaked.GetType ().Name} - {leaked.ToDebugString ()}");
        }

        View.Instances.Clear ();

        Assert.Empty (leakedViews);
#endif

        return;

        void OnInit (object? s, EventArgs<IApplication> a)
        {
            app = a.Value;
            timeout = app.AddTimeout (TimeSpan.FromMilliseconds (abortTime), ForceClose);
            app.Iteration += OnIteration;
        }

        void OnDisposed (object? s, EventArgs<IApplication> a)
        {
            if (app is { })
            {
                app.Iteration -= OnIteration;
            }
        }

        void OnIteration (object? s, EventArgs<IApplication?> a)
        {
            iterationCount++;

            if (iterationCount > 5)
            {
                app?.RequestStop ();
            }
        }

        bool ForceClose ()
        {
            _output.WriteLine ($"  Force-closing '{scenarioType.Name}' after {abortTime}ms");
            app?.RequestStop ();

            return false;
        }
    }

    /// <summary>
    ///     Scenarios that were ported from TextView to Editor in this PR.
    /// </summary>
    public static IEnumerable<object []> EditorScenarioTypes =>
    [
        [typeof (Notepad)],
        [typeof (TextInputControls)],
        [typeof (SyntaxHighlighting)],
        [typeof (ConfigurationEditor)],
        [typeof (DimAutoDemo)],
        [typeof (MarkdownTester)],
        [typeof (TextViewAutocompletePopup)],
        [typeof (DynamicStatusBar)],
        [typeof (Adornments)],
        [typeof (ComputedLayout)],
        [typeof (UnicodeInMenu)],
        [typeof (Localization)],
        [typeof (MessageBoxes)],
        [typeof (AnsiEscapeSequenceRequests)],
        [typeof (TextAlignmentAndDirection)]
    ];
}
