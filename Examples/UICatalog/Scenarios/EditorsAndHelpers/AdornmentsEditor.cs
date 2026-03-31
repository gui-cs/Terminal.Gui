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

        Add (_tabs);

        Height = Dim.Auto ();
        Width = Dim.Auto ();
    }

    private Tabs _tabs = new Tabs ()
    {
        TabSide = Side.Left,
    };

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

        MarginEditor = new MarginEditor { BorderStyle = LineStyle.None };
        View marginTab = new () { Title = "Margin" };
        marginTab.Add (MarginEditor);
        _tabs.Add (marginTab);

        View borderTab = new () { Title = "Border" };
        BorderEditor = new BorderEditor { BorderStyle = LineStyle.None };
        borderTab.Add (BorderEditor);
        _tabs.Add (borderTab);

        View paddingTab = new () { Title = "Padding" };
        PaddingEditor = new PaddingEditor { BorderStyle = LineStyle.None };
        paddingTab.Add (PaddingEditor);
        _tabs.Add (paddingTab);

        // Set all tabs to Dim.Auto 
        marginTab.Width = Dim.Auto ();
        borderTab.Width = Dim.Auto ();
        paddingTab.Width = Dim.Auto ();

        marginTab.Height = Dim.Auto ();
        borderTab.Height = Dim.Auto ();
        paddingTab.Height = Dim.Auto ();

        Layout ();

        // Get the largest 
        int max = new [] { MarginEditor.Frame.Width, BorderEditor.Frame.Width, PaddingEditor.Frame.Width }.Max ();
        _tabs.Width = Dim.Auto (minimumContentDim: max + marginTab.GetAdornmentsThickness ().Horizontal);

        max = new [] { MarginEditor.Frame.Height, BorderEditor.Frame.Height, PaddingEditor.Frame.Height }.Max ();
        _tabs.Height = Dim.Auto (minimumContentDim: max + marginTab.GetAdornmentsThickness ().Vertical);

        marginTab.Width = Dim.Fill ();
        borderTab.Width = Dim.Fill ();
        paddingTab.Width = Dim.Fill ();
        marginTab.Height = Dim.Fill ();
        borderTab.Height = Dim.Fill ();
        paddingTab.Height = Dim.Fill ();

        MarginEditor.AdornmentToEdit = ViewToEdit?.Margin ?? null;
        BorderEditor.AdornmentToEdit = ViewToEdit?.Border ?? null;
        PaddingEditor.AdornmentToEdit = ViewToEdit?.Padding ?? null;
    }
}
