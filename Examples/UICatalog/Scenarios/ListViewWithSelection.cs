using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using JetBrains.Annotations;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("List View With Selection", "ListView with custom rendering, columns, and selection")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("ListView")]
[ScenarioCategory ("Scrolling")]
public class ListViewWithSelection : Scenario
{
    private CheckBox _allowMarkingCb;
    private CheckBox _allowMultipleCb;
    private CheckBox _customRenderCb;
    private CheckBox _keep;
    private ListView _listView;
    private ObservableCollection<Scenario> _scenarios;
    private Window _appWindow;

    private ObservableCollection<string> _eventList = new ();
    private ListView _eventListView;

    /// <inheritdoc />
    public override void Main ()
    {
        Application.Init ();

        _appWindow = new ()
        {
            Title = GetQuitKeyAndName (),
        };

        _scenarios = GetScenarios ();

        _customRenderCb = new CheckBox { X = 0, Y = 0, Text = "Custom _Rendering" };
        _appWindow.Add (_customRenderCb);
        _customRenderCb.CheckedStateChanging += CustomRenderCB_Toggle;

        _allowMarkingCb = new CheckBox
        {
            X = Pos.Right (_customRenderCb) + 1,
            Y = 0,
            Text = "Allows_Marking",
            AllowCheckStateNone = false
        };
        _appWindow.Add (_allowMarkingCb);
        _allowMarkingCb.CheckedStateChanging += AllowsMarkingCB_Toggle;

        _allowMultipleCb = new CheckBox
        {
            X = Pos.Right (_allowMarkingCb) + 1,
            Y = 0,
            Enabled = _allowMarkingCb.CheckedState == CheckState.Checked,
            Text = "AllowsMulti_Select"
        };
        _appWindow.Add (_allowMultipleCb);
        _allowMultipleCb.CheckedStateChanging += AllowsMultipleSelectionCB_Toggle;

        _keep = new CheckBox
        {
            X = Pos.Right (_allowMultipleCb) + 1,
            Y = 0,
            Text = "Allow_YGreaterThanContentHeight"
        };
        _appWindow.Add (_keep);
        _keep.CheckedStateChanging += AllowYGreaterThanContentHeightCB_Toggle;

        _listView = new ListView
        {
            Title = "_ListView",
            X = 0,
            Y = Pos.Bottom (_allowMarkingCb),
            Width = Dim.Func (() => _listView?.MaxLength ?? 10),
            Height = Dim.Fill (),
            AllowsMarking = false,
            AllowsMultipleSelection = false,
            BorderStyle = LineStyle.Dotted,
            Arrangement = ViewArrangement.Resizable
        };
        _listView.RowRender += ListView_RowRender;
        _appWindow.Add (_listView);

        _listView.SetSource (_scenarios);

        _eventList = new ();

        _eventListView = new ListView
        {
            X = Pos.Right (_listView) + 1,
            Y = Pos.Top (_listView),
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Source = new ListWrapper<string> (_eventList)
        };
        _eventListView.ColorScheme = Colors.ColorSchemes ["TopLevel"];
        _appWindow.Add (_eventListView);

        _listView.SelectedItemChanged += (s, a) => LogEvent (s as View, a, "SelectedItemChanged");
        _listView.OpenSelectedItem += (s, a) => LogEvent (s as View, a, "OpenSelectedItem");
        _listView.CollectionChanged += (s, a) => LogEvent (s as View, a, "CollectionChanged");
        _listView.Accepting += (s, a) => LogEvent (s as View, a, "Accept");
        _listView.Selecting += (s, a) => LogEvent (s as View, a, "Select");
        _listView.VerticalScrollBar.AutoShow = true;
        _listView.HorizontalScrollBar.AutoShow = true;

        bool? LogEvent (View sender, EventArgs args, string message)
        {
            var msg = $"{message,-7}: {args}";
            _eventList.Add (msg);
            _eventListView.MoveDown ();

            return null;
        }

        Application.Run (_appWindow);
        _appWindow.Dispose ();
        Application.Shutdown ();
    }

    private void CustomRenderCB_Toggle (object sender, CancelEventArgs<CheckState> stateEventArgs)
    {
        if (stateEventArgs.CurrentValue == CheckState.Checked)
        {
            _listView.SetSource (_scenarios);
        }
        else
        {
            _listView.Source = new ScenarioListDataSource (_scenarios);
        }

        _appWindow.SetNeedsDraw ();
    }

    private void AllowsMarkingCB_Toggle (object sender, [NotNull] CancelEventArgs<CheckState> stateEventArgs)
    {
        _listView.AllowsMarking = stateEventArgs.NewValue == CheckState.Checked;
        _allowMultipleCb.Enabled = _listView.AllowsMarking;
        _appWindow.SetNeedsDraw ();
    }

