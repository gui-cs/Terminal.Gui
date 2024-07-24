using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

    /// <inheritdoc />
    public override void Main ()
    {
        Application.Init ();

        _appWindow = new ()
        {
            Title = GetQuitKeyAndName (),
        };

        _scenarios = GetScenarios ();

        _customRenderCB = new CheckBox { X = 0, Y = 0, Text = "Use custom rendering" };
        _appWindow.Add (_customRenderCB);
        _customRenderCB.Toggle += _customRenderCB_Toggle;

        _allowMarkingCB = new CheckBox
        {
            X = Pos.Right (_customRenderCB) + 1, Y = 0, Text = "Allow Marking", AllowCheckStateNone = false
        };
        _appWindow.Add (_allowMarkingCB);
        _allowMarkingCB.Toggle += AllowMarkingCB_Toggle;

        _allowMultipleCB = new CheckBox
        {
            X = Pos.Right (_allowMarkingCB) + 1,
            Y = 0,
            Visible = _allowMarkingCB.State == CheckState.Checked,
            Text = "Allow Multi-Select"
        };
        _appWindow.Add (_allowMultipleCB);
        _allowMultipleCB.Toggle += AllowMultipleCB_Toggle;

        _listView = new ListView
        {
            X = 1,
            Y = 2,
            Height = Dim.Fill (),
            Width = Dim.Fill (1),

            //ColorScheme = Colors.ColorSchemes ["TopLevel"],
            AllowsMarking = false,
            AllowsMultipleSelection = false
        };
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

        var k = "Keep Content Always In Viewport";

        var keepCheckBox = new CheckBox
        {
            X = Pos.AnchorEnd (k.Length + 3), Y = 0, Text = k, State = scrollBar.AutoHideScrollBars ? CheckState.Checked : CheckState.UnChecked
        };
        keepCheckBox.Toggle += (s, e) => scrollBar.KeepContentAlwaysInViewport = e.NewValue == CheckState.Checked;
        _appWindow.Add (keepCheckBox);

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
            obj.RowAttribute = new Attribute (Color.BrightRed, Color.BrightYellow);

            return;
        }

        if (obj.Row % 2 == 0)
        {
            obj.RowAttribute = new Attribute (Color.BrightGreen, Color.Magenta);
        }
        else
        {
            obj.RowAttribute = new Attribute (Color.BrightMagenta, Color.Green);
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

        /// <inheritdoc />
        public event NotifyCollectionChangedEventHandler CollectionChanged;
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
