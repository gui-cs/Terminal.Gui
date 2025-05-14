using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui;

/// <summary>
///     Contains module initializers that run when the Terminal.Gui assembly is loaded.
/// </summary>
internal static class ModuleInitializers
{
    /// <summary>
    /// Initializes the ConfigurationManager when the Terminal.Gui assembly is loaded.
    /// Ensures configuration properties are loaded deterministically before any part
    /// of the library is used.
    /// </summary>
    [RequiresUnreferencedCode ("AOT")]
#pragma warning disable CA2255
    [ModuleInitializer]
    [RequiresDynamicCode ("Calls Terminal.Gui.ConfigurationManager.Initialize()")]
#pragma warning restore CA2255
    internal static void InitializeConfigurationManager ()
    {
        // Initialize ConfigurationManager to ensure all configuration properties
        // are loaded deterministically
        ConfigurationManager.Initialize ();

        // Note: We're only initializing the config properties structure here,
        // not loading settings from files. That still happens during Application.Init()
        // via InitializeConfigurationManagement()
    }
}
