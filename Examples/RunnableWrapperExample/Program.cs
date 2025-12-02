#nullable enable
// Example demonstrating how to make ANY View runnable without implementing IRunnable

using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// Example metadata
[assembly: Terminal.Gui.Examples.ExampleMetadata ("Runnable Wrapper Example", "Shows how to wrap any View to make it runnable without implementing IRunnable")]
[assembly: Terminal.Gui.Examples.ExampleCategory ("API Patterns")]
[assembly: Terminal.Gui.Examples.ExampleCategory ("Views")]
[assembly: Terminal.Gui.Examples.ExampleDemoKeyStrokes (KeyStrokes = ["SetDelay:200", "t", "e", "s", "t", "Esc"], Order = 1)]
[assembly: Terminal.Gui.Examples.ExampleDemoKeyStrokes (KeyStrokes = ["SetDelay:200", "Enter", "Esc"], Order = 2)]
[assembly: Terminal.Gui.Examples.ExampleDemoKeyStrokes (KeyStrokes = ["SetDelay:200", "Enter", "Esc"], Order = 3)]
[assembly: Terminal.Gui.Examples.ExampleDemoKeyStrokes (KeyStrokes = ["SetDelay:200", "Enter", "Esc"], Order = 4)]
[assembly: Terminal.Gui.Examples.ExampleDemoKeyStrokes (KeyStrokes = ["SetDelay:200", "Enter", "Esc"], Order = 5)]

IApplication app = Application.Create (example: true);
app.Init ();

// Example 1: Use extension method with result extraction
var textField = new TextField { Width = 40, Text = "Default text" };
textField.Title = "Enter your name";
textField.BorderStyle = LineStyle.Single;

RunnableWrapper<TextField, string> textRunnable = textField.AsRunnable (tf => tf.Text);
app.Run (textRunnable);

if (textRunnable.Result is { } name)
{
    MessageBox.Query (app, "Result", $"You entered: {name}", "OK");
}
else
{
    MessageBox.Query (app, "Result", "Canceled", "OK");
}

textRunnable.Dispose ();

// Example 2: Use IApplication.RunView() for one-liner
Color selectedColor = app.RunView (
                                   new ColorPicker
                                   {
                                       Title = "Pick a Color",
                                       BorderStyle = LineStyle.Single
                                   },
                                   cp => cp.SelectedColor);

MessageBox.Query (app, "Result", $"Selected color: {selectedColor}", "OK");

// Example 3: FlagSelector with typed enum result
FlagSelector<SelectorStyles> flagSelector = new()
{
    Title = "Choose Styles",
    BorderStyle = LineStyle.Single
};

RunnableWrapper<FlagSelector<SelectorStyles>, SelectorStyles?> flagsRunnable = flagSelector.AsRunnable (fs => fs.Value);
app.Run (flagsRunnable);

MessageBox.Query (app, "Result", $"Selected styles: {flagsRunnable.Result}", "OK");
flagsRunnable.Dispose ();

// Example 4: Any View without result extraction
var label = new Label
{
    Text = "Press Esc to continue...",
    X = Pos.Center (),
    Y = Pos.Center ()
};

RunnableWrapper<Label, object> labelRunnable = label.AsRunnable ();
app.Run (labelRunnable);

// Can still access the wrapped view
MessageBox.Query (app, "Result", $"Label text was: {labelRunnable.WrappedView.Text}", "OK");
labelRunnable.Dispose ();

// Example 5: Complex custom View made runnable
View formView = CreateCustomForm ();
RunnableWrapper<View, FormData> formRunnable = formView.AsRunnable (ExtractFormData);

app.Run (formRunnable);

if (formRunnable.Result is { } formData)
{
    MessageBox.Query (
                      app,
                      "Form Results",
                      $"Name: {formData.Name}\nAge: {formData.Age}\nAgreed: {formData.Agreed}",
                      "OK");
}

formRunnable.Dispose ();

app.Dispose ();

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

    return new()
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
