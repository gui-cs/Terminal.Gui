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

[ScenarioMetadata ("List View With Selection", "ListView with columns and selection")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("ListView")]
public class ListViewWithSelection : Scenario
{
    private CheckBox _allowMarkingCB;
    private CheckBox _allowMultipleCB;
    private CheckBox _customRenderCB;
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

        _customRenderCB = new CheckBox { X = 0, Y = 0, Text = "Use custom _rendering" };
        _appWindow.Add (_customRenderCB);
        _customRenderCB.CheckedStateChanging += _customRenderCB_Toggle;

        _allowMarkingCB = new CheckBox
        {
            X = Pos.Right (_customRenderCB) + 1, Y = 0, Text = "Allow _Marking", AllowCheckStateNone = false
        };
        _appWindow.Add (_allowMarkingCB);
        _allowMarkingCB.CheckedStateChanging += AllowMarkingCB_Toggle;

        _allowMultipleCB = new CheckBox
        {
            X = Pos.Right (_allowMarkingCB) + 1,
            Y = 0,
            Visible = _allowMarkingCB.CheckedState == CheckState.Checked,
            Text = "Allow Multi-_Select"
        };
        _appWindow.Add (_allowMultipleCB);
        _allowMultipleCB.CheckedStateChanging += AllowMultipleCB_Toggle;

        _listView = new ListView
        {
            Title = "_ListView",
            X = 0,
            Y = Pos.Bottom(_allowMarkingCB),
            Height = Dim.Fill (),
            Width = Dim.Func (() => _listView?.MaxLength ?? 10),

            AllowsMarking = false,
            AllowsMultipleSelection = false
        };
        _listView.Border.Thickness = new Thickness (0, 1, 0, 0);
        _listView.RowRender += ListView_RowRender;
        _appWindow.Add (_listView);

        var scrollBar = new ScrollBarView (_listView, true);

        scrollBar.ChangedPosition += (s, e) =>
        {
            _listView.TopItem = scrollBar.Position;

            if (_listView.TopItem != scrollBar.Position)
            {
                scrollBar.Position = _listView.TopItem;
            }

            _listView.SetNeedsDisplay ();
        };

        scrollBar.OtherScrollBarView.ChangedPosition += (s, e) =>
        {
            _listView.LeftItem = scrollBar.OtherScrollBarView.Position;

            if (_listView.LeftItem != scrollBar.OtherScrollBarView.Position)
            {
                scrollBar.OtherScrollBarView.Position = _listView.LeftItem;
            }

            _listView.SetNeedsDisplay ();
        };

        _listView.DrawContent += (s, e) =>
        {
            scrollBar.Size = _listView.Source.Count;
            scrollBar.Position = _listView.TopItem;
            scrollBar.OtherScrollBarView.Size = _listView.MaxLength;
            scrollBar.OtherScrollBarView.Position = _listView.LeftItem;
            scrollBar.Refresh ();
        };

        _listView.SetSource (_scenarios);

        var k = "_Keep Content Always In Viewport";

        var keepCheckBox = new CheckBox
        {
            X = Pos.Right(_allowMultipleCB) + 1,
            Y = 0, 
            Text = k, 
            CheckedState = scrollBar.AutoHideScrollBars ? CheckState.Checked : CheckState.UnChecked
        };
        keepCheckBox.CheckedStateChanging += (s, e) => scrollBar.KeepContentAlwaysInViewport = e.NewValue == CheckState.Checked;
        _appWindow.Add (keepCheckBox);

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

    private void _customRenderCB_Toggle (object sender, CancelEventArgs<CheckState> stateEventArgs)
    {
        if (stateEventArgs.CurrentValue == CheckState.Checked)
        {
            _listView.SetSource (_scenarios);
        }
        else
        {
            _listView.Source = new ScenarioListDataSource (_scenarios);
        }

        _appWindow.SetNeedsDisplay ();
    }

    private void AllowMarkingCB_Toggle (object sender, [NotNull] CancelEventArgs<CheckState> stateEventArgs)
    {
        _listView.AllowsMarking = stateEventArgs.NewValue == CheckState.Checked;
        _allowMultipleCB.Visible = _listView.AllowsMarking;
        _appWindow.SetNeedsDisplay ();
    }

    private void AllowMultipleCB_Toggle (object sender, [NotNull] CancelEventArgs<CheckState> stateEventArgs)
    {
        _listView.AllowsMultipleSelection = stateEventArgs.NewValue == CheckState.Checked;
        _appWindow.SetNeedsDisplay ();
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
            ConsoleDriver driver,
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
            RenderUstr (driver, $"{s} ({Scenarios [item].GetDescription ()})", col, line, width, start);
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
        private void RenderUstr (ConsoleDriver driver, string ustr, int col, int line, int width, int start = 0)
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

                driver.AddRune (rune);
                used += count;
                index += size;
            }

            while (used < width)
            {
                driver.AddRune ((Rune)' ');
                used++;
            }
        }

        public void Dispose ()
        {
            _scenarios = null;
        }
    }
}
