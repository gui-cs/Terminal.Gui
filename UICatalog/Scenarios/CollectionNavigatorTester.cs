﻿using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata (
                      "Collection Navigator",
                      "Demonstrates keyboard navigation in ListView & TreeView (CollectionNavigator)."
                  )]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("ListView")]
[ScenarioCategory ("TreeView")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Mouse and Keyboard")]
public class CollectionNavigatorTester : Scenario
{
    private readonly List<string> _items = new []
    {
        "a",
        "b",
        "bb",
        "c",
        "ccc",
        "ccc",
        "cccc",
        "ddd",
        "dddd",
        "dddd",
        "ddddd",
        "dddddd",
        "ddddddd",
        "this",
        "this is a test",
        "this was a test",
        "this and",
        "that and that",
        "the",
        "think",
        "thunk",
        "thunks",
        "zip",
        "zap",
        "zoo",
        "@jack",
        "@sign",
        "@at",
        "@ateme",
        "n@",
        "n@brown",
        ".net",
        "$100.00",
        "$101.00",
        "$101.10",
        "$101.11",
        "$200.00",
        "$210.99",
        "$$",
        "appricot",
        "arm",
        "丗丙业丞",
        "丗丙丛",
        "text",
        "egg",
        "candle",
        " <- space",
        "\t<- tab",
        "\n<- newline",
        "\r<- formfeed",
        "q",
        "quit",
        "quitter"
    }.ToList ();

    private ListView _listView;
    private TreeView _treeView;

    // Don't create a Window, just return the top-level view
    public override void Init ()
    {
        Application.Init ();
        Top = new ();
        Top.ColorScheme = Colors.ColorSchemes ["Base"];
    }

    public override void Setup ()
    {
        var allowMarking = new MenuItem ("Allow _Marking", "", null)
        {
            CheckType = MenuItemCheckStyle.Checked, Checked = false
        };
        allowMarking.Action = () => allowMarking.Checked = _listView.AllowsMarking = !_listView.AllowsMarking;

        var allowMultiSelection = new MenuItem ("Allow Multi _Selection", "", null)
        {
            CheckType = MenuItemCheckStyle.Checked, Checked = false
        };

        allowMultiSelection.Action = () =>
                                         allowMultiSelection.Checked =
                                             _listView.AllowsMultipleSelection = !_listView.AllowsMultipleSelection;
        allowMultiSelection.CanExecute = () => (bool)allowMarking.Checked;

        var menu = new MenuBar
        {
            Menus =
            [
                new MenuBarItem (
                                 "_Configure",
                                 new []
                                 {
                                     allowMarking,
                                     allowMultiSelection,
                                     null,
                                     new (
                                          "_Quit",
                                          $"{Application.QuitKey}",
                                          () => Quit (),
                                          null,
                                          null,
                                          (KeyCode)Application.QuitKey
                                         )
                                 }
                                ),
                new MenuBarItem ("_Quit", $"{Application.QuitKey}", () => Quit ())
            ]
        };

        Top.Add (menu);

        _items.Sort (StringComparer.OrdinalIgnoreCase);

        CreateListView ();
        var vsep = new LineView (Orientation.Vertical) { X = Pos.Right (_listView), Y = 1, Height = Dim.Fill () };
        Top.Add (vsep);
        CreateTreeView ();
    }

    private void CreateListView ()
    {
        var label = new Label
        {
            Text = "ListView",
            TextAlignment = TextAlignment.Centered,
            X = 0,
            Y = 1, // for menu
            AutoSize = false,
            Width = Dim.Percent (50),
            Height = 1
        };
        Top.Add (label);

        _listView = new ListView
        {
            X = 0,
            Y = Pos.Bottom (label),
            Width = Dim.Percent (50) - 1,
            Height = Dim.Fill (),
            AllowsMarking = false,
            AllowsMultipleSelection = false
        };
        Top.Add (_listView);

        _listView.SetSource (_items);

        _listView.KeystrokeNavigator.SearchStringChanged += (s, e) => { label.Text = $"ListView: {e.SearchString}"; };
    }

    private void CreateTreeView ()
    {
        var label = new Label
        {
            Text = "TreeView",
            TextAlignment = TextAlignment.Centered,
            X = Pos.Right (_listView) + 2,
            Y = 1, // for menu
            AutoSize = false,
            Width = Dim.Percent (50),
            Height = 1
        };
        Top.Add (label);

        _treeView = new TreeView
        {
            X = Pos.Right (_listView) + 1, Y = Pos.Bottom (label), Width = Dim.Fill (), Height = Dim.Fill ()
        };
        _treeView.Style.HighlightModelTextOnly = true;
        Top.Add (_treeView);

        var root = new TreeNode ("IsLetterOrDigit examples");

        root.Children = _items.Where (i => char.IsLetterOrDigit (i [0]))
                              .Select (i => new TreeNode (i))
                              .Cast<ITreeNode> ()
                              .ToList ();
        _treeView.AddObject (root);
        root = new TreeNode ("Non-IsLetterOrDigit examples");

        root.Children = _items.Where (i => !char.IsLetterOrDigit (i [0]))
                              .Select (i => new TreeNode (i))
                              .Cast<ITreeNode> ()
                              .ToList ();
        _treeView.AddObject (root);
        _treeView.ExpandAll ();
        _treeView.GoToFirst ();

        _treeView.KeystrokeNavigator.SearchStringChanged += (s, e) => { label.Text = $"TreeView: {e.SearchString}"; };
    }

    private void Quit () { Application.RequestStop (); }
}
