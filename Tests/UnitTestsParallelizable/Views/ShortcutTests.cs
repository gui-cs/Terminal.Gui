using JetBrains.Annotations;

namespace ViewsTests;

[TestSubject (typeof (Shortcut))]
public class ShortcutTests
{
    // Test view for Shortcut tests
    private sealed class ShortcutTestView : View, IValue<int?>
    {
        public int? Value
        {
            get;
            set
            {
                if (field == value)
                {
                    return;
                }
                int? oldValue = field;
                field = value;
                OnValueChanging (oldValue, value);
                OnValueChanged (oldValue, value);
            }
        }

        public event EventHandler<ValueChangingEventArgs<int?>>? ValueChanging;
        public event EventHandler<ValueChangedEventArgs<int?>>? ValueChanged;
        public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

        private void OnValueChanging (int? oldValue, int? newValue) =>
            ValueChanging?.Invoke (this, new ValueChangingEventArgs<int?> (oldValue, newValue));

        private void OnValueChanged (int? oldValue, int? newValue)
        {
            ValueChanged?.Invoke (this, new ValueChangedEventArgs<int?> (oldValue, newValue));
            ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (oldValue, newValue));
        }
    }

    [Fact]
    public void Constructor_Defaults ()
    {
        // Test parameterless constructor
        Shortcut shortcut = new ();

        Assert.NotNull (shortcut);

        // CanFocus defaults
        Assert.True (shortcut.CanFocus);
        Assert.False (shortcut.CommandView.CanFocus);

        // Dimension defaults
        Assert.IsType<DimAuto> (shortcut.Width);
        Assert.IsType<DimAuto> (shortcut.Height);

        // Orientation and alignment defaults
        Assert.Equal (Orientation.Horizontal, shortcut.Orientation);
        Assert.Equal (AlignmentModes.StartToEnd | AlignmentModes.IgnoreFirstOrLast, shortcut.AlignmentModes);

        // Key defaults
        Assert.Equal (Key.Empty, shortcut.Key);
        Assert.False (shortcut.BindKeyToApplication);
        Assert.Equal (0, shortcut.MinimumKeyTextSize);

        // Text/Title defaults
        Assert.Equal (string.Empty, shortcut.Title);
        Assert.Equal (string.Empty, shortcut.Text);
        Assert.Equal (string.Empty, shortcut.HelpText);

        // CommandView defaults
        Assert.NotNull (shortcut.CommandView);
        Assert.Equal ("CommandView", shortcut.CommandView.Id);

        // HelpView defaults
        Assert.NotNull (shortcut.HelpView);
        Assert.Equal ("_helpView", shortcut.HelpView.Id);
        Assert.Equal (string.Empty, shortcut.HelpView.Text);
        Assert.True (shortcut.HelpView.Visible);

        // KeyView defaults
        Assert.NotNull (shortcut.KeyView);
        Assert.Equal ("_keyView", shortcut.KeyView.Id);
        Assert.Equal (string.Empty, shortcut.KeyView.Text);
        Assert.True (shortcut.KeyView.Visible);

        // Action defaults
        Assert.Null (shortcut.Action);

        // Mouse highlight defaults
        Assert.Equal (MouseState.In, shortcut.MouseHighlightStates);

        // Focus defaults
        Assert.False (shortcut.ForceFocusColors);

        // SubViews - CommandView added, HelpView and KeyView not added (empty)
        Assert.Contains (shortcut.CommandView, shortcut.SubViews);
        Assert.DoesNotContain (shortcut.HelpView, shortcut.SubViews);
        Assert.DoesNotContain (shortcut.KeyView, shortcut.SubViews);

        // Test parameterized constructor
        Shortcut shortcut1 = new (Key.A, "_CommandText", () => { }, "Help text");
        Assert.Equal (Key.A, shortcut1.Key);
        Assert.Equal ("_CommandText", shortcut1.Title);
        Assert.Equal ("Help text", shortcut1.HelpText);
        Assert.Equal ("Help text", shortcut1.Text);
        Assert.NotNull (shortcut1.Action);

        // Other properties should have defaults
        Assert.True (shortcut1.CanFocus);
        Assert.IsType<DimAuto> (shortcut1.Width);
        Assert.IsType<DimAuto> (shortcut1.Height);
        Assert.False (shortcut1.BindKeyToApplication);
        Assert.Equal (Orientation.Horizontal, shortcut1.Orientation);
        Assert.Equal (AlignmentModes.StartToEnd | AlignmentModes.IgnoreFirstOrLast, shortcut1.AlignmentModes);
        Assert.Equal ("_CommandText", shortcut1.CommandView.Text); // Title syncs to CommandView.Text
        Assert.Equal (MouseState.In, shortcut1.MouseHighlightStates);

        // SubViews - All three should be present with values set
        Assert.Contains (shortcut1.CommandView, shortcut1.SubViews);
        Assert.Contains (shortcut1.HelpView, shortcut1.SubViews);
        Assert.Contains (shortcut1.KeyView, shortcut1.SubViews);

        // KeyView should display the key text
        Assert.Equal ("a", shortcut1.KeyView.Text);
    }

    [Fact]
    public void Size_Defaults ()
    {
        Shortcut shortcut = new ();
        shortcut.Layout ();

        Assert.Equal (2, shortcut.Frame.Width);
        Assert.Equal (1, shortcut.Frame.Height);
        Assert.Equal (2, shortcut.Viewport.Width);
        Assert.Equal (1, shortcut.Viewport.Height);

        Assert.Equal (0, shortcut.CommandView.Viewport.Width);
        Assert.Equal (1, shortcut.CommandView.Viewport.Height);

        Assert.Equal (0, shortcut.HelpView.Viewport.Width);
        Assert.Equal (0, shortcut.HelpView.Viewport.Height);

        Assert.Equal (0, shortcut.KeyView.Viewport.Width);
        Assert.Equal (0, shortcut.KeyView.Viewport.Height);

        //  0123456789
        // "   0  A "
        shortcut = new Shortcut { Key = Key.A, HelpText = "0" };
        shortcut.Layout ();
        Assert.Equal (8, shortcut.Frame.Width);
        Assert.Equal (1, shortcut.Frame.Height);
        Assert.Equal (8, shortcut.Viewport.Width);
        Assert.Equal (1, shortcut.Viewport.Height);

        Assert.Equal (0, shortcut.CommandView.Viewport.Width);
        Assert.Equal (1, shortcut.CommandView.Viewport.Height);

        Assert.Equal (1, shortcut.HelpView.Viewport.Width);
        Assert.Equal (1, shortcut.HelpView.Viewport.Height);

        Assert.Equal (1, shortcut.KeyView.Viewport.Width);
        Assert.Equal (1, shortcut.KeyView.Viewport.Height);

        //  0123456789
        // " C  0  A "
        shortcut = new Shortcut { Title = "C", Key = Key.A, HelpText = "0" };
        shortcut.Layout ();
        Assert.Equal (9, shortcut.Frame.Width);
        Assert.Equal (1, shortcut.Frame.Height);
        Assert.Equal (9, shortcut.Viewport.Width);
        Assert.Equal (1, shortcut.Viewport.Height);

        Assert.Equal (1, shortcut.CommandView.Viewport.Width);
        Assert.Equal (1, shortcut.CommandView.Viewport.Height);

        Assert.Equal (1, shortcut.HelpView.Viewport.Width);
        Assert.Equal (1, shortcut.HelpView.Viewport.Height);

        Assert.Equal (1, shortcut.KeyView.Viewport.Width);
        Assert.Equal (1, shortcut.KeyView.Viewport.Height);
    }

    [Theory]
    [InlineData ("", "", KeyCode.Null, 2)]
    [InlineData ("C", "", KeyCode.Null, 3)]
    [InlineData ("", "H", KeyCode.Null, 5)]
    [InlineData ("", "", KeyCode.K, 5)]
    [InlineData ("C", "", KeyCode.K, 6)]
    [InlineData ("C", "H", KeyCode.Null, 6)]
    [InlineData ("", "H", KeyCode.K, 8)]
    [InlineData ("C", "H", KeyCode.K, 9)]
    public void NaturalSize (string command, string help, KeyCode key, int expectedWidth)
    {
        var shortcut = new Shortcut { HelpText = help, Key = key, Title = command };

        shortcut.Layout ();

        // |0123456789
        // | C  H  K |
        Assert.Equal (expectedWidth, shortcut.Frame.Width);

        shortcut = new Shortcut { HelpText = help, Title = command, Key = key };

        shortcut.Layout ();
        Assert.Equal (expectedWidth, shortcut.Frame.Width);

        shortcut = new Shortcut { HelpText = help, Key = key, Title = command };

        shortcut.Layout ();
        Assert.Equal (expectedWidth, shortcut.Frame.Width);

        shortcut = new Shortcut { Key = key, HelpText = help, Title = command };

        shortcut.Layout ();
        Assert.Equal (expectedWidth, shortcut.Frame.Width);
    }

    [Theory]
    [InlineData (0, 0, 3, 3)]
    [InlineData (1, 0, 3, 3)]
    [InlineData (2, 0, 3, 3)]
    [InlineData (3, 0, 3, 3)]
    [InlineData (4, 0, 3, 3)]
    [InlineData (5, 0, 3, 3)]
    [InlineData (6, 0, 3, 3)]
    [InlineData (7, 0, 3, 4)]
    [InlineData (8, 0, 3, 5)]
    [InlineData (9, 0, 3, 6)]
    [InlineData (10, 0, 4, 7)]
    [InlineData (11, 0, 5, 8)]
    public void Set_Width_Layouts_Correctly (int width, int expectedCmdX, int expectedHelpX, int expectedKeyX)
    {
        var shortcut = new Shortcut { Width = width, Title = "C", Text = "H", Key = Key.K };
        shortcut.Layout ();

        // 01234
        // -C--K

        // 012345
        // -C--K-

        // 0123456
        // -C-H-K-

        // 01234567
        // -C--H-K-

        // 012345678
        // -C--H--K-

        // 0123456789
        // -C--H--K-
        Assert.Equal (expectedCmdX, shortcut.CommandView.Frame.X);
        Assert.Equal (expectedHelpX, shortcut.HelpView.Frame.X);
        Assert.Equal (expectedKeyX, shortcut.KeyView.Frame.X);
    }

    [Fact]
    public void CommandView_Text_And_Title_Track ()
    {
        var shortcut = new Shortcut { Title = "T" };

        Assert.Equal (shortcut.Title, shortcut.CommandView.Text);

        shortcut = new Shortcut ();

        shortcut.CommandView = new View { Text = "T" };
        Assert.Equal (shortcut.Title, shortcut.CommandView.Text);
    }

    [Fact]
    public void HelpText_And_Text_Are_The_Same ()
    {
        var shortcut = new Shortcut { Text = "H" };

        Assert.Equal (shortcut.Text, shortcut.HelpText);

        shortcut = new Shortcut { HelpText = "H" };

        Assert.Equal (shortcut.Text, shortcut.HelpText);
    }

    [Theory]
    [InlineData (KeyCode.Null, "")]
    [InlineData (KeyCode.F1, "F1")]
    public void KeyView_Text_Tracks_Key (KeyCode key, string expected)
    {
        var shortcut = new Shortcut { Key = key };

        Assert.Equal (expected, shortcut.KeyView.Text);
    }

    // Test Key
    [Fact]
    public void Key_Defaults_To_Empty ()
    {
        Shortcut shortcut = new ();

        Assert.Equal (Key.Empty, shortcut.Key);
    }

    [Fact]
    public void Key_Can_Be_Set ()
    {
        Shortcut shortcut = new ();

        shortcut.Key = Key.F1;

        Assert.Equal (Key.F1, shortcut.Key);
    }

    [Fact]
    public void Key_Can_Be_Set_To_Empty ()
    {
        Shortcut shortcut = new ();

        shortcut.Key = Key.Empty;

        Assert.Equal (Key.Empty, shortcut.Key);
    }

    [Fact]
    public void Key_Set_Binds_Key_To_CommandView_Accept ()
    {
        Shortcut shortcut = new ();

        shortcut.Key = Key.F1;

        // TODO:
    }

    [Fact]
    public void Key_Changing_Removes_Previous_Binding ()
    {
        Shortcut shortcut = new ();

        shortcut.Key = Key.A;
        Assert.True (shortcut.HotKeyBindings.TryGet (Key.A, out _));

        shortcut.Key = Key.B;
        Assert.False (shortcut.HotKeyBindings.TryGet (Key.A, out _));
        Assert.True (shortcut.HotKeyBindings.TryGet (Key.B, out _));
    }

    // Test Key gets bound correctly
    [Fact]
    public void BindKeyToApplication_Defaults_To_HotKey ()
    {
        Shortcut shortcut = new ();

        Assert.False (shortcut.BindKeyToApplication);
    }

    [Fact]
    public void BindKeyToApplication_Can_Be_Set ()
    {
        IApplication app = Application.Create ();
        Shortcut shortcut = new () { App = app };

        shortcut.BindKeyToApplication = true;

        Assert.True (shortcut.BindKeyToApplication);
    }

    [Fact]
    public void BindKeyToApplication_Changing_Adjusts_KeyBindings ()
    {
        Shortcut shortcut = new ();

        shortcut.Key = Key.A;
        Assert.True (shortcut.HotKeyBindings.TryGet (Key.A, out _));

        shortcut.App = Application.Create ();
        shortcut.BindKeyToApplication = true;
        shortcut.BeginInit ();
        shortcut.EndInit ();
        Assert.False (shortcut.HotKeyBindings.TryGet (Key.A, out _));
        Assert.True (shortcut.App?.Keyboard.KeyBindings.TryGet (Key.A, out _));

        shortcut.BindKeyToApplication = false;
        Assert.True (shortcut.HotKeyBindings.TryGet (Key.A, out _));
        Assert.False (shortcut.App?.Keyboard.KeyBindings.TryGet (Key.A, out _));
    }

    [Theory]
    [InlineData (Orientation.Horizontal)]
    [InlineData (Orientation.Vertical)]
    public void Orientation_SetsCorrectly (Orientation orientation)
    {
        var shortcut = new Shortcut { Orientation = orientation };

        Assert.Equal (orientation, shortcut.Orientation);
    }

    [Theory]
    [InlineData (AlignmentModes.StartToEnd)]
    [InlineData (AlignmentModes.EndToStart)]
    public void AlignmentModes_SetsCorrectly (AlignmentModes alignmentModes)
    {
        var shortcut = new Shortcut { AlignmentModes = alignmentModes };

        Assert.Equal (alignmentModes, shortcut.AlignmentModes);
    }

    [Fact]
    public void Action_SetsAndGetsCorrectly ()
    {
        var actionInvoked = false;

        var shortcut = new Shortcut { Action = () => { actionInvoked = true; } };

        shortcut.Action.Invoke ();

        Assert.True (actionInvoked);
    }

    [Fact]
    public void SubView_Visibility_Controlled_By_Removal ()
    {
        Shortcut shortcut = new ();

        Assert.True (shortcut.CommandView.Visible);
        Assert.Contains (shortcut.CommandView, shortcut.SubViews);
        Assert.True (shortcut.HelpView.Visible);
        Assert.DoesNotContain (shortcut.HelpView, shortcut.SubViews);
        Assert.True (shortcut.KeyView.Visible);
        Assert.DoesNotContain (shortcut.KeyView, shortcut.SubViews);

        shortcut.HelpText = "help";
        Assert.True (shortcut.HelpView.Visible);
        Assert.Contains (shortcut.HelpView, shortcut.SubViews);
        Assert.True (shortcut.KeyView.Visible);
        Assert.DoesNotContain (shortcut.KeyView, shortcut.SubViews);

        shortcut.Key = Key.A;
        Assert.True (shortcut.HelpView.Visible);
        Assert.Contains (shortcut.HelpView, shortcut.SubViews);
        Assert.True (shortcut.KeyView.Visible);
        Assert.Contains (shortcut.KeyView, shortcut.SubViews);

        shortcut.HelpView.Visible = false;
        shortcut.ShowHide ();
        Assert.False (shortcut.HelpView.Visible);
        Assert.DoesNotContain (shortcut.HelpView, shortcut.SubViews);
        Assert.True (shortcut.KeyView.Visible);
        Assert.Contains (shortcut.KeyView, shortcut.SubViews);

        shortcut.KeyView.Visible = false;
        shortcut.ShowHide ();
        Assert.False (shortcut.HelpView.Visible);
        Assert.DoesNotContain (shortcut.HelpView, shortcut.SubViews);
        Assert.False (shortcut.KeyView.Visible);
        Assert.DoesNotContain (shortcut.KeyView, shortcut.SubViews);
    }

    [Fact]
    public void Focus_CanFocus_Default_Is_True ()
    {
        Shortcut shortcut = new ();
        shortcut.Key = Key.A;
        shortcut.Text = "Help";
        shortcut.Title = "Command";
        Assert.True (shortcut.CanFocus);
        Assert.False (shortcut.CommandView.CanFocus);
    }

    [Fact]
    public void Focus_CanFocus_CommandView_Add_Tracks ()
    {
        Shortcut shortcut = new ();
        Assert.True (shortcut.CanFocus);
        Assert.False (shortcut.CommandView.CanFocus);

        shortcut.CommandView = new View { CanFocus = true };
        Assert.True (shortcut.CommandView.CanFocus);

        shortcut.CommandView.CanFocus = true;
        Assert.True (shortcut.CommandView.CanFocus);

        shortcut.CanFocus = false;
        Assert.False (shortcut.CanFocus);
        Assert.True (shortcut.CommandView.CanFocus);

        shortcut.CommandView.CanFocus = false;
        Assert.False (shortcut.CanFocus);
        Assert.False (shortcut.CommandView.CanFocus);

        shortcut.CommandView.CanFocus = true;
        Assert.False (shortcut.CanFocus);
        Assert.True (shortcut.CommandView.CanFocus);
    }

    [Theory (Skip = "Broke somehow!")]
    [InlineData (true, KeyCode.A, 1, 1)]
    [InlineData (true, KeyCode.C, 1, 1)]
    [InlineData (true, KeyCode.C | KeyCode.AltMask, 1, 1)]
    [InlineData (true, KeyCode.Enter, 1, 1)]
    [InlineData (true, KeyCode.Space, 1, 1)]
    [InlineData (true, KeyCode.F1, 0, 0)]
    [InlineData (false, KeyCode.A, 1, 1)]
    [InlineData (false, KeyCode.C, 1, 1)]
    [InlineData (false, KeyCode.C | KeyCode.AltMask, 1, 1)]
    [InlineData (false, KeyCode.Enter, 0, 0)]
    [InlineData (false, KeyCode.Space, 0, 0)]
    [InlineData (false, KeyCode.F1, 0, 0)]
    public void KeyDown_CheckBox_Raises_Accepted_Selected (bool canFocus, KeyCode key, int expectedAccept, int expectedSelect)
    {
        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);

        Shortcut shortcut = new () { Key = Key.A, Text = "0", CommandView = new CheckBox { Title = "_C" }, CanFocus = canFocus };
        runnable.Add (shortcut);

        Assert.Equal (canFocus, shortcut.HasFocus);

        var accepted = 0;

        shortcut.Accepting += (_, e) =>
                              {
                                  accepted++;
                                  e.Handled = true;
                              };

        var selected = 0;
        shortcut.Activating += (_, _) => selected++;

        app.Keyboard.RaiseKeyDownEvent (key);

        Assert.Equal (expectedAccept, accepted);
        Assert.Equal (expectedSelect, selected);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that a CheckBox with CanFocus=false CAN change state when
    ///     Command.Activate is invoked directly on it.
    ///     CanFocus only controls keyboard focus, not the ability to change state.
    /// </summary>
    [Fact]
    public void CheckBox_CanFocus_False_Changes_State_On_Direct_Activate ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };

        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.False (checkBox.CanFocus);

        // Act - Directly invoke Command.Activate on the CheckBox
        checkBox.InvokeCommand (Command.Activate);

        // Assert - CheckBox with CanFocus=false SHOULD change state
        // CanFocus only controls keyboard focus, not state changes
        Assert.Equal (CheckState.Checked, checkBox.Value);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that a CheckBox with CanFocus=true DOES change state when
    ///     Command.Activate is invoked directly on it.
    /// </summary>
    [Fact]
    public void CheckBox_CanFocus_True_Changes_State_On_Direct_Activate ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = true };

        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.True (checkBox.CanFocus);

        // Act - Directly invoke Command.Activate on the CheckBox
        checkBox.InvokeCommand (Command.Activate);

        // Assert - CheckBox with CanFocus=true SHOULD change state
        Assert.Equal (CheckState.Checked, checkBox.Value);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that direct InvokeCommand(Activate) on Shortcut does NOT forward to CommandView.
    ///     OnActivating only forwards to CommandView when the activation comes from:
    ///     1. IsFromCommandView - the command originated from CommandView
    ///     2. IsBindingFromShortcut - the command came via a keyboard/mouse binding
    ///     Direct InvokeCommand has neither, so CommandView is not activated.
    /// </summary>
    [Fact]
    public void CheckBox_CanFocus_False_Direct_InvokeCommand_Does_Not_Change_State ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = checkBox };

        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.False (checkBox.CanFocus);

        // Act - Invoke Command.Activate directly on the Shortcut (no binding)
        shortcut.InvokeCommand (Command.Activate);

        // Assert - CheckBox does NOT change state because direct InvokeCommand
        // doesn't go through OnActivating's forwarding logic
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that direct InvokeCommand(Activate) on Shortcut does NOT forward to CommandView,
    ///     even when CommandView.CanFocus=true.
    ///     OnActivating only forwards to CommandView when the activation comes via proper bindings.
    /// </summary>
    [Fact]
    public void CheckBox_CanFocus_True_Direct_InvokeCommand_Does_Not_Change_State ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = true };

        Shortcut shortcut = new () { Key = Key.T, CommandView = checkBox };

        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.True (checkBox.CanFocus);

        // Act - Invoke Command.Activate directly on the Shortcut (no binding)
        shortcut.InvokeCommand (Command.Activate);

        // Assert - CheckBox does NOT change state because direct InvokeCommand
        // doesn't go through OnActivating's forwarding logic
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that a mouse release on a CheckBox CommandView changes its state.
    ///     View base class binds LeftButtonReleased to Command.Activate.
    /// </summary>
    [Fact]
    public void CheckBox_CommandView_MouseRelease_Changes_State ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };

        // BUGBUG: This test tests nothing.
        Shortcut shortcut = new () { Key = Key.T, CommandView = checkBox };

        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        // Verify CheckBox has the expected mouse binding for LeftButtonReleased (from View base class)
        Assert.True (checkBox.MouseBindings.TryGet (MouseFlags.LeftButtonReleased, out MouseBinding binding));
        Assert.Contains (Command.Activate, binding.Commands);

        // Act - Simulate a mouse release by invoking the bound command directly
        checkBox.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonReleased });

        // Assert - CheckBox should change state
        Assert.Equal (CheckState.Checked, checkBox.Value);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that invoking Command.Activate directly on the CheckBox CommandView
    ///     (simulating what a mouse press should do) changes its state.
    /// </summary>
    [Fact]
    public void CheckBox_CommandView_Direct_Activate_Changes_State ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };

        // BUGBUG: This test tests nothing.
        Shortcut shortcut = new () { Key = Key.T, CommandView = checkBox };

        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        // Act - Directly invoke Command.Activate on the CheckBox (what mouse press should trigger)
        checkBox.InvokeCommand (Command.Activate);

        // Assert - CheckBox should change state
        Assert.Equal (CheckState.Checked, checkBox.Value);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that MouseHighlightStates defaults to MouseState.In for Shortcut.
    ///     This ensures Shortcuts highlight when the mouse hovers over them.
    /// </summary>
    [Fact]
    public void MouseHighlightStates_Defaults_To_In ()
    {
        // Arrange & Act
        Shortcut shortcut = new ();

        // Assert
        Assert.Equal (MouseState.In, shortcut.MouseHighlightStates);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that when CommandView raises Activating (e.g., from direct click),
    ///     the Shortcut also raises its Activating event.
    /// </summary>
    [Fact]
    public void CommandView_Command_Activating_Forwards_To_Activating ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = checkBox };

        var shortcutActivatingRaised = false;
        shortcut.Activating += (_, _) => shortcutActivatingRaised = true;

        // Act - Invoke Command.Activate directly on CheckBox (simulating direct mouse click)
        checkBox.InvokeCommand (Command.Activate);

        // Assert - Shortcut.Activating should have been raised
        Assert.True (shortcutActivatingRaised);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that when CommandView raises Accepting (e.g., double-click on CheckBox),
    ///     the Shortcut raises its Activating event.
    /// </summary>
    [Fact]
    public void CommandView_Command_Accept_Forwards_To_Activating ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = true };

        Shortcut shortcut = new () { Key = Key.T, CommandView = checkBox };

        var shortcutAcceptingRaised = false;
        shortcut.Accepting += (_, _) => shortcutAcceptingRaised = true;
        var shortcutActivatingRaised = false;
        shortcut.Activating += (_, _) => shortcutActivatingRaised = true;
        var handlingHotkeyRaised = false;
        shortcut.HandlingHotKey += (_, _) => handlingHotkeyRaised = true;

        // Act - Invoke Command.Accept directly on CheckBox (simulating double-click)
        checkBox.InvokeCommand (Command.Accept);

        Assert.False (shortcutAcceptingRaised);
        Assert.True (shortcutActivatingRaised);
        Assert.False (handlingHotkeyRaised);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that when Shortcut is activated via direct InvokeCommand (no binding),
    ///     Activating is raised once but CommandView is NOT forwarded to.
    ///     Direct InvokeCommand bypasses the forwarding logic in OnActivating.
    /// </summary>
    [Fact]
    public void Command_Activate_Direct_InvokeCommand_Raises_Activating_Once ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = checkBox };

        var shortcutActivatingCount = 0;
        shortcut.Activating += (_, _) => shortcutActivatingCount++;

        // Act - Invoke Command.Activate on Shortcut directly (no binding)
        shortcut.InvokeCommand (Command.Activate);

        // Assert - Shortcut.Activating should be raised exactly once
        Assert.Equal (1, shortcutActivatingCount);

        // CheckBox does NOT change state - direct InvokeCommand doesn't forward to CommandView
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that HotKey invoked on CommandView does NOT bubble to Shortcut.
    ///     HotKey is not in Shortcut's CommandsToBubbleUp list [Activate, Accept].
    /// </summary>
    [Fact]
    public void CommandView_Command_HotKey_Does_Not_Bubble_To_Shortcut ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = true };

        Shortcut shortcut = new () { Key = Key.T, CommandView = checkBox };

        var shortcutAcceptingRaised = false;
        shortcut.Accepting += (_, _) => shortcutAcceptingRaised = true;
        var shortcutActivatingRaised = false;
        shortcut.Activating += (_, _) => shortcutActivatingRaised = true;
        var handlingHotkeyRaised = false;
        shortcut.HandlingHotKey += (_, _) => handlingHotkeyRaised = true;

        // Act - Invoke Command.HotKey directly on CheckBox
        checkBox.InvokeCommand (Command.HotKey);

        // HotKey is NOT in CommandsToBubbleUp, so nothing bubbles to Shortcut
        Assert.False (shortcutAcceptingRaised);
        Assert.False (shortcutActivatingRaised);
        Assert.False (handlingHotkeyRaised);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that CommandView with CanFocus=true still allows Shortcut.Activating
    ///     to be raised when the CommandView is clicked directly.
    /// </summary>
    [Fact]
    public void CommandView_CanFocus_True_Click_Raises_Activating ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = true };

        Shortcut shortcut = new () { Key = Key.T, CommandView = checkBox };

        var shortcutActivatingRaised = false;
        shortcut.Activating += (_, _) => shortcutActivatingRaised = true;

        // Act - Invoke Command.Activate directly on CheckBox (simulating mouse click)
        checkBox.InvokeCommand (Command.Activate);

        // Assert - Shortcut.Activating should have been raised
        Assert.True (shortcutActivatingRaised);

        // And CheckBox should have changed state
        Assert.Equal (CheckState.Checked, checkBox.Value);
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void Command_Activate_Raises_Activating_Only ()
    {
        Shortcut shortcut = new () { Title = "Test", Key = Key.T.WithCtrl };
        var activatingFired = false;

        shortcut.Activating += (_, _) => { activatingFired = true; };

        var acceptingFired = false;
        shortcut.Accepting += (_, _) => { acceptingFired = true; };

        var handlingHotKeyFired = false;
        shortcut.HandlingHotKey += (_, _) => { handlingHotKeyFired = true; };

        shortcut.InvokeCommand (Command.Activate);

        Assert.True (activatingFired);
        Assert.False (acceptingFired);
        Assert.False (handlingHotKeyFired);

        shortcut.Dispose ();
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that when Command.Accept is invoked directly on a Shortcut (without going through
    ///     a keyboard/mouse binding), OnAccepting calls RaiseActivating, sets Handled=true, and returns true.
    ///     This prevents the Accepting event from being raised (Accepting is NOT raised because OnAccepting
    ///     returns true before the event can be invoked).
    /// </summary>
    [Fact]
    public void Command_Accept_Raises_Activating_Not_Accepting ()
    {
        using Shortcut shortcut = new ();
        shortcut.Title = "Test";
        shortcut.Key = Key.T.WithCtrl;
        var activatingFired = false;

        shortcut.Activating += (_, _) => { activatingFired = true; };

        var acceptingFired = false;
        shortcut.Accepting += (_, _) => { acceptingFired = true; };

        var handlingHotKeyFired = false;
        shortcut.HandlingHotKey += (_, _) => { handlingHotKeyFired = true; };

        shortcut.InvokeCommand (Command.Accept);

        // OnAccepting calls RaiseActivating (so Activating fires), then returns true
        // which prevents the Accepting event from being raised
        Assert.True (activatingFired);
        Assert.False (acceptingFired);
        Assert.False (handlingHotKeyFired);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that when Command.HotKey is invoked directly on a Shortcut (without going through
    ///     a keyboard binding), OnHandlingHotKey does NOT call RaiseActivating because IsBindingFromShortcut
    ///     returns false (no binding was provided). Only HandlingHotKey is raised.
    /// </summary>
    [Fact]
    public void Command_HotKey_Raises_HandlingHotKey_Only ()
    {
        using Shortcut shortcut = new () { Title = "Test", Key = Key.T.WithCtrl };
        var activatingFired = false;

        shortcut.Activating += (_, _) => { activatingFired = true; };

        var acceptingFired = false;
        shortcut.Accepting += (_, _) => { acceptingFired = true; };

        var handlingHotKeyFired = false;
        shortcut.HandlingHotKey += (_, _) => { handlingHotKeyFired = true; };

        shortcut.InvokeCommand (Command.HotKey);

        // OnHandlingHotKey only calls RaiseActivating when IsBindingFromShortcut is true
        // Since we invoked directly without a binding, Activating is NOT raised
        Assert.False (activatingFired);
        Assert.False (acceptingFired);
        Assert.True (handlingHotKeyFired);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that when Command.Accept is invoked directly on a Shortcut (without going through
    ///     a keyboard/mouse binding), Action is NOT executed because:
    ///     1. OnAccepting calls RaiseActivating (not DefaultActivateHandler), so Activated isn't raised
    ///     2. OnAccepting returns true, preventing Accepted from being raised
    ///     3. Action is invoked in OnActivated and OnAccepted, but neither is called
    /// </summary>
    [Fact]
    public void Command_Accept_Does_Not_Execute_Action_Without_Binding ()
    {
        Shortcut shortcut = new () { Title = "Test" };
        var actionFired = false;
        shortcut.Action = () => actionFired = true;

        // Accept invoked directly does NOT execute Action
        shortcut.InvokeCommand (Command.Accept);

        Assert.False (actionFired);

        shortcut.Dispose ();
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that when Command.HotKey is invoked directly on a Shortcut (without going through
    ///     a keyboard binding), Action is NOT executed because:
    ///     1. OnHandlingHotKey only calls RaiseActivating when IsBindingFromShortcut is true
    ///     2. Without a binding, IsBindingFromShortcut returns false, so RaiseActivating isn't called
    ///     3. Action is invoked in OnActivated, but Activated isn't raised
    /// </summary>
    [Fact]
    public void Command_HotKey_Does_Not_Execute_Action_Without_Binding ()
    {
        Shortcut shortcut = new () { Title = "_Test" };
        var actionFired = false;
        shortcut.Action = () => actionFired = true;

        // HotKey invoked directly does NOT execute Action (no binding = no Activating)
        shortcut.InvokeCommand (Command.HotKey);

        Assert.False (actionFired);

        shortcut.Dispose ();
    }

    // Claude - Haiku 4.5
    /// <summary>
    ///     Verifies that Context.TryGetSource() can be used in Activating event handlers to determine
    ///     if the activation came from the CommandView itself.
    ///     This pattern (shown in Shortcuts.cs:374) allows different handling based on the activation source.
    /// </summary>
    [Fact]
    public void Activating_Context_TryGetSource_Identifies_CommandView_Activation ()
    {
        // Arrange
        using ShortcutTestView commandView = new ();
        commandView.Text = "Command";
        using Shortcut shortcut = new ();
        shortcut.Key = Key.F9;
        shortcut.HelpText = "Test";
        shortcut.CommandView = commandView;

        View? capturedSource = null;
        var activatingFired = 0;

        shortcut.Activating += (_, args) =>
                               {
                                   // ReSharper disable once AccessToModifiedClosure
                                   activatingFired++;
                                   args.Context.TryGetSource (out capturedSource);
                               };

        // Act 1 - Activate the CommandView directly (simulates clicking on the CommandView)
        commandView.InvokeCommand (Command.Activate);

        // Assert - The source should be the CommandView
        Assert.Equal (1, activatingFired);
        Assert.NotNull (capturedSource);
        Assert.Same (commandView, capturedSource);

        // Reset
        capturedSource = null;
        activatingFired = 0;

        // Act 2 - Activate the Shortcut directly (not through CommandView)
        shortcut.InvokeCommand (Command.Activate);

        // Assert - The source should be the Shortcut when activating it directly
        Assert.Equal (1, activatingFired);
        Assert.NotNull (capturedSource);
        Assert.Same (shortcut, capturedSource);
    }

    // Claude - Haiku 4.5
    /// <summary>
    ///     Demonstrates the pattern from Shortcuts.cs:374 where Activating event is marked as Handled
    ///     when activation comes from the CommandView, but custom logic runs otherwise.
    /// </summary>
    [Fact]
    public void Activating_Can_Handle_Differently_Based_On_CommandView_Source ()
    {
        // Arrange
        using ShortcutTestView commandView = new ();
        commandView.Text = "Command";
        using Shortcut shortcut = new ();
        shortcut.Key = Key.F9;
        shortcut.HelpText = "Cycles value";
        shortcut.CommandView = commandView;

        var customLogicExecuted = false;
        var handledWhenFromCommandView = false;

        shortcut.Activating += (_, args) =>
                               {
                                   // Pattern from Shortcuts.cs:374 - check if activation came from CommandView
                                   if (args.Context.TryGetSource (out View? ctxSource) && ctxSource == shortcut.CommandView)
                                   {
                                       // Mark as handled when coming from CommandView
                                       args.Handled = true;
                                       handledWhenFromCommandView = true;
                                   }
                                   else
                                   {
                                       // Execute custom logic when NOT from CommandView
                                       customLogicExecuted = true;
                                   }
                               };

        // Act 1 - Activate the CommandView directly
        commandView.InvokeCommand (Command.Activate);

        // Assert - Should be marked as handled, custom logic should NOT run
        Assert.True (handledWhenFromCommandView);
        Assert.False (customLogicExecuted);

        // Reset
        handledWhenFromCommandView = false;
        customLogicExecuted = false;

        // Act 2 - Activate the Shortcut directly (not through CommandView)
        shortcut.InvokeCommand (Command.Activate);

        // Assert - Should NOT be marked as handled, custom logic SHOULD run
        Assert.False (handledWhenFromCommandView);
        Assert.True (customLogicExecuted);
    }
}
