// Example demonstrating the Prompt API for getting typed input from users

using System.Drawing;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.Resources;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Color = Terminal.Gui.Drawing.Color;

ConfigurationManager.Enable (ConfigLocations.All);
using IApplication app = Application.Create ().Init (DriverRegistry.Names.DOTNET);

// Create a main window to host the prompts
using Window mainWindow = new ();
mainWindow.Title = "Prompt API Examples (Esc to quit)";

// Add instructions
mainWindow.Add (new Label
{
    Text =
        "This example demonstrates various uses of the Prompt API.\nPress the buttons to try different prompt types.\nPress Esc to quit.",
    X = Pos.Center (),
    Y = 1
});

var buttonY = 6;

// Example 1: TextField with string result using auto-Text extraction
Button textFieldButton = new () { Title = "TextField (Auto-Text)", X = Pos.Center (), Y = buttonY++ };

textFieldButton.Accepting += (_, _) =>
                             {
                                 string? result = mainWindow.Prompt<TextField, string> (beginInitHandler: prompt =>
                                                                                        {
                                                                                            prompt.Title = textFieldButton.Title;
                                                                                            prompt.GetWrappedView ().Width = 40;
                                                                                            prompt.GetWrappedView ().Text = "Default name";
                                                                                        });

                                 MessageBox.Query (app, textFieldButton.Title, result is { } ? $"You entered: {result}" : "Canceled", Strings.btnOk);
                             };

mainWindow.Add (textFieldButton);

// Example 1: TextField with string result using auto-Text extraction
Button textViewButton = new () { Title = "TextView (Auto-Text)", X = Pos.Center (), Y = buttonY++ };

textViewButton.Accepting += (_, _) =>
                            {
                                string? result = mainWindow.Prompt<TextView, string> (beginInitHandler: prompt =>
                                                                                                         {
                                                                                                             prompt.Title = textViewButton.Title;
                                                                                                             prompt.GetWrappedView ().Text = "Some text\nis nice.";
                                                                                                             prompt.GetWrappedView ().Width = Dim.Fill (0, 40);
                                                                                                             prompt.GetWrappedView ().Height = Dim.Fill (0, 8);
                                                                                                         });

                                MessageBox.Query (app, textViewButton.Title, result is { } ? $"You entered: {result}" : "Canceled", Strings.btnOk);
                            };

mainWindow.Add (textViewButton);


// Example 2: DatePicker with DateTime result
Button datePickerButton = new () { Title = "DatePicker (Typed Result)", X = Pos.Center (), Y = buttonY++ };

datePickerButton.Accepting += (_, _) =>
                              {
                                  DateTime? result = mainWindow.Prompt<DatePicker, DateTime> (resultExtractor: dp => dp.Date,
                                                                                              beginInitHandler: prompt =>
                                                                                              {
                                                                                                  prompt.Title = "Select a Date";
                                                                                                  prompt.GetWrappedView ().Date = DateTime.Now;
                                                                                              });

                                  if (result is { } selectedDate)
                                  {
                                      MessageBox.Query (app, datePickerButton.Title, $"You selected: {selectedDate:yyyy-MM-dd}", Strings.btnOk);
                                  }
                                  else
                                  {
                                      MessageBox.Query (app, datePickerButton.Title, "Canceled", Strings.btnOk);
                                  }
                              };

mainWindow.Add (datePickerButton);

// Example 3: ColorPicker with Color result
Button colorPickerButton = new () { Title = "ColorPicker (Typed Result)", X = Pos.Center (), Y = buttonY++ };

colorPickerButton.Accepting += (_, _) =>
                               {
                                   Color? result = mainWindow.Prompt<ColorPicker, Color?> (input: null,
                                                                                          beginInitHandler: prompt =>
                                                                                          {
                                                                                              prompt.Title = "Pick a Color";
                                                                                          });

                                   if (result is { } selectedColor)
                                   {
                                       MessageBox.Query (app, colorPickerButton.Title, $"You selected: {selectedColor}", Strings.btnOk);
                                   }
                                   else
                                   {
                                       MessageBox.Query (app, colorPickerButton.Title, "Canceled", Strings.btnOk);
                                   }
                               };

mainWindow.Add (colorPickerButton);

