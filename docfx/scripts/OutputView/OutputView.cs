#nullable enable
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Attribute = Terminal.Gui.Drawing.Attribute;

// Disable all shadows and highlights
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

string [] commandArgs = Environment.GetCommandLineArgs ();

for (var i = 0; i < commandArgs.Length; i++)
{
    if (commandArgs [i].StartsWith ("--view=", StringComparison.OrdinalIgnoreCase))
    {
        viewName = commandArgs [i] ["--view=".Length..];
    }
    else if (commandArgs [i] == "--view" && i + 1 < commandArgs.Length)
    {
        viewName = commandArgs [i + 1];
    }
    else if (commandArgs [i].StartsWith ("--output=", StringComparison.OrdinalIgnoreCase))
    {
        outputFile = commandArgs [i] ["--output=".Length..];
    }
    else if (commandArgs [i] == "--output" && i + 1 < commandArgs.Length)
    {
        outputFile = commandArgs [i + 1];
    }
}

if (string.IsNullOrEmpty (viewName))
{
    Console.WriteLine (@"No view name specified. Use --view=ViewName to specify a view.");

    return;
}

ViewDemoWindow.ViewName = viewName;

// Force 16 colors and end after first iteration
Application.EndAfterFirstIteration = true;

var demoWindow = Application.Run<ViewDemoWindow> ();
string? output = demoWindow.Output?.Trim ();
demoWindow.Dispose ();

// Before the application exits, reset Terminal.Gui for clean shutdown
Application.Shutdown ();

if (output is null)
{
    Console.WriteLine (@"No output was generated.");

    return;
}

// Write to file or console
if (!string.IsNullOrEmpty (outputFile))
{
    File.WriteAllText (outputFile, output);
}
else
{
    Console.WriteLine (output);
}

// Defines a top-level window with border and title
public class ViewDemoWindow : Window
{
    public static string? ViewName { get; set; }
    public string? Output { get; set; }

    public ViewDemoWindow ()
    {
        // Limit the size of the window to 50x20, which works good for most views
        Width = 50;
        Height = 20;

        // Use only white on black
        SetScheme (new (new Attribute (ColorName16.White, ColorName16.Black)));
        BorderStyle = LineStyle.None;

        // Convert ViewName to type that's in the Terminal.Gui assembly:
        var type = Type.GetType ($"Terminal.Gui.Views.{ViewName!}, Terminal.Gui", false, true);

        if (type is null)
        {
            Console.WriteLine (@$"View {ViewName} type is invalid.");

            return;
        }

        // Create the view
        View? view = CreateView (type!);

        if (view is null)
        {
            Console.WriteLine (@$"View {ViewName} could not be created.");

            return;
        }

        // Initialize the view
        view.Initialized += ViewInitialized;

        base.Add (view);

        // In normal apps, each iteration would call Application.LayoutAndDraw()
        // but since we set Application.EndAfterFirstIteration = true, we need to
        // call it manually here and capture the output
        Application.Iteration += (sender, args) =>
                                 {
                                     Application.LayoutAndDraw ();
                                     Output = Application.ToString ();
                                 };
    }

    private static View? CreateView (Type type)
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
            Console.WriteLine (@$"Cannot create an instance of {type} because it contains generic parameters.");

            return null;
        }

        // Instantiate view
        var view = (View)Activator.CreateInstance (type)!;

        if (view is IDesignable designable)
        {
            var settingsEditorDemoText = "Demo Text";
            designable.EnableForDesign (ref settingsEditorDemoText);
        }
        else
        {
            view.Text = "Demo Text";
            view.Title = "_Demo Title";
        }

        return view;
    }

    private static void ViewInitialized (object? sender, EventArgs e)
    {
        if (sender is not View view)
        {
            return;
        }

        if (view.Width == Dim.Absolute (0) || view.Width is null)
        {
            view.Width = Dim.Fill ();
        }

        if (view.Height == Dim.Absolute (0) || view.Height is null)
        {
            view.Height = Dim.Fill ();
        }

        view.X = 0;
        view.Y = 0;
    }
}
