using System.Text;
using JetBrains.Annotations;

namespace ViewsTests;

[TestSubject (typeof (Shortcut))]
public class ShortcutTests
{
    // CommandView Test view for Shortcut tests
    private sealed class TestCommandView : View, IValue<int?>
    {
        public TestCommandView ()
        {
            Width = 5;
            Height = 1;
        }

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

        private void OnValueChanging (int? oldValue, int? newValue) => ValueChanging?.Invoke (this, new ValueChangingEventArgs<int?> (oldValue, newValue));

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
    public void HotKey_Title_Initializer_SetsCorrectly ()
    {
        Shortcut shortcut = new () { Title = "_C" };
        Assert.Equal (Key.Empty, shortcut.CommandView.HotKey);
        Assert.Equal (Key.C, shortcut.HotKey);
    }

    [Fact]
    public void HotKey_CommandView_Set_Sets_Correctly ()
    {
        Shortcut shortcut = new () { Title = "_C" };

        shortcut.CommandView = new View { HotKeySpecifier = (Rune)'_', Text = "_D" };
        Assert.Equal (Key.Empty, shortcut.CommandView.HotKey);
        Assert.Equal (Key.D, shortcut.HotKey);

        shortcut = new Shortcut
        {
            Title = "_C",
            CommandView = new View { HotKeySpecifier = (Rune)'_', Text = "_D" }
        };
        Assert.Equal (Key.Empty, shortcut.CommandView.HotKey);
        Assert.Equal (Key.D, shortcut.HotKey);

        shortcut = new Shortcut
        {
            CommandView = new View { HotKeySpecifier = (Rune)'_', Text = "_D" },
            Title = "_C",
        };
        Assert.Equal (Key.Empty, shortcut.CommandView.HotKey);
        Assert.Equal (Key.C, shortcut.HotKey);
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

    [Theory]
    [CombinatorialData]
    public void KeyDown_Key_Raises_HandlingHotKey_And_Accepting (bool canFocus)
    {
        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);

        Shortcut shortcut = new () { Key = Key.A, Text = "0", Title = "_C", CanFocus = canFocus };
        runnable.Add (shortcut);

        Assert.Equal (canFocus, shortcut.HasFocus);

        var accepting = 0;

        shortcut.Accepting += (_, e) => { accepting++; };

        var activated = 0;

        shortcut.Activating += (_, e) => { activated++; };

        var handlingHotKey = 0;

        shortcut.HandlingHotKey += (_, e) => { handlingHotKey++; };

        app.Keyboard.RaiseKeyDownEvent (shortcut.Key);

        Assert.Equal (0, accepting);
        Assert.Equal (1, handlingHotKey);
        Assert.Equal (1, activated);
    }

    [Theory]
    [CombinatorialData]
    public void CheckBox_KeyDown_Key_Raises_HandlingHotKey_And_Accepting (bool canFocus)
    {
        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);

        Shortcut shortcut = new () { Key = Key.F4, Text = "0", CanFocus = canFocus, CommandView = new CheckBox { Title = "_Test", CanFocus = canFocus } };
        runnable.Add (shortcut);

        Assert.Equal (canFocus, shortcut.HasFocus);

        var accepting = 0;

        shortcut.Accepting += (_, e) => { accepting++; };

        var activated = 0;

        shortcut.Activating += (_, e) => { activated++; };

        var handlingHotKey = 0;

        shortcut.HandlingHotKey += (_, e) => { handlingHotKey++; };

        app.Keyboard.RaiseKeyDownEvent (shortcut.Key);

        Assert.Equal (0, accepting);
        Assert.Equal (1, handlingHotKey);
        Assert.Equal (1, activated);
    }

