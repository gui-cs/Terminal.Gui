#nullable enable
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
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
var ansi = false;
var addBorderFrame = false;
var live = false;
var queryKeyStrokes = false;

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
    else if (commandArgs [i] == "--live" || commandArgs [i] == "-l")
    {
        live = true;
    }
    else if (commandArgs [i] == "--keystrokes" || commandArgs [i] == "-k")
    {
        queryKeyStrokes = true;
    }
}

if (string.IsNullOrEmpty (viewName))
{
    Console.WriteLine (@"No view name specified. Use --view=ViewName to specify a view.");

    return;
}

// If --keystrokes, just query the view's demo keystrokes and exit
if (queryKeyStrokes)
{
    Type? type = ViewDemoWindow.ResolveViewType (viewName);

    if (type is null)
    {
        Console.Error.WriteLine ($"`{viewName}` type is not a valid Terminal.Gui View type.");
        Environment.Exit (1);

        return;
    }

    View? view = (View?)Activator.CreateInstance (type);

    if (view is IDesignable designable)
    {
        string? keystrokes = designable.GetDemoKeyStrokes ();
        Console.WriteLine (keystrokes ?? "");
    }
    else
    {
        Console.WriteLine ("");
    }

    return;
}

ViewDemoWindow.ViewName = viewName;
ViewDemoWindow.AddBorderFrame = addBorderFrame;
ViewDemoWindow.IsLiveMode = live;

IApplication app = Application.Create ();
app.Init (DriverRegistry.Names.ANSI);

if (live)
{
    // Live mode: run normally so tuirec can record the interaction.
    // Write a dot colored to match the agg monokai theme background (#272822 = RGB 39,40,34)
    // before TG renders, then pause 500ms. This creates 2 visually distinct frames for
    // tuirec's --trim without any visible preroll artifact.
    Console.Write ("\x1b[2J\x1b[H\x1b[38;2;39;40;34m.\x1b[0m");
    Console.Out.Flush ();
    Thread.Sleep (500);

    app.Driver!.SetScreenSize (80, 20);
    app.Run<ViewDemoWindow> ();
    app.Dispose ();
}
else
{
    // Original mode: stop after first iteration and capture output
    app.StopAfterFirstIteration = true;
    app.Driver!.Force16Colors = !ansi;
    app.Driver!.SetScreenSize (80, 20);

    var result = app.Run<ViewDemoWindow> ().GetResult<string> ();

    if (result is { })
    {
        Console.WriteLine (result);
        app.Dispose ();

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
}

// Defines a top-level window with border and title
internal class ViewDemoWindow : Runnable<string>
{
    public static string? ViewName { get; set; }
    public static bool IsLiveMode { get; set; }

    public ViewDemoWindow ()
    {
        // Limit the size of the window to 80x20, which works good for most views
        Width = 80;
        Height = 20;

        if (!IsLiveMode)
        {
            // Use only white on black for static HTML/ANSI capture
            SetScheme (new Scheme (new Attribute (ColorName16.White, ColorName16.Black)));
        }
        else
        {
            // In live mode, don't let child Accept bubble up and stop the app
            CommandsToBubbleUp = [];
        }

        BorderStyle = LineStyle.None;
    }

    public static bool AddBorderFrame { get; set; }

    /// <summary>
    ///     Resolves a view type name to its <see cref="Type"/>, handling generic types like "ListView`1".
    /// </summary>
    public static Type? ResolveViewType (string viewName)
    {
        // Try direct resolution first
        Type? type = Type.GetType ($"Terminal.Gui.Views.{viewName}, Terminal.Gui", false, true);

        if (type is not null)
        {
            return type;
        }

        // Search the assembly for types matching by name (handles generics)
        System.Reflection.Assembly asm = typeof (View).Assembly;

        return asm.GetTypes ()
                  .FirstOrDefault (t => t.IsClass
                                        && !t.IsAbstract
                                        && t.IsSubclassOf (typeof (View))
                                        && string.Equals (t.Name, viewName, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    protected override void OnIsRunningChanged (bool newIsRunning)
    {
        base.OnIsRunningChanged (newIsRunning);

        if (!newIsRunning)
        {
            return;
        }

        // Convert ViewName to type that's in the Terminal.Gui assembly:
        Type? type = ResolveViewType (ViewName!);

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
