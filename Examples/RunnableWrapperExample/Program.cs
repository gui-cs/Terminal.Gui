// Example demonstrating how to make ANY View runnable without implementing IRunnable

using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

IApplication app = Application.Create ();
app.Init ();

// Example 1: Use extension method with result extraction
var textField = new TextField { Width = 40, Text = "Default text" };
textField.Title = "Enter your name";
textField.BorderStyle = LineStyle.Single;

var textRunnable = textField.AsRunnable (tf => tf.Text);
app.Run (textRunnable);

if (textRunnable.Result is { } name)
{
    MessageBox.Query ("Result", $"You entered: {name}", "OK");
}
else
{
    MessageBox.Query ("Result", "Canceled", "OK");
}
textRunnable.Dispose ();

// Example 2: Use IApplication.RunView() for one-liner
var selectedColor = app.RunView (
    new ColorPicker
    {
        Title = "Pick a Color",
        BorderStyle = LineStyle.Single
    },
    cp => cp.SelectedColor);

MessageBox.Query ("Result", $"Selected color: {selectedColor}", "OK");

// Example 3: FlagSelector with typed enum result
var flagSelector = new FlagSelector<SelectorStyles>
{
    Title = "Choose Styles",
    BorderStyle = LineStyle.Single
};

var flagsRunnable = flagSelector.AsRunnable (fs => fs.Value);
app.Run (flagsRunnable);

MessageBox.Query ("Result", $"Selected styles: {flagsRunnable.Result}", "OK");
flagsRunnable.Dispose ();

// Example 4: Any View without result extraction
var label = new Label
{
    Text = "Press Esc to continue...",
    X = Pos.Center (),
    Y = Pos.Center ()
};

var labelRunnable = label.AsRunnable ();
app.Run (labelRunnable);

// Can still access the wrapped view
MessageBox.Query ("Result", $"Label text was: {labelRunnable.WrappedView.Text}", "OK");
labelRunnable.Dispose ();

// Example 5: Complex custom View made runnable
var formView = CreateCustomForm ();
var formRunnable = formView.AsRunnable (ExtractFormData);

app.Run (formRunnable);

if (formRunnable.Result is { } formData)
{
    MessageBox.Query (
        "Form Results",
        $"Name: {formData.Name}\nAge: {formData.Age}\nAgreed: {formData.Agreed}",
        "OK");
}
formRunnable.Dispose ();

app.Shutdown ();

// Helper method to create a custom form
View CreateCustomForm ()
{
    var form = new View
    {
        Title = "User Information",
        BorderStyle = LineStyle.Single,
        Width = 50,
        Height = 10
    };

    var nameField = new TextField
    {
        Id = "nameField",
        X = 10,
        Y = 1,
        Width = 30
    };

    var ageField = new TextField
    {
        Id = "ageField",
        X = 10,
        Y = 3,
        Width = 10
    };

    var agreeCheckbox = new CheckBox
    {
        Id = "agreeCheckbox",
        Title = "I agree to terms",
        X = 10,
        Y = 5
    };

    var okButton = new Button
    {
        Title = "OK",
        X = Pos.Center (),
        Y = 7,
        IsDefault = true
    };

    okButton.Accepting += (s, e) =>
    {
        form.App?.RequestStop ();
        e.Handled = true;
    };

    form.Add (new Label { Text = "Name:", X = 2, Y = 1 });
    form.Add (nameField);
    form.Add (new Label { Text = "Age:", X = 2, Y = 3 });
    form.Add (ageField);
    form.Add (agreeCheckbox);
    form.Add (okButton);

    return form;
}

// Helper method to extract data from the custom form
FormData ExtractFormData (View form)
{
    var nameField = form.SubViews.FirstOrDefault (v => v.Id == "nameField") as TextField;
    var ageField = form.SubViews.FirstOrDefault (v => v.Id == "ageField") as TextField;
    var agreeCheckbox = form.SubViews.FirstOrDefault (v => v.Id == "agreeCheckbox") as CheckBox;

    return new FormData
    {
        Name = nameField?.Text ?? string.Empty,
        Age = int.TryParse (ageField?.Text, out int age) ? age : 0,
        Agreed = agreeCheckbox?.CheckedState == CheckState.Checked
    };
}

// Result type for custom form
record FormData
{
    public string Name { get; init; } = string.Empty;
    public int Age { get; init; }
    public bool Agreed { get; init; }
}
