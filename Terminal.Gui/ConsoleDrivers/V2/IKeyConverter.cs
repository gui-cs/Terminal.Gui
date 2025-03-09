namespace Terminal.Gui;

/// <summary>
///     Interface for subcomponent of a <see cref="InputProcessor{T}"/> which
///     can translate the raw console input type T (which typically varies by
///     driver) to the shared Terminal.Gui <see cref="Key"/> class.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IKeyConverter<in T>
{
    /// <summary>
    ///     Converts the native keyboard class read from console into
    ///     the shared <see cref="Key"/> class used by Terminal.Gui views.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    Key ToKey (T value);
}
