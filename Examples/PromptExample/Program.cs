// Example demonstrating the Prompt API for getting typed input from users

using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

IApplication app = Application.Create ();
app.Init ();

// Create a main window to host the prompts
Window mainWindow = new ()
{
    Title = "Prompt API Examples (Esc to quit)",
    Width = Dim.Fill (),
    Height = Dim.Fill ()
};

// Add instructions
mainWindow.Add (new Label
{
    Text = "This example demonstrates various uses of the Prompt API.\nPress the buttons to try different prompt types.\nPress Esc to quit.",
    X = Pos.Center (),
    Y = 1,
    Width = Dim.Fill () - 2,
    Height = 4,
    TextAlignment = Alignment.Center
});

int buttonY = 6;

// Example 1: TextField with string result using auto-Text extraction
Button textFieldButton = new ()
{
    Title = "TextField (Auto-Text)",
    X = Pos.Center (),
    Y = buttonY++
};

textFieldButton.Accepting += (_, _) =>
{
    string? result = mainWindow.Prompt<TextField, string> (
        beginInitHandler: prompt =>
        {
            prompt.Title = "Enter Your Name";
            prompt.GetWrappedView ().Width = 40;
            prompt.GetWrappedView ().Text = "Default name";
        });

    if (result is { })
    {
        MessageBox.Query ("Result", $"You entered: {result}", Strings.btnOk);
    }
    else
    {
        MessageBox.Query ("Result", "Canceled", Strings.btnOk);
    }
};

mainWindow.Add (textFieldButton);

// Example 2: DatePicker with DateTime result
Button datePickerButton = new ()
{
    Title = "DatePicker (Typed Result)",
    X = Pos.Center (),
    Y = buttonY++
};

datePickerButton.Accepting += (_, _) =>
{
    DateTime? result = mainWindow.Prompt<DatePicker, DateTime> (
        resultExtractor: dp => dp.Date,
        beginInitHandler: prompt =>
        {
            prompt.Title = "Select a Date";
            prompt.GetWrappedView ().Date = DateTime.Now;
        });

    if (result is { } selectedDate)
    {
        MessageBox.Query ("Result", $"You selected: {selectedDate:yyyy-MM-dd}", Strings.btnOk);
    }
    else
    {
        MessageBox.Query ("Result", "Canceled", Strings.btnOk);
    }
};

mainWindow.Add (datePickerButton);

// Example 3: ColorPicker with Color result
Button colorPickerButton = new ()
{
    Title = "ColorPicker (Typed Result)",
    X = Pos.Center (),
    Y = buttonY++
};

colorPickerButton.Accepting += (_, _) =>
{
    Color? result = mainWindow.Prompt<ColorPicker, Color> (
        resultExtractor: cp => cp.SelectedColor,
        beginInitHandler: prompt =>
        {
            prompt.Title = "Pick a Color";
            prompt.GetWrappedView ().SelectedColor = Color.Blue;
        });

    if (result is { } selectedColor)
    {
        MessageBox.Query ("Result", $"You selected: {selectedColor}", Strings.btnOk);
    }
    else
    {
        MessageBox.Query ("Result", "Canceled", Strings.btnOk);
    }
};

mainWindow.Add (colorPickerButton);

// Example 4: ColorPicker with auto-Text extraction
Button colorTextButton = new ()
{
    Title = "ColorPicker (Auto-Text)",
    X = Pos.Center (),
    Y = buttonY++
};

colorTextButton.Accepting += (_, _) =>
{
    string? result = mainWindow.Prompt<ColorPicker, string> (
        beginInitHandler: prompt =>
        {
            prompt.Title = "Pick a Color (as text)";
            prompt.GetWrappedView ().SelectedColor = Color.Red;
        });

    if (result is { })
    {
        MessageBox.Query ("Result", $"Color as text: {result}", Strings.btnOk);
    }
    else
    {
        MessageBox.Query ("Result", "Canceled", Strings.btnOk);
    }
};

mainWindow.Add (colorTextButton);

// Example 5: Pre-created view
Button preCreatedButton = new ()
{
    Title = "Pre-Created View",
    X = Pos.Center (),
    Y = buttonY++
};

