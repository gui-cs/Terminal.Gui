
namespace Terminal.Gui.Views;

public partial class ColorPicker
{
    /// <summary>
    ///     Open a <see cref="Dialog"/> with two <see cref="ColorPicker"/> or <see cref="ColorPicker16"/>, based on the
    ///     <see cref="IConsoleDriver.Force16Colors"/> is false or true, respectively, for <see cref="Attribute.Foreground"/>
    ///     and <see cref="Attribute.Background"/> colors.
    /// </summary>
    /// <param name="title">The title to show in the dialog.</param>
    /// <param name="currentAttribute">The current attribute used.</param>
    /// <param name="newAttribute">The new attribute.</param>
    /// <returns><see langword="true"/> if a new color was accepted, otherwise <see langword="false"/>.</returns>
    public static bool Prompt (string title, Attribute? currentAttribute, out Attribute newAttribute)
    {
        var accept = false;

        var d = new Dialog
        {
            Title = title,
            Width = Application.Force16Colors ? 37 : Dim.Auto (DimAutoStyle.Auto, Dim.Percent (80), Dim.Percent (90)),
            Height = 20
        };

        var btnOk = new Button
        {
            X = Pos.Center () - 5,
            Y = Application.Force16Colors ? 6 : 4,
            Text = "Ok",
            Width = Dim.Auto (),
            IsDefault = true
        };

        btnOk.Accepting += (s, e) =>
                        {
                            accept = true;
                            e.Handled = true;
                            Application.RequestStop ();
                        };

        var btnCancel = new Button
        {
            X = Pos.Center () + 5,
            Y = 4,
            Text = Strings.btnCancel,
            Width = Dim.Auto ()
        };

        btnCancel.Accepting += (s, e) =>
                            {
                                e.Handled = true;
                                Application.RequestStop ();
                            };

        d.Add (btnOk);
        d.Add (btnCancel);

        d.AddButton (btnOk);
        d.AddButton (btnCancel);

        View cpForeground;

        if (Application.Force16Colors)
        {
            cpForeground = new ColorPicker16
            {
                SelectedColor = currentAttribute!.Value.Foreground.GetClosestNamedColor16 (),
                Width = Dim.Fill (),
                BorderStyle = LineStyle.Single,
                Title = "Foreground"
            };
        }
        else
        {
            cpForeground = new ColorPicker
            {
                SelectedColor = currentAttribute!.Value.Foreground,
                Width = Dim.Fill (),
                Style = new () { ShowColorName = true, ShowTextFields = true },
                BorderStyle = LineStyle.Single,
                Title = "Foreground"
            };
            ((ColorPicker)cpForeground).ApplyStyleChanges ();
        }

        View cpBackground;

        if (Application.Force16Colors)
        {
            cpBackground = new ColorPicker16
            {
                SelectedColor = currentAttribute!.Value.Background.GetClosestNamedColor16 (),
                Y = Pos.Bottom (cpForeground) + 1,
                Width = Dim.Fill (),
                BorderStyle = LineStyle.Single,
                Title = "Background"
            };
        }
        else
        {
            cpBackground = new ColorPicker
            {
                SelectedColor = currentAttribute!.Value.Background,
                Width = Dim.Fill (),
                Y = Pos.Bottom (cpForeground) + 1,
                Style = new () { ShowColorName = true, ShowTextFields = true },
                BorderStyle = LineStyle.Single,
                Title = "Background"
            };
            ((ColorPicker)cpBackground).ApplyStyleChanges ();
        }

        d.Add (cpForeground, cpBackground);

        Application.Run (d);
        d.Dispose ();
        Color newForeColor = Application.Force16Colors ? ((ColorPicker16)cpForeground).SelectedColor : ((ColorPicker)cpForeground).SelectedColor;
        Color newBackColor = Application.Force16Colors ? ((ColorPicker16)cpBackground).SelectedColor : ((ColorPicker)cpBackground).SelectedColor;
        newAttribute = new (newForeColor, newBackColor);

        return accept;
    }
}
