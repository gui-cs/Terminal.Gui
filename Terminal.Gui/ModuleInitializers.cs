using System.Runtime.CompilerServices;
using Terminal.Gui.Configuration;

#pragma warning disable CS0618 // Obsolete - ConfigurationManager still used internally during transition

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
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void InitializeConfigurationManager ()
    {
        // Initialize ConfigurationManager to ensure all configuration properties
        // are loaded deterministically
        ConfigurationManager.Initialize ();

        // After CM initialization, also apply MEC-based settings.
        // MEC values (from the same config files) take precedence over CM defaults.
        TuiConfigurationBuilder mecBuilder = new ();
        mecBuilder.ApplyToStaticFacades ();

        // Note: We're only initializing the config properties structure here,
        // not loading settings from files. That still happens during Application.Init()
        // via InitializeConfigurationManagement()
    }
}

#pragma warning restore CS0618
