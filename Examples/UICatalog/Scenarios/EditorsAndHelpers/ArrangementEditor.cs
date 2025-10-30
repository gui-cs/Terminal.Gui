#nullable enable
using System;
using System.Collections.Generic;
using Terminal.Gui.ViewBase;

namespace UICatalog.Scenarios;

/// <summary>
///     Provides an editor UI for the Margin, Border, and Padding of a View.
/// </summary>
public sealed class ArrangementEditor : EditorBase
{
    public ArrangementEditor ()
    {
        Title = "ArrangementEditor";
        TabStop = TabBehavior.TabGroup;

        Initialized += ArrangementEditor_Initialized;

        Add (_arrangementSelector);
    }

    private readonly FlagSelector<ViewArrangement> _arrangementSelector = new()
    {
        Orientation = Orientation.Vertical,
    };

    protected override void OnViewToEditChanged ()
    {
        _arrangementSelector.Enabled = ViewToEdit is not Adornment;

        _arrangementSelector.ValueChanged -= ArrangementFlagsOnValueChanged;

        // Set the appropriate options in the slider based on _viewToEdit.Arrangement
        if (ViewToEdit is { })
        {
            _arrangementSelector.Value = ViewToEdit.Arrangement;
        }

        _arrangementSelector.ValueChanged += ArrangementFlagsOnValueChanged;
    }

    private void ArrangementFlagsOnValueChanged (object? sender, EventArgs<int?> e)
    {
        if (ViewToEdit is { } && e.Value is { })
        {
            ViewToEdit.Arrangement = (ViewArrangement)e.Value;

            if (ViewToEdit.Arrangement.HasFlag (ViewArrangement.Overlapped))
            {
                ViewToEdit.ShadowStyle = ShadowStyle.Transparent;
                ViewToEdit.SchemeName = "Toplevel";
            }
            else
            {
                ViewToEdit.ShadowStyle = ShadowStyle.None;
                ViewToEdit.SchemeName = ViewToEdit!.SuperView!.SchemeName;
            }

            if (ViewToEdit.Arrangement.HasFlag (ViewArrangement.Movable))
            {
                ViewToEdit.BorderStyle = LineStyle.Double;
            }
            else
            {
                ViewToEdit.BorderStyle = LineStyle.Single;
            }
        }
    }

    private void ArrangementEditor_Initialized (object? sender, EventArgs e)
    {
        _arrangementSelector.ValueChanged += ArrangementFlagsOnValueChanged;
    }
}