    [Theory]
    [InlineData (true, KeyCode.A, 0, 1)]
    [InlineData (true, KeyCode.C, 0, 1)]
    [InlineData (true, KeyCode.C | KeyCode.AltMask, 0, 1)]
    [InlineData (true, KeyCode.Enter, 1, 0)]
    [InlineData (true, KeyCode.Space, 0, 1)]
    [InlineData (true, KeyCode.F1, 0, 0)]
    [InlineData (false, KeyCode.A, 0, 1)]
    [InlineData (false, KeyCode.C, 0, 1)]
    [InlineData (false, KeyCode.C | KeyCode.AltMask, 0, 1)]
    [InlineData (false, KeyCode.Enter, 0, 0)]
    [InlineData (false, KeyCode.Space, 0, 0)]
    [InlineData (false, KeyCode.F1, 0, 0)]
    public void KeyDown_Valid_Keys_Raises_Accepted_Activated_Correctly (bool canFocus, KeyCode key, int expectedAccept, int expectedActivate)
    {
        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);

        Shortcut shortcut = new () { Key = Key.A, Text = "0", Title = "_C", CanFocus = canFocus };

        // The default CommandView does not have a HotKey, so only the Shortcut's Key should trigger activation, not the CommandView's HotKey
        Assert.Equal (Key.A, shortcut.Key);
        Assert.Equal (Key.C, shortcut.HotKey);
        Assert.Equal (Key.Empty, shortcut.CommandView.HotKey);

        runnable.Add (shortcut);

        Assert.Equal (canFocus, shortcut.HasFocus);

        var accepting = 0;

        shortcut.Accepting += (_, e) =>
                              {
                                  accepting++;

                                  //e.Handled = true;
                              };

        var activated = 0;

        shortcut.Activating += (_, e) =>
                               {
                                   activated++;

                                   //e.Handled = true;
                               };

        var handlingHotKey = 0;

        shortcut.HandlingHotKey += (_, e) =>
                                   {
                                       handlingHotKey++;

                                       //e.Handled = true;
                                   };

        app.Keyboard.RaiseKeyDownEvent (key);

        Assert.Equal (expectedAccept, accepting);
        Assert.Equal (expectedActivate, activated);
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

    [Fact]
    public void CommandView_Command_Activate_Bubbles_To_Shortcut ()
    {
        // Arrange
        TestCommandView testCommandView = new () { Title = "_Test", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = testCommandView };

        var shortcutActivatingRaised = 0;
        shortcut.Activating += (_, _) => shortcutActivatingRaised++;

        var commandViewActivatingRaised = 0;
        testCommandView.Activating += (_, _) => commandViewActivatingRaised++;

        // Act - Invoke Command.HotKey directly on CheckBox
        testCommandView.InvokeCommand (Command.Activate);

        // Assert - Shortcut.Activating should have been raised
        Assert.Equal (1, shortcutActivatingRaised);
        Assert.Equal (1, commandViewActivatingRaised);
    }

    [Fact]
    public void CommandView_Command_Accept_Forwards_To_Accepting ()
    {
        // Arrange
        TestCommandView testCommandView = new () { Title = "_Test", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = testCommandView };

        var shortcutActivatingRaised = 0;
        shortcut.Activating += (_, _) => shortcutActivatingRaised++;

        var commandViewActivatingRaised = 0;
        testCommandView.Activating += (_, _) => commandViewActivatingRaised++;

        var shortcutAcceptingRaised = 0;
        shortcut.Accepting += (_, _) => { shortcutAcceptingRaised++; };

        var commandViewAcceptingRaised = 0;
        testCommandView.Accepting += (_, _) => { commandViewAcceptingRaised++; };

        // Act - Invoke Command.HotKey directly on CheckBox
        testCommandView.InvokeCommand (Command.Accept);

        // Assert - Shortcut.Activating should have been raised
        Assert.Equal (1, shortcutAcceptingRaised);
        Assert.Equal (1, commandViewAcceptingRaised);
        Assert.Equal (0, shortcutActivatingRaised);
        Assert.Equal (0, commandViewActivatingRaised);
    }

    [Fact]
    public void Command_Activate_Direct_InvokeCommand_Raises_Activating_Once ()
    {
        // Arrange
        TestCommandView testCommandView = new () { Title = "_Test", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = testCommandView };

        var shortcutActivatingCount = 0;
        shortcut.Activating += (_, _) => shortcutActivatingCount++;

        shortcut.InvokeCommand (Command.Activate);

        // Assert - Shortcut.Activating should be raised exactly once
        Assert.Equal (1, shortcutActivatingCount);
    }