    private void AllowsMultipleSelectionCB_Toggle (object sender, [NotNull] CancelEventArgs<CheckState> stateEventArgs)
    {
        _listView.AllowsMultipleSelection = stateEventArgs.NewValue == CheckState.Checked;
        _appWindow.SetNeedsDraw ();
    }


    private void AllowYGreaterThanContentHeightCB_Toggle (object sender, [NotNull] CancelEventArgs<CheckState> stateEventArgs)
    {
        if (stateEventArgs.NewValue == CheckState.Checked)
        {
            _listView.ViewportSettings |= Terminal.Gui.ViewportSettings.AllowYGreaterThanContentHeight;
        }
        else
        {
            _listView.ViewportSettings &= ~Terminal.Gui.ViewportSettings.AllowYGreaterThanContentHeight;
        }
        _appWindow.SetNeedsDraw ();
    }

    private void ListView_RowRender (object sender, ListViewRowEventArgs obj)
    {
        if (obj.Row == _listView.SelectedItem)
        {
            return;
        }

        if (_listView.AllowsMarking && _listView.Source.IsMarked (obj.Row))
        {
            obj.RowAttribute = new Attribute (Color.Black, Color.White);

            return;
        }

        if (obj.Row % 2 == 0)
        {
            obj.RowAttribute = new Attribute (Color.Green, Color.Black);
        }
        else
        {
            obj.RowAttribute = new Attribute (Color.Black, Color.Green);
        }
    }

    // This is basically the same implementation used by the UICatalog main window
    internal class ScenarioListDataSource : IListDataSource
    {
        private readonly int _nameColumnWidth = 30;
        private int _count;
        private BitArray _marks;
        private ObservableCollection<Scenario> _scenarios;
        public ScenarioListDataSource (ObservableCollection<Scenario> itemList) { Scenarios = itemList; }

        public ObservableCollection<Scenario> Scenarios
        {
            get => _scenarios;
            set
            {
                if (value != null)
                {
                    _count = value.Count;
                    _marks = new BitArray (_count);
                    _scenarios = value;
                    Length = GetMaxLengthItem ();
                }
            }
        }

        public bool IsMarked (int item)
        {
            if (item >= 0 && item < _count)
            {
                return _marks [item];
            }

            return false;
        }
#pragma warning disable CS0067
        /// <inheritdoc />
        public event NotifyCollectionChangedEventHandler CollectionChanged;
#pragma warning restore CS0067

        public int Count => Scenarios?.Count ?? 0;
        public int Length { get; private set; }
        public bool SuspendCollectionChangedEvent { get => throw new System.NotImplementedException (); set => throw new System.NotImplementedException (); }

        public void Render (
            ListView container,
            bool selected,
            int item,
            int col,
            int line,
            int width,
            int start = 0
        )
        {
            container.Move (col, line);

            // Equivalent to an interpolated string like $"{Scenarios[item].Name, -widtestname}"; if such a thing were possible
            string s = string.Format (
                                      string.Format ("{{0,{0}}}", -_nameColumnWidth),
                                      Scenarios [item].GetName ()
                                     );
            RenderUstr (container, $"{s} ({Scenarios [item].GetDescription ()})", col, line, width, start);
        }

        public void SetMark (int item, bool value)
        {
            if (item >= 0 && item < _count)
            {
                _marks [item] = value;
            }
        }

        public IList ToList () { return Scenarios; }

        private int GetMaxLengthItem ()
        {
            if (_scenarios?.Count == 0)
            {
                return 0;
            }

            var maxLength = 0;

            for (var i = 0; i < _scenarios.Count; i++)
            {
                string s = string.Format (
                                          $"{{0,{-_nameColumnWidth}}}",
                                          Scenarios [i].GetName ()
                                         );
                var sc = $"{s}  {Scenarios [i].GetDescription ()}";
                int l = sc.Length;

                if (l > maxLength)
                {
                    maxLength = l;
                }
            }

            return maxLength;
        }

        // A slightly adapted method from: https://github.com/gui-cs/Terminal.Gui/blob/fc1faba7452ccbdf49028ac49f0c9f0f42bbae91/Terminal.Gui/Views/ListView.cs#L433-L461
        private void RenderUstr (View view, string ustr, int col, int line, int width, int start = 0)
        {
            var used = 0;
            int index = start;

            while (index < ustr.Length)
            {
                (Rune rune, int size) = ustr.DecodeRune (index, index - ustr.Length);
                int count = rune.GetColumns ();

                if (used + count >= width)
                {
                    break;
                }

                view.AddRune (rune);
                used += count;
                index += size;
            }

            while (used < width)
            {
                view.AddRune ((Rune)' ');
                used++;
            }
        }

        public void Dispose ()
        {
            _scenarios = null;
        }
    }
}
