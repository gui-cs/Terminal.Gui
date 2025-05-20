#nullable enable
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Themes", "Shows off Themes, Schemes, and VisualRoles.")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("Drawing")]
[ScenarioCategory ("Configuration")]

public sealed class Themes : Scenario
{
    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Window appWindow = new ()
        {
            Title = GetQuitKeyAndName (),
            BorderStyle = LineStyle.None
        };

        int y = 0;
        SchemeViewer? prevSchemeViewer = null;
        foreach (KeyValuePair<string, Scheme?> kvp in SchemeManager.GetSchemesForCurrentTheme ())
        {
            SchemeViewer? schemeViewer = new SchemeViewer ()
            {
                Id = $"schemeViewer for {kvp.Key}",
                SchemeName = kvp.Key
            };
            if (prevSchemeViewer is { })
            {
                schemeViewer.Y = Pos.Bottom (prevSchemeViewer);
            }

            prevSchemeViewer = schemeViewer;
            appWindow.Add (schemeViewer);
        }

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }
}

public class SchemeViewer : View
{
    public SchemeViewer ()
    {
        CanFocus = true;
        Height = Dim.Auto ();
        Width = Dim.Auto ();
        BorderStyle = LineStyle.Single;

        VisualRoleViewer? prevRoleViewer = null;
        foreach (VisualRole role in Enum.GetValues<VisualRole> ())
        {
            VisualRoleViewer? roleViewer = new VisualRoleViewer ()
            {
                Role = role,
            };
            if (prevRoleViewer is { })
            {
                roleViewer.Y = Pos.Bottom (prevRoleViewer);
            }
            base.Add (roleViewer);

            prevRoleViewer = roleViewer;
        }
    }

    /// <inheritdoc />
    protected override bool OnSettingSchemeName (in string? currentName, ref string? newName)
    {
        Title = newName ?? "null";

        foreach (VisualRoleViewer v in SubViews.OfType<VisualRoleViewer> ())
        {
            v.SchemeName = newName;
        }
        return base.OnSettingSchemeName (in currentName, ref newName);
    }
}

public class VisualRoleViewer : View
{
    public VisualRoleViewer ()
    {
        CanFocus = true;
        Height = Dim.Auto (DimAutoStyle.Text);
        Width = Dim.Auto (DimAutoStyle.Text);
    }

    private VisualRole? _role;

    public VisualRole? Role
    {
        get => _role;
        set
        {
            _role = value;
            Text = $"{Role?.ToString ()?.PadRight (10)} 0123456789 𝔽𝕆𝕆𝔹𝔸ℝ {SchemeName}";
        }
    }

    /// <inheritdoc />
    protected override bool OnGettingAttributeForRole (in VisualRole role, ref Attribute currentAttribute)
    {
        if (role != Role)
        {
            currentAttribute = GetAttributeForRole (Role!.Value);
            return true;
        }

        return base.OnGettingAttributeForRole (in role, ref currentAttribute);
    }
}