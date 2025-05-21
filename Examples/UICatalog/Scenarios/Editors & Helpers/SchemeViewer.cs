#nullable enable
using Terminal.Gui;

namespace UICatalog.Scenarios;

public class SchemeViewer : FrameView
{
    public SchemeViewer ()
    {
        TabStop = TabBehavior.TabStop;
        CanFocus = true;
        Height = Dim.Auto ();
        Width = Dim.Auto ();

        VisualRoleViewer? prevRoleViewer = null;

        foreach (VisualRole role in Enum.GetValues<VisualRole> ())
        {
            var roleViewer = new VisualRoleViewer
            {
                Role = role
            };

            if (prevRoleViewer is { })
            {
                roleViewer.Y = Pos.Bottom (prevRoleViewer);
            }

            base.Add (roleViewer);

            prevRoleViewer = roleViewer;
        }
    }

    /// <inheritdoc/>
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
