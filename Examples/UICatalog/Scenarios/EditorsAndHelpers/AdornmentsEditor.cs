#nullable enable
using System;

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
        if (MarginEditor is not null)
        {
            MarginEditor.AdornmentToEdit = ViewToEdit?.Margin ?? null;
        }

        if (BorderEditor is not null)
        {
            BorderEditor.AdornmentToEdit = ViewToEdit?.Border ?? null;
        }

        if (PaddingEditor is not null)
        {
            PaddingEditor.AdornmentToEdit = ViewToEdit?.Padding ?? null;
        }

        if (Padding is not null)
        {
            Padding.Text = GetIdentifyingString (ViewToEdit);
        }
    }

    private string GetIdentifyingString (View? view)
    {
        if (view is null)
        {
            return "null";
        }

        if (!string.IsNullOrEmpty (view.Id))
        {
            return view.Id;
        }

        if (!string.IsNullOrEmpty (view.Title))
        {
            return view.Title;
        }

        if (!string.IsNullOrEmpty (view.Text))
        {
            return view.Text;
        }

        return view.GetType ().Name;
    }

    public bool ShowViewIdentifier
    {
        get => Padding is not null && Padding.Thickness != Thickness.Empty;
        set
        {
            if (Padding is null)
            {
                return;
            }

            Padding.Thickness = value ? new (0, 2, 0, 0) : Thickness.Empty;
        }
    }

    private void AdornmentsEditor_Initialized (object? sender, EventArgs e)
    {
        if (ExpanderButton is not null)
        {
            ExpanderButton.Orientation = Orientation.Horizontal;
        }

        MarginEditor = new ()
        {
            X = -1,
            Y = 0,
            SuperViewRendersLineCanvas = true,
            BorderStyle = BorderStyle
        };
        MarginEditor.Border!.Thickness = MarginEditor.Border!.Thickness with { Bottom = 0 };
        Add (MarginEditor);

        BorderEditor = new ()
        {
            X = Pos.Left (MarginEditor),
            Y = Pos.Bottom (MarginEditor),
            SuperViewRendersLineCanvas = true,
            BorderStyle = BorderStyle
        };
        BorderEditor.Border!.Thickness = BorderEditor.Border!.Thickness with { Bottom = 0 };
        Add (BorderEditor);

        PaddingEditor = new ()
        {
            X = Pos.Left (BorderEditor),
            Y = Pos.Bottom (BorderEditor),
            SuperViewRendersLineCanvas = true,
            BorderStyle = BorderStyle
        };
        PaddingEditor.Border!.Thickness = PaddingEditor.Border!.Thickness with { Bottom = 0 };
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
