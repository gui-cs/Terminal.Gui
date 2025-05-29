#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

/// <summary>
///     Provides an editor UI for the Margin, Border, and Padding of a View.
/// </summary>
public class LayoutEditor : EditorBase
{
    public LayoutEditor ()
    {
        Title = "_LayoutEditor";
        CanFocus = true;

        Initialized += LayoutEditor_Initialized;
    }

    private PosEditor? _xEditor;
    private PosEditor? _yEditor;

    private DimEditor? _widthEditor;
    private DimEditor? _heightEditor;

    protected override void OnViewToEditChanged ()
    {
        if (_xEditor is { })
        {
            _xEditor.ViewToEdit = ViewToEdit;
        }

        if (_yEditor is { })
        {
            _yEditor.ViewToEdit = ViewToEdit;
        }

        if (_widthEditor is { })
        {
            _widthEditor.ViewToEdit = ViewToEdit;
        }

        if (_heightEditor is { })
        {
            _heightEditor.ViewToEdit = ViewToEdit;
        }
    }

    private void LayoutEditor_Initialized (object? sender, EventArgs e)
    {
        _xEditor = new ()
        {
            Title = "_X",
            BorderStyle = LineStyle.None,
            Dimension = Dimension.Width
        };

        _yEditor = new ()
        {
            Title = "_Y",
            BorderStyle = LineStyle.None,
            Dimension = Dimension.Height,
            X = Pos.Right (_xEditor) + 1
        };


        _widthEditor = new ()
        {
            Title = "_Width",
            BorderStyle = LineStyle.None,
            Dimension = Dimension.Width,
            X = Pos.Right (_yEditor) + 1
        };

        _heightEditor = new ()
        {
            Title = "_Height",
            BorderStyle = LineStyle.None,
            Dimension = Dimension.Height,
            X = Pos.Right (_widthEditor) + 1
        };

        Add (_xEditor, _yEditor, _widthEditor, _heightEditor);
    }
}
