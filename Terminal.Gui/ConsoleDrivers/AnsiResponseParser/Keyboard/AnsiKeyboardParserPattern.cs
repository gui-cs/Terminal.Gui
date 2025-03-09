#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Base class for ANSI keyboard parsing patterns.
/// </summary>
public abstract class AnsiKeyboardParserPattern
{
    /// <summary>
    ///     Does this pattern dangerously overlap with other sequences
    ///     such that it should only be applied at the lsat second after
    ///     all other sequences have been tried.
    ///     <remarks>
    ///         When <see langword="true"/> this pattern will only be used
    ///         at <see cref="AnsiResponseParser.Release"/> time.
    ///     </remarks>
    /// </summary>
    public bool IsLastMinute { get; set; }

    /// <summary>
    ///     Returns <see langword="true"/> if <paramref name="input"/> is one
    ///     of the terminal sequences recognised by this class.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public abstract bool IsMatch (string input);

    private readonly string _name;

    /// <summary>
    ///     Creates a new instance of the class.
    /// </summary>
    protected AnsiKeyboardParserPattern () { _name = GetType ().Name; }

    /// <summary>
    ///     Returns the <see cref="Key"/> described by the escape sequence.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public Key? GetKey (string input)
    {
        Key? key = GetKeyImpl (input);
        Logging.Trace ($"{nameof (AnsiKeyboardParser)} interpreted {input} as {key} using {_name}");

        return key;
    }

    /// <summary>
    ///     When overriden in a derived class, returns the <see cref="Key"/>
    ///     that matches the input ansi escape sequence.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    protected abstract Key? GetKeyImpl (string input);
}
