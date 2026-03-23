#nullable enable
namespace UICatalog.Scenarios;

/// <summary>
///     Provides an editor UI for the Margin, Border, and Padding of a View.
/// </summary>
public class AdornmentsEditor : EditorBase
{
    public AdornmentsEditor ()
    {
        Title = "AdornmentsEditor";

        TabStop = TabBehavior.TabGroup;

        Initialized += AdornmentsEditor_Initialized;

        SchemeName = "Dialog";
    }

    public MarginEditor? MarginEditor { get; set; }
    public BorderEditor? BorderEditor { get; private set; }
    public PaddingEditor? PaddingEditor { get; private set; }

    /// <inheritdoc/>
    protected override void OnViewToEditChanged ()
    {
        MarginEditor?.AdornmentToEdit = ViewToEdit?.Margin ?? null;

        BorderEditor?.AdornmentToEdit = ViewToEdit?.Border ?? null;

        PaddingEditor?.AdornmentToEdit = ViewToEdit?.Padding ?? null;
        base.OnViewToEditChanged ();
    }

    private void AdornmentsEditor_Initialized (object? sender, EventArgs e)
    {
        ExpanderButton?.Orientation = Orientation.Horizontal;

        MarginEditor = new MarginEditor { X = -1, Y = 0, SuperViewRendersLineCanvas = true, BorderStyle = BorderStyle };
        MarginEditor.Border.Thickness = MarginEditor.Border.Thickness with { Bottom = 0 };
        Add (MarginEditor);

        BorderEditor = new BorderEditor
        {
            X = Pos.Left (MarginEditor), Y = Pos.Bottom (MarginEditor), SuperViewRendersLineCanvas = true, BorderStyle = BorderStyle
        };
        BorderEditor.Border.Thickness = BorderEditor.Border.Thickness with { Bottom = 0 };
        Add (BorderEditor);

        PaddingEditor = new PaddingEditor
        {
            X = Pos.Left (BorderEditor), Y = Pos.Bottom (BorderEditor), SuperViewRendersLineCanvas = true, BorderStyle = BorderStyle
        };
        PaddingEditor.Border.Thickness = PaddingEditor.Border.Thickness with { Bottom = 0 };
        Add (PaddingEditor);

        Width = Dim.Auto (maximumContentDim: Dim.Func (_ => MarginEditor.Frame.Width - 2));

        MarginEditor.ExpanderButton!.Collapsed = true;
        BorderEditor.ExpanderButton!.Collapsed = true;
        PaddingEditor.ExpanderButton!.Collapsed = true;

        MarginEditor.AdornmentToEdit = ViewToEdit?.Margin ?? null;
        BorderEditor.AdornmentToEdit = ViewToEdit?.Border ?? null;
        PaddingEditor.AdornmentToEdit = ViewToEdit?.Padding ?? null;
    }
}
