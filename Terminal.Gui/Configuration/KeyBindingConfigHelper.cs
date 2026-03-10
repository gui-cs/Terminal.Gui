namespace Terminal.Gui.Configuration;

/// <summary>
///     Provides helper methods for applying user-configurable key bindings to views.
/// </summary>
/// <remarks>
///     <para>
///         Views expose <see langword="public static"/> <see cref="Dictionary{TKey,TValue}"/> properties decorated with
///         <see cref="ConfigurationPropertyAttribute"/> so that key bindings can be configured via <em>config.json</em>.
///         Each dictionary maps a <see cref="Command"/> name (string) to an array of key strings parseable by
///         <see cref="Key.TryParse"/>.
///     </para>
///     <para>
///         Call <see cref="Apply"/> from a view's <c>CreateCommandsAndBindings</c> method instead of individual
///         <c>KeyBindings.Add</c> calls.
///     </para>
/// </remarks>
internal static class KeyBindingConfigHelper
{
    /// <summary>
    ///     Applies key bindings from configurable dictionaries to a view.
    /// </summary>
    /// <param name="view">The view to apply bindings to.</param>
    /// <param name="baseBindings">
    ///     Command-to-keys map applied on all platforms. Each key is a <see cref="Command"/> name; each value is an
    ///     array of key strings parseable by <see cref="Key.TryParse"/>. <see langword="null"/> is silently skipped.
    /// </param>
    /// <param name="platformBindings">
    ///     Additional command-to-keys map applied only on non-Windows platforms. Entries are appended to (not replacing)
    ///     the base bindings. <see langword="null"/> is silently skipped.
    /// </param>
    internal static void Apply (View view, Dictionary<string, string []>? baseBindings, Dictionary<string, string []>? platformBindings = null)
    {
        applyDict (baseBindings);

        if (!OperatingSystem.IsWindows ())
        {
            applyDict (platformBindings);
        }

        void applyDict (Dictionary<string, string []>? dict)
        {
            if (dict is null)
            {
                return;
            }

            foreach ((string commandName, string [] keyStrings) in dict)
            {
                if (!Enum.TryParse (commandName, out Command command))
                {
                    continue;
                }

                foreach (string keyString in keyStrings)
                {
                    if (!Key.TryParse (keyString, out Key key))
                    {
                        continue;
                    }

                    if (view.KeyBindings.TryGet (key, out _))
                    {
                        continue;
                    }

                    view.KeyBindings.Add (key, command);
                }
            }
        }
    }
}