    [Fact]
    public void CommandView_Command_HotKey_Ignored_Raised_By_Shortcut ()
    {
        // Arrange
        TestCommandView testCommandView = new () { Title = "_Test", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = testCommandView };

        var shortcutActivatingRaised = 0;
        shortcut.Activating += (_, _) => shortcutActivatingRaised++;

        var commandViewActivatingRaised = 0;
        testCommandView.Activating += (_, _) => commandViewActivatingRaised++;

        var shortcutHandlingHotKeyFired = 0;
        shortcut.HandlingHotKey += (_, _) => { shortcutHandlingHotKeyFired++; };

        var commandViewHandlingHotKeyFired = 0;
        testCommandView.HandlingHotKey += (_, _) => { commandViewHandlingHotKeyFired++; };

        // Act - Invoke Command.HotKey directly on CommandView
        testCommandView.InvokeCommand (Command.HotKey);

        // Assert - Shortcut.Activating should have been raised
        Assert.Equal (0, shortcutHandlingHotKeyFired);
        Assert.Equal (1, commandViewHandlingHotKeyFired);
        Assert.Equal (1, shortcutActivatingRaised);
        Assert.Equal (1, commandViewActivatingRaised);
    }

    [Theory]
    [CombinatorialData]
    public void CommandView_Click_Raises_Activating_On_Both (bool commandViewCanFocus)
    {
        // Arrange
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        TestCommandView testCommandView = new () { Title = "_Test", CanFocus = commandViewCanFocus };

        Shortcut shortcut = new () { Key = Key.T, CommandView = testCommandView };
        (runnable as View)?.Add (shortcut);
        app.Begin (runnable);

        var shortcutActivatingRaised = 0;
        shortcut.Activating += (_, _) => shortcutActivatingRaised++;

        var commandViewActivatingRaised = 0;
        testCommandView.Activating += (_, _) => commandViewActivatingRaised++;

        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (testCommandView.Frame.Location));