// Example 4: ColorPicker with auto-Text extraction
Button colorTextButton = new () { Title = "ColorPicker (Auto-Text)", X = Pos.Center (), Y = buttonY++ };

colorTextButton.Accepting += (_, _) =>
                             {
                                 string? result = mainWindow.Prompt<ColorPicker, string> (beginInitHandler: prompt =>
                                                                                          {
                                                                                              prompt.Title = "Pick a Color (as text)";
                                                                                              prompt.GetWrappedView ().SelectedColor = Color.Red;
                                                                                          });

                                 MessageBox.Query (app, colorTextButton.Title, result is { } ? $"Color as text: {result}" : "Canceled", Strings.btnOk);
                             };

mainWindow.Add (colorTextButton);

// Example 5: Pre-created view
Button preCreatedButton = new () { Title = "Pre-Created TextField", X = Pos.Center (), Y = buttonY++ };

preCreatedButton.Accepting += (_, _) =>
                              {
                                  // Pre-create and configure the view
                                  TextField textField = new () { Width = 50, Text = "Pre-configured text" };

                                  string? result = mainWindow.Prompt<TextField, string?> (textField,
                                                                                          field => field.Text,
                                                                                          beginInitHandler: prompt =>
                                                                                          {
                                                                                              prompt.Title = preCreatedButton.Title;
                                                                                              prompt.BorderStyle = LineStyle.Rounded;
                                                                                          });

                                  if (result is { })
                                  {
                                      MessageBox.Query (app, preCreatedButton.Title, $"You entered: {result}", Strings.btnOk);
                                  }
                              };

mainWindow.Add (preCreatedButton);

// Example 6: Custom form with complex result extraction
Button customFormButton = new () { Title = "Custom Form", X = Pos.Center (), Y = buttonY++ };

customFormButton.Accepting += (_, _) =>
                              {
                                  View formView = CreateCustomForm ();

                                  FormData? result = mainWindow.Prompt (formView,
                                                                        ExtractFormData,
                                                                        beginInitHandler: prompt => { prompt.Title = "User Information Form"; });

                                  if (result is { })
                                  {
                                      MessageBox.Query (app,
                                                        customFormButton.Title,
                                                        $"Name: {result.Name}\nAge: {result.Age}\nAgreed: {result.Agreed}",
                                                        Strings.btnOk);
                                  }
                                  else
                                  {
                                      MessageBox.Query (app, "Form canceled", Strings.btnOk);
                                  }
                              };

mainWindow.Add (customFormButton);

// Example 7: FlagSelector with enum result
Button flagSelectorButton = new () { Title = "FlagSelector (Enum Result)", X = Pos.Center (), Y = buttonY++ };

flagSelectorButton.Accepting += (_, _) =>
                                {
                                    SelectorStyles? result =
                                        mainWindow.Prompt<FlagSelector<SelectorStyles>, SelectorStyles> (resultExtractor: fs => fs.Value!.Value,
                                                                                                         beginInitHandler: prompt =>
                                                                                                         {
                                                                                                             prompt.Title = "Choose Selector Styles";
                                                                                                         });

                                    if (result is { } styles)
                                    {
                                        MessageBox.Query (app, flagSelectorButton.Title, $"Selected styles: {styles}", Strings.btnOk);
                                    }
                                    else
                                    {
                                        MessageBox.Query (app, flagSelectorButton.Title, "Canceled", Strings.btnOk);
                                    }
                                };

mainWindow.Add (flagSelectorButton);

// Add a quit button
Button quitButton = new () { Title = "Quit", X = Pos.Center (), Y = Pos.AnchorEnd () };

quitButton.Accepting += (_, _) => app.RequestStop ();
mainWindow.Add (quitButton);

app.Run (mainWindow);

return;

// Helper method to create a custom form
View CreateCustomForm ()
{
    View form = new () { Width = 50, Height = 10 };

    TextField nameField = new () { Id = "nameField", X = 10, Y = 1, Width = 30 };

    TextField ageField = new () { Id = "ageField", X = 10, Y = 3, Width = 10 };

    CheckBox agreeCheckbox = new () { Id = "agreeCheckbox", Title = "I agree to terms", X = 10, Y = 5 };

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
internal record FormData
{
    public string Name { get; init; } = string.Empty;
    public int Age { get; init; }
    public bool Agreed { get; init; }
}
