#nullable enable
namespace Terminal.Gui;

/// <summary>
///     The exception that is thrown when a <see cref="ConfigurationManager"/> API is called but the configuration manager is not enabled.
/// </summary>
public class ConfigurationManagerNotEnabledException : Exception
{ }