        // Assert - Shortcut.Activating should have been raised
        Assert.Equal (1, shortcutActivatingRaised);
        Assert.Equal (1, commandViewActivatingRaised);
    }

    [Theory]
    [CombinatorialData]
    public void CommandView_KeyDown_HotKey_Raises_Activating_On_Both (bool commandViewCanFocus)
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        CheckBox testCommandView = new () { Title = "_Test", CanFocus = commandViewCanFocus };
        testCommandView.Value = 0;

        Shortcut shortcut = new () { Key = Key.F4, CommandView = testCommandView };
        Assert.Equal (Key.F4, shortcut.Key);
        Assert.Equal (Key.Empty, shortcut.HotKey);
        Assert.Equal (Key.T, testCommandView.HotKey);

        (runnable as View)?.Add (shortcut);
        app.Begin (runnable);
        Assert.Equal (commandViewCanFocus, testCommandView.HasFocus);
        Assert.True (shortcut.HasFocus);

        var shortcutActivatingRaised = 0;
        shortcut.Activating += (_, _) => shortcutActivatingRaised++;

        var shortcutActivatedRaised = 0;
        shortcut.Activated += (_, _) => shortcutActivatedRaised++;

        var commandViewActivatingRaised = 0;
        testCommandView.Activating += (_, _) => commandViewActivatingRaised++;

        var shortcutHandlingHotKeyFired = 0;
        shortcut.HandlingHotKey += (_, _) => { shortcutHandlingHotKeyFired++; };

        var commandViewHandlingHotKeyFired = 0;
        testCommandView.HandlingHotKey += (_, _) => { commandViewHandlingHotKeyFired++; };

        Assert.Equal (CheckState.UnChecked, testCommandView.Value);
        app.Keyboard.RaiseKeyDownEvent (testCommandView.HotKey);

        // Assert - Shortcut.Activating should have been raised
        Assert.Equal (1, shortcutActivatingRaised);
        Assert.Equal (1, shortcutActivatedRaised);
        Assert.Equal (0, shortcutHandlingHotKeyFired);
        Assert.Equal (1, commandViewActivatingRaised);
        Assert.Equal (1, commandViewHandlingHotKeyFired);

        Assert.Equal (CheckState.Checked, testCommandView.Value);
    }

    [Fact]
    public void Command_Activate_Raises_Activating_Only ()
    {
        Shortcut shortcut = new ();
        var activatingFired = 0;

        shortcut.Activating += (_, _) => activatingFired++;

        var acceptingFired = 0;
        shortcut.Accepting += (_, _) => acceptingFired++;

        var handlingHotKeyFired = 0;
        shortcut.HandlingHotKey += (_, _) => handlingHotKeyFired++;

        shortcut.InvokeCommand (Command.Activate);

        Assert.Equal (1, activatingFired);
        Assert.Equal (0, acceptingFired);
        Assert.Equal (0, handlingHotKeyFired);

        shortcut.Dispose ();
    }

    [Fact]
    public void Command_Accept_Raises_Accepting_Only ()
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

        Assert.False (activatingFired);
        Assert.True (acceptingFired);
        Assert.False (handlingHotKeyFired);
    }

    [Fact]
    public void Command_Accept_Direct_InvokeCommand_Raises_Accepting_Once ()
    {
        // Arrange
        TestCommandView testCommandView = new () { Title = "_Test", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = testCommandView };

        var shortcutAcceptingCount = 0;
        shortcut.Accepting += (_, _) => shortcutAcceptingCount++;

        shortcut.InvokeCommand (Command.Accept);

        Assert.Equal (1, shortcutAcceptingCount);
    }

    [Fact]
    public void Command_HotKey_Raises_HandlingHotKey_Then_Activating ()
    {
        using Shortcut shortcut = new ();
        var activatingFired = 0;

        shortcut.Activating += (_, _) => { activatingFired++; };

        var acceptingFired = 0;
        shortcut.Accepting += (_, _) => { acceptingFired++; };

        var handlingHotKeyFired = 0;
        shortcut.HandlingHotKey += (_, _) => { handlingHotKeyFired++; };

        shortcut.InvokeCommand (Command.HotKey);

        Assert.Equal (1, handlingHotKeyFired);
        Assert.Equal (1, activatingFired);
        Assert.Equal (0, acceptingFired);
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
        using TestCommandView commandView = new ();
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
        using TestCommandView commandView = new ();
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

    // Claude - Haiku 4.5
    /// <summary>
    ///     Verifies that clicking anywhere across the entire width of a Shortcut causes activation,
    ///     including clicks in gaps between CommandView, HelpView, and KeyView.
    /// </summary>
    [Fact]
    public void Click_Anywhere_On_Shortcut_Causes_Activation ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        using Shortcut shortcut = new ();
        shortcut.Key = Key.F1;
        shortcut.HelpText = "Help text";
        shortcut.Title = "Command";
        shortcut.Width = 40; // Wide enough to create gaps between subviews
        shortcut.Height = 1;

        (runnable as View)?.Add (shortcut);
        app.Begin (runnable);

        var activatingCount = 0;

        shortcut.Activating += (_, _) => { activatingCount++; };

        // Verify layout created gaps
        Assert.True (shortcut.Frame.Width >= 40, "Shortcut should be wide enough for gaps");
        Assert.True (shortcut.CommandView.Frame.Width > 0, "CommandView should be visible");
        Assert.True (shortcut.HelpView.Frame.Width > 0, "HelpView should be visible");
        Assert.True (shortcut.KeyView.Frame.Width > 0, "KeyView should be visible");

        // Act & Assert - Click at various X positions across the entire width
        for (var x = 0; x < shortcut.Frame.Width; x++)
        {
            int expectedCount = activatingCount + 1;

            // Simulate mouse click at position x
            app.InjectSequence (InputInjectionExtensions.LeftButtonClick (new Point (x, 0)));

            Assert.True (activatingCount == expectedCount,
                         $"Click at X={x} should activate the Shortcut. Expected: {expectedCount}, Actual: {activatingCount}");
        }
    }
}
