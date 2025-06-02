#nullable enable


namespace Terminal.Gui.Views;

/// <summary>
///     A status bar is a <see cref="View"/> that snaps to the bottom of a <see cref="Toplevel"/> displaying set of
///     <see cref="Shortcut"/>s. The <see cref="StatusBar"/> should be context sensitive. This means, if the main menu
///     and an open text editor are visible, the items probably shown will be ~F1~ Help ~F2~ Save ~F3~ Load. While a dialog
///     to ask a file to load is executed, the remaining commands will probably be ~F1~ Help. So for each context must be a
///     new instance of a status bar.
/// </summary>
public class StatusBar : Bar, IDesignable
{
    /// <inheritdoc/>
    public StatusBar () : this ([]) { }

    /// <inheritdoc/>
    public StatusBar (IEnumerable<Shortcut> shortcuts) : base (shortcuts)
    {
        TabStop = TabBehavior.NoStop;
        Orientation = Orientation.Horizontal;
        Y = Pos.AnchorEnd ();
        Width = Dim.Fill ();
        Height = Dim.Auto (DimAutoStyle.Content, 1);

        if (Border is { })
        {
            Border.LineStyle = DefaultSeparatorLineStyle;
        }

        SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Menu);

        ConfigurationManager.Applied += OnConfigurationManagerApplied;
        SuperViewChanged += OnSuperViewChanged;
    }

    private void OnSuperViewChanged (object? sender, SuperViewChangedEventArgs e)
    {
        if (SuperView is null)
        {
            // BUGBUG: This is a hack for avoiding a race condition in ConfigurationManager.Apply
            // BUGBUG: For some reason in some unit tests, when Top is disposed, MenuBar.Dispose does not get called.
            // BUGBUG: Yet, the MenuBar does get Removed from Top (and it's SuperView set to null).
            // BUGBUG: Related: https://github.com/gui-cs/Terminal.Gui/issues/4021
            ConfigurationManager.Applied -= OnConfigurationManagerApplied;
        }
    }
    private void OnConfigurationManagerApplied (object? sender, ConfigurationManagerEventArgs e)
    {
        if (Border is { })
        {
            Border.LineStyle = DefaultSeparatorLineStyle;
        }
    }

    /// <summary>
    ///     Gets or sets the default Line Style for the separators between the shortcuts of the StatusBar.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static LineStyle DefaultSeparatorLineStyle { get; set; } = LineStyle.Single;

    /// <inheritdoc />
    protected override void OnSubViewLayout (LayoutEventArgs args)
    {
        for (int index = 0; index < SubViews.Count; index++)
        {
            View barItem = SubViews.ElementAt (index);

            barItem.BorderStyle = BorderStyle;

            if (barItem.Border is { })
            {
                barItem.Border.Thickness = index == SubViews.Count - 1 ? new Thickness (0, 0, 0, 0) : new Thickness (0, 0, 1, 0);
            }

            if (barItem is Shortcut shortcut)
            {
                shortcut.Orientation = Orientation.Horizontal;
            }
        }
        base.OnSubViewLayout (args);
    }

    /// <inheritdoc/>
    protected override void OnSubViewAdded (View subView)
    {
        subView.CanFocus = false;

        if (subView is Shortcut shortcut)
        {
            // TODO: not happy about using AlignmentModes for this. Too implied.
            // TODO: instead, add a property (a style enum?) to Shortcut to control this
            shortcut.AlignmentModes = AlignmentModes.EndToStart;
        }
    }

    /// <inheritdoc />
    bool IDesignable.EnableForDesign ()
    {
        var shortcut = new Shortcut
        {
            Text = "Quit",
            Title = "Q_uit",
            Key = Key.Z.WithCtrl,
        };

        Add (shortcut);

        shortcut = new Shortcut
        {
            Text = "Help Text",
            Title = "Help",
            Key = Key.F1,
        };

        Add (shortcut);

        shortcut = new Shortcut
        {
            Title = "_Show/Hide",
            Key = Key.F10,
            CommandView = new CheckBox
            {
                CanFocus = false,
                Text = "_Show/Hide"
            },
        };

        Add (shortcut);

        var button1 = new Button
        {
            Text = "I'll Hide",
            // Visible = false
        };
        button1.Accepting += OnButtonClicked;
        Add (button1);

        shortcut.Accepting += (s, e) =>
                           {
                               button1.Visible = !button1.Visible;
                               button1.Enabled = button1.Visible;
                               e.Handled = false;
                           };

        Add (new Label
        {
            HotKeySpecifier = new Rune ('_'),
            Text = "Fo_cusLabel",
            CanFocus = true
        });

        var button2 = new Button
        {
            Text = "Or me!",
        };
        button2.Accepting += (s, e) => Application.RequestStop ();

        Add (button2);

        return true;

        void OnButtonClicked (object? sender, EventArgs? e) { MessageBox.Query ("Hi", $"You clicked {sender}"); }
    }

    /// <inheritdoc />
    protected override void Dispose (bool disposing)
    {
        base.Dispose (disposing);

        SuperViewChanged -= OnSuperViewChanged;
        ConfigurationManager.Applied -= OnConfigurationManagerApplied;
    }
}
