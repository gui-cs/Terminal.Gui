using System.Collections;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("List View With Selection", "ListView with columns and selection")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("ListView")]
public class ListViewWithSelection : Scenario {
    public CheckBox _allowMarkingCB;
    public CheckBox _allowMultipleCB;
    public CheckBox _customRenderCB;
    public List<Scenario> _scenarios;
    public ListView _listView;

    public override void Setup () {
        _scenarios = GetScenarios ();

        _customRenderCB = new CheckBox { X = 0, Y = 0, Text = "Use custom rendering" };
        Win.Add (_customRenderCB);
        _customRenderCB.Toggled += _customRenderCB_Toggled;

        _allowMarkingCB = new CheckBox {
            X = Pos.Right (_customRenderCB) + 1, Y = 0, Text = "Allow Marking", AllowNullChecked = false
        };
        Win.Add (_allowMarkingCB);
        _allowMarkingCB.Toggled += AllowMarkingCB_Toggled;

        _allowMultipleCB = new CheckBox {
            X = Pos.Right (_allowMarkingCB) + 1,
            Y = 0,
            Visible = (bool)_allowMarkingCB.Checked,
            Text = "Allow Multi-Select"
        };
        Win.Add (_allowMultipleCB);
        _allowMultipleCB.Toggled += AllowMultipleCB_Toggled;

        _listView = new ListView {
            X = 1,
            Y = 2,
            Height = Dim.Fill (),
            Width = Dim.Fill (1),

            //ColorScheme = Colors.ColorSchemes ["TopLevel"],
            AllowsMarking = false,
            AllowsMultipleSelection = false
        };
        _listView.RowRender += ListView_RowRender;
        Win.Add (_listView);

        var scrollBar = new ScrollBarView (_listView, true);

        scrollBar.ChangedPosition += (s, e) => {
            _listView.TopItem = scrollBar.Position;
            if (_listView.TopItem != scrollBar.Position) {
                scrollBar.Position = _listView.TopItem;
            }

            _listView.SetNeedsDisplay ();
        };

        scrollBar.OtherScrollBarView.ChangedPosition += (s, e) => {
            _listView.LeftItem = scrollBar.OtherScrollBarView.Position;
            if (_listView.LeftItem != scrollBar.OtherScrollBarView.Position) {
                scrollBar.OtherScrollBarView.Position = _listView.LeftItem;
            }

            _listView.SetNeedsDisplay ();
        };

        _listView.DrawContent += (s, e) => {
            scrollBar.Size = _listView.Source.Count - 1;
            scrollBar.Position = _listView.TopItem;
            scrollBar.OtherScrollBarView.Size = _listView.MaxLength - 1;
            scrollBar.OtherScrollBarView.Position = _listView.LeftItem;
            scrollBar.Refresh ();
        };

        _listView.SetSource (_scenarios);

        var k = "Keep Content Always In Viewport";
        var keepCheckBox = new CheckBox {
            X = Pos.AnchorEnd (k.Length + 3), Y = 0, Text = k, Checked = scrollBar.AutoHideScrollBars
        };
        keepCheckBox.Toggled += (s, e) => scrollBar.KeepContentAlwaysInViewport = (bool)keepCheckBox.Checked;
        Win.Add (keepCheckBox);
    }

    private void _customRenderCB_Toggled (object sender, ToggleEventArgs e) {
        if (e.OldValue == true) {
            _listView.SetSource (_scenarios);
        } else {
            _listView.Source = new ScenarioListDataSource (_scenarios);
        }

        Win.SetNeedsDisplay ();
    }

    private void AllowMarkingCB_Toggled (object sender, ToggleEventArgs e) {
        _listView.AllowsMarking = (bool)!e.OldValue;
        _allowMultipleCB.Visible = _listView.AllowsMarking;
        Win.SetNeedsDisplay ();
    }

    private void AllowMultipleCB_Toggled (object sender, ToggleEventArgs e) {
        _listView.AllowsMultipleSelection = (bool)!e.OldValue;
        Win.SetNeedsDisplay ();
    }

    private void ListView_RowRender (object sender, ListViewRowEventArgs obj) {
        if (obj.Row == _listView.SelectedItem) {
            return;
        }

        if (_listView.AllowsMarking && _listView.Source.IsMarked (obj.Row)) {
            obj.RowAttribute = new Attribute (Color.BrightRed, Color.BrightYellow);

            return;
        }

        if (obj.Row % 2 == 0) {
            obj.RowAttribute = new Attribute (Color.BrightGreen, Color.Magenta);
        } else {
            obj.RowAttribute = new Attribute (Color.BrightMagenta, Color.Green);
        }
    }

    // This is basically the same implementation used by the UICatalog main window
    internal class ScenarioListDataSource : IListDataSource {
        public ScenarioListDataSource (List<Scenario> itemList) { Scenarios = itemList; }
        private readonly int _nameColumnWidth = 30;
        private BitArray marks;
        private int count;
        private List<Scenario> scenarios;

        public List<Scenario> Scenarios {
            get => scenarios;
            set {
                if (value != null) {
                    count = value.Count;
                    marks = new BitArray (count);
                    scenarios = value;
                    Length = GetMaxLengthItem ();
                }
            }
        }

        public bool IsMarked (int item) {
            if (item >= 0 && item < count) {
                return marks[item];
            }

            return false;
        }

        public int Count => Scenarios != null ? Scenarios.Count : 0;

        public int Length { get; private set; }

        public void Render (
            ListView container,
            ConsoleDriver driver,
            bool selected,
            int item,
            int col,
            int line,
            int width,
            int start = 0
        ) {
            container.Move (col, line);

            // Equivalent to an interpolated string like $"{Scenarios[item].Name, -widtestname}"; if such a thing were possible
            string s = string.Format (
                string.Format ("{{0,{0}}}", -_nameColumnWidth),
                Scenarios[item].GetName ()
            );
            RenderUstr (driver, $"{s} ({Scenarios[item].GetDescription ()})", col, line, width, start);
        }

        public void SetMark (int item, bool value) {
            if (item >= 0 && item < count) {
                marks[item] = value;
            }
        }

        public IList ToList () { return Scenarios; }

        private int GetMaxLengthItem () {
            if (scenarios?.Count == 0) {
                return 0;
            }

            var maxLength = 0;
            for (var i = 0; i < scenarios.Count; i++) {
                string s = string.Format (
                    string.Format ("{{0,{0}}}", -_nameColumnWidth),
                    Scenarios[i].GetName ()
                );
                var sc = $"{s}  {Scenarios[i].GetDescription ()}";
                int l = sc.Length;
                if (l > maxLength) {
                    maxLength = l;
                }
            }

            return maxLength;
        }

        // A slightly adapted method from: https://github.com/gui-cs/Terminal.Gui/blob/fc1faba7452ccbdf49028ac49f0c9f0f42bbae91/Terminal.Gui/Views/ListView.cs#L433-L461
        private void RenderUstr (ConsoleDriver driver, string ustr, int col, int line, int width, int start = 0) {
            var used = 0;
            int index = start;
            while (index < ustr.Length) {
                (Rune rune, int size) = ustr.DecodeRune (index, index - ustr.Length);
                int count = rune.GetColumns ();
                if (used + count >= width) {
                    break;
                }

                driver.AddRune (rune);
                used += count;
                index += size;
            }

            while (used < width) {
                driver.AddRune ((Rune)' ');
                used++;
            }
        }
    }
}
