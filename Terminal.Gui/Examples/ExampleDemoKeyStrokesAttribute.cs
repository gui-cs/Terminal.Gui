namespace Terminal.Gui.Examples;

/// <summary>
///     Defines keystrokes to be automatically injected when the example is run in demo or test mode.
///     Apply this attribute to an assembly to specify automated input sequences for demonstration or testing purposes.
/// </summary>
/// <remarks>
///     <para>
///         Multiple instances of this attribute can be applied to a single assembly to define a sequence
///         of keystroke injections. The <see cref="Order"/> property controls the execution sequence.
///     </para>
///     <para>
///         Keystrokes can include special "SetDelay:nnn" entries to change the delay between subsequent keys.
///         The default delay is 100ms. For example: KeyStrokes = ["SetDelay:500", "Enter", "SetDelay:100", "Tab"]
///     </para>
/// </remarks>
/// <example>
///     <code>
///     [assembly: ExampleDemoKeyStrokes(RepeatKey = "CursorDown", RepeatCount = 5, Order = 1)]
///     [assembly: ExampleDemoKeyStrokes(KeyStrokes = ["SetDelay:500", "Enter", "SetDelay:100", "Esc"], Order = 2)]
///     </code>
/// </example>
[AttributeUsage (AttributeTargets.Assembly, AllowMultiple = true)]
public class ExampleDemoKeyStrokesAttribute : System.Attribute
{
    /// <summary>
    ///     Gets or sets an array of keystroke names to inject.
    ///     Each string should be a valid key name that can be parsed by <see cref="Input.Key.TryParse"/>,
    ///     or a special "SetDelay:nnn" command to change the delay between subsequent keys.
    /// </summary>
    public string []? KeyStrokes { get; set; }

    /// <summary>
    ///     Gets or sets the name of a single key to repeat multiple times.
    ///     This is a convenience for repeating the same keystroke.
    /// </summary>
    public string? RepeatKey { get; set; }

    /// <summary>
    ///     Gets or sets the number of times to repeat <see cref="RepeatKey"/>.
    ///     Only used when <see cref="RepeatKey"/> is specified.
    /// </summary>
    public int RepeatCount { get; set; } = 1;

    /// <summary>
    ///     Gets or sets the order in which this keystroke sequence should be executed
    ///     relative to other <see cref="ExampleDemoKeyStrokesAttribute"/> instances.
    /// </summary>
    public int Order { get; set; } = 0;
}