preCreatedButton.Accepting += (_, _) =>
{
    // Pre-create and configure the view
    TextField textField = new ()
    {
        Width = 50,
        Text = "Pre-configured text"
    };

    string? result = mainWindow.Prompt<TextField, string> (
        view: textField,
        beginInitHandler: prompt =>
        {
            prompt.Title = "Pre-Created TextField";
            prompt.BorderStyle = LineStyle.Rounded;
        });

    if (result is { })
    {
        MessageBox.Query ("Result", $"You entered: {result}", Strings.btnOk);
    }
};

mainWindow.Add (preCreatedButton);

// Example 6: Custom form with complex result extraction
Button customFormButton = new ()
{
    Title = "Custom Form",
    X = Pos.Center (),
    Y = buttonY++
};

customFormButton.Accepting += (_, _) =>
{
    View formView = CreateCustomForm ();

    FormData? result = mainWindow.Prompt<View, FormData> (
        view: formView,
        resultExtractor: ExtractFormData,
        beginInitHandler: prompt =>
        {
            prompt.Title = "User Information Form";
        });

    if (result is { } formData)
    {
        MessageBox.Query (
            "Form Results",
            $"Name: {formData.Name}\nAge: {formData.Age}\nAgreed: {formData.Agreed}",
            Strings.btnOk);
    }
    else
    {
        MessageBox.Query ("Result", "Form canceled", Strings.btnOk);
    }
};

mainWindow.Add (customFormButton);

// Example 7: FlagSelector with enum result
Button flagSelectorButton = new ()
{
    Title = "FlagSelector (Enum Result)",
    X = Pos.Center (),
    Y = buttonY++
};

flagSelectorButton.Accepting += (_, _) =>
{
    SelectorStyles? result = mainWindow.Prompt<FlagSelector<SelectorStyles>, SelectorStyles> (
        resultExtractor: fs => fs.Value,
        beginInitHandler: prompt =>
        {
            prompt.Title = "Choose Selector Styles";
        });

    if (result is { } styles)
    {
        MessageBox.Query ("Result", $"Selected styles: {styles}", Strings.btnOk);
    }
    else
    {
        MessageBox.Query ("Result", "Canceled", Strings.btnOk);
    }
};

mainWindow.Add (flagSelectorButton);

// Add a quit button
Button quitButton = new ()
{
    Title = "Quit",
    X = Pos.Center (),
    Y = buttonY + 2
};

quitButton.Accepting += (_, _) => app.RequestStop ();
mainWindow.Add (quitButton);

app.Run (mainWindow);
mainWindow.Dispose ();
app.Dispose ();

// Helper method to create a custom form
View CreateCustomForm ()
{
    View form = new ()
    {
        Width = 50,
        Height = 10
    };

    TextField nameField = new ()
    {
        Id = "nameField",
        X = 10,
        Y = 1,
        Width = 30
    };

    TextField ageField = new ()
    {
        Id = "ageField",
        X = 10,
        Y = 3,
        Width = 10
    };

    CheckBox agreeCheckbox = new ()
    {
        Id = "agreeCheckbox",
        Title = "I agree to terms",
        X = 10,
        Y = 5
    };

    form.Add (new Label { Text = "Name:", X = 2, Y = 1 });
    form.Add (nameField);
    form.Add (new Label { Text = "Age:", X = 2, Y = 3 });
    form.Add (ageField);
    form.Add (agreeCheckbox);

    return form;
}

// Helper method to extract data from the custom form
FormData ExtractFormData (View form)
{
    TextField? nameField = form.SubViews.FirstOrDefault (v => v.Id == "nameField") as TextField;
    TextField? ageField = form.SubViews.FirstOrDefault (v => v.Id == "ageField") as TextField;
    CheckBox? agreeCheckbox = form.SubViews.FirstOrDefault (v => v.Id == "agreeCheckbox") as CheckBox;

    return new ()
    {
        Name = nameField?.Text ?? string.Empty,
        Age = int.TryParse (ageField?.Text, out int age) ? age : 0,
        Agreed = agreeCheckbox?.CheckedState == CheckState.Checked
    };
}

// Result type for custom form
internal record FormData
{
    public string Name { get; init; } = string.Empty;
    public int Age { get; init; }
    public bool Agreed { get; init; }
}
