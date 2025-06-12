// A simple Terminal.Gui example in C# - using C# 9.0 Top-level statements

// This is a simple example application.  For the full range of functionality
// see the UICatalog project

using Terminal.Gui.Configuration;
using Terminal.Gui.App;

// Override the default configuration for the application to use the Light theme
ConfigurationManager.RuntimeConfig = """{ "Theme": "Light" }""";
ConfigurationManager.Enable(ConfigLocations.All);

// As Run<T> is used to start the application, it will create an instance of ExampleWindow and run it without needing to explicitly call `Application.Init()`.
Application.Run<ExampleWindow> ().Dispose ();

// Before the application exits, reset Terminal.Gui for clean shutdown
Application.Shutdown ();

// To see this output on the screen it must be done after shutdown,
// which restores the previous screen.
Console.WriteLine ($@"Username: {ExampleWindow.UserName}");
