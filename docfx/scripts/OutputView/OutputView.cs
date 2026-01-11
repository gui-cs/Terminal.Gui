#nullable enable
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
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
                                                     "Dialog.DefaultShadow": "None",
                                                     "Button.DefaultShadow": "None",
                                                     "Menu.DefaultBorderStyle": "Single"
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
bool ansi = false;
bool addBorderFrame = false;

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
    else if (commandArgs [i] == "--frame" || commandArgs [i] == "-f")
    {
        addBorderFrame = true;
    }
    else if (commandArgs [i] == "--ansi" || commandArgs [i] == "-a")
    {
        ansi = true;
    }
}

if (string.IsNullOrEmpty (viewName))
{
    Console.WriteLine (@"No view name specified. Use --view=ViewName to specify a view.");

    return;
}

ViewDemoWindow.ViewName = viewName;
ViewDemoWindow.AddBorderFrame = addBorderFrame;

IApplication app = Application.Create ();
app.Init (DriverRegistry.Names.ANSI);

// Force 16 colors and end after first iteration
app.StopAfterFirstIteration = true;
app.Driver!.Force16Colors = !ansi;
app.Driver!.SetScreenSize (80, 20);

string? result = app.Run<ViewDemoWindow> ().GetResult<string> ();

if (result is { })
{
    Console.WriteLine (result);

    return;
}

// Run it again, since it set the Screen size to just fit
app.Run<ViewDemoWindow> ().GetResult<string> ();

string output = ansi ? app.Driver.ToAnsi () : app.ToString ().Trim ();
app.Dispose ();

if (string.IsNullOrEmpty (output))
{
    Console.WriteLine (@"No output was generated.");

    return;
}

if (ansi)
{
    output = AnsiConsoleToHtml.AnsiConsole.ToHtml (output);
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
internal class ViewDemoWindow : Runnable<string>
{
    public static string? ViewName { get; set; }

    public ViewDemoWindow ()
    {
        // Limit the size of the window to 80x20, which works good for most views
        Width = 80;
        Height = 20;

        // Use only white on black
        SetScheme (new (new Attribute (ColorName16.White, ColorName16.Black)));
        BorderStyle = LineStyle.None;
    }

    public static bool AddBorderFrame { get; set; }

    /// <inheritdoc />
    protected override void OnIsRunningChanged (bool newIsRunning)
    {
        base.OnIsRunningChanged (newIsRunning);

        if (!newIsRunning)
        {
            return;
        }

        // Convert ViewName to type that's in the Terminal.Gui assembly:
        Type? type = Type.GetType ($"Terminal.Gui.Views.{ViewName!}, Terminal.Gui", false, true);

        if (type is null)
        {
            Result = @$"`{ViewName}` type is not a valid Terminal.Gui View type.";

            return;
        }

        // Create the view
        View? view = CreateView (type);

        if (view is null)
        {
            Result = @$"`{ViewName}` could not be created.";

            return;
        }

        // Initialize the view
        view.Initialized += ViewInitialized;

        Add (view);

        Layout ();
        App?.Driver?.SetScreenSize (view.Frame.Width, view.Frame.Height);
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
            var demoText = "This is some demo text.";
            designable.EnableForDesign (ref demoText);
        }
        else
        {
            view.Text = "This is some demo text.";
        }
        //view.Title = $"View: {type.Name}";

        return view;
    }

    private static void ViewInitialized (object? sender, EventArgs e)
    {
        if (sender is not View view)
        {
            return;
        }

        if (view.Width == Dim.Absolute (0))
        {
            view.Width = Dim.Fill ();
        }

        if (view.Height == Dim.Absolute (0))
        {
            view.Height = Dim.Fill ();
        }

        view.X = 0;
        view.Y = 0;

        if (AddBorderFrame && view.BorderStyle == LineStyle.None)
        {
            view.BorderStyle = LineStyle.Dotted;
        }
    }
}
