#nullable enable
using System.Diagnostics;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

ConfigurationManager.RuntimeConfig = """
{
    "Themes": [
        {   
            "Default": {
                "Window.DefaultShadow": "None",
                "CheckBox.DefaultHighlightStyle": "None",
                "Dialog.DefaultShadow": "None",
                "Button.DefaultShadow": "None",
                "Menuv2.DefaultBorderStyle": "Single"
            }
        }
    ]
}
""";

ConfigurationManager.Enable (ConfigLocations.Runtime);

// Get the view name and output file from commandline
string? viewName = null;
string? outputFile = null;

var commandArgs = Environment.GetCommandLineArgs();
for (int i = 0; i < commandArgs.Length; i++)
{
    if (commandArgs[i].StartsWith("--view=", StringComparison.OrdinalIgnoreCase))
        viewName = commandArgs[i].Substring("--view=".Length);
    else if (commandArgs[i] == "--view" && i + 1 < commandArgs.Length)
        viewName = commandArgs[i + 1];
    else if (commandArgs[i].StartsWith("--output=", StringComparison.OrdinalIgnoreCase))
        outputFile = commandArgs[i].Substring("--output=".Length);
    else if (commandArgs[i] == "--output" && i + 1 < commandArgs.Length)
        outputFile = commandArgs[i + 1];
}

if (string.IsNullOrEmpty(viewName))
{
    Console.WriteLine ("No view name specified. Use --view=ViewName to specify a view.");
    return;
}

ViewDemoWindow.ViewName = viewName;

Application.Force16Colors = true;
Application.EndAfterFirstIteration = true;
var demoWindow = Application.Run<ViewDemoWindow> ();
string? output = demoWindow.Output;
demoWindow.Dispose ();

// Before the application exits, reset Terminal.Gui for clean shutdown
Application.Shutdown ();

if (output is null)
{
    var message = "No output was generated.";
    if (!string.IsNullOrEmpty(outputFile))
        File.WriteAllText(outputFile, message);
    else
        Console.WriteLine(message);
    return;
}

// Write to file or console
if (!string.IsNullOrEmpty(outputFile))
{
    File.WriteAllText(outputFile, output);
}
else
{
    Console.WriteLine(output);
}

// Defines a top-level window with border and title
public class ViewDemoWindow : Window
{
    public static string? ViewName { get; set; }
    public string? Output { get; set; }

    public ViewDemoWindow ()
    {
        SetScheme(new Scheme(new Terminal.Gui.Drawing.Attribute(ColorName16.White, ColorName16.Black)));
        BorderStyle = LineStyle.None;

        // Convert ViewName to type that's in the Terminal.Gui assembly:
        Type? type = Type.GetType ($"Terminal.Gui.Views.{ViewName!}, Terminal.Gui", throwOnError: false, ignoreCase: true);


        View? view = CreateView (type!);

        base.Add (view);

        Application.Iteration += (sender, args) =>
                                {
                                    Application.LayoutAndDraw ();
                                    Output = Application.ToString ();
                                };
    }

    private View? CreateView (Type type)
    {
        // If we are to create a generic Type
        if (type.IsGenericType)
        {
            // For each of the <T> arguments
            List<Type> typeArguments = new ();

            // use <object> or the original type if applicable
            foreach (Type arg in type.GetGenericArguments ())
            {
                if (arg.IsValueType && Nullable.GetUnderlyingType (arg) == null)
                {
                    typeArguments.Add (arg);
                }
                else
                {
                    typeArguments.Add (typeof (object));
                }
            }

            // And change what type we are instantiating from MyClass<T> to MyClass<object> or MyClass<T>
            type = type.MakeGenericType (typeArguments.ToArray ());
        }

        // Ensure the type does not contain any generic parameters
        if (type.ContainsGenericParameters)
        {
            Logging.Warning ($"Cannot create an instance of {type} because it contains generic parameters.");
            //throw new ArgumentException ($"Cannot create an instance of {type} because it contains generic parameters.");
            return null;
        }

        // Instantiate view
        var view = (View)Activator.CreateInstance (type)!;

        if (view is IDesignable designable)
        {
            string settingsEditorDemoText = "Demo Text";
            designable.EnableForDesign (ref settingsEditorDemoText);
        }
        else
        {
            view.Text = "Demo Text";
            view.Title = "_Demo Title";
        }

        return view;

    }
}
