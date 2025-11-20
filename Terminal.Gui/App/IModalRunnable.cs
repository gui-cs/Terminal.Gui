namespace Terminal.Gui.App;

/// <summary>
/// Defines a runnable view that captures exclusive input (modal behavior) and returns a result.
/// </summary>
/// <typeparam name="TResult">
/// The type of result data returned when the modal is accepted.
/// Common types: <see cref="int"/> for button indices, <see cref="string"/> for file paths,
/// custom types for complex form data.
/// </typeparam>
/// <remarks>
/// <para>
/// Modal runnables block execution of <see cref="IApplication.Run"/> until stopped,
/// capture all keyboard and mouse input exclusively, and typically have elevated Z-order.
/// </para>
/// <para>
/// When <see cref="Result"/> is <c>null</c>, the modal was stopped without being accepted
/// (e.g., ESC key pressed, window closed). When non-<c>null</c>, it contains the result data
/// that was extracted from the Accept command before the modal's subviews were disposed.
/// </para>
/// <para>
/// Common implementations include <see cref="Dialog"/>, <see cref="MessageBox"/>, and <see cref="Wizard"/>.
/// </para>
/// <para>
/// For modals that need to provide additional context or data upon completion, populate the result
/// before setting it. By default (except for <see cref="Button"/>s with <see cref="Button.IsDefault"/> set),
/// if a modal does not handle the Accept command, <see cref="Result"/> will be null (canceled).
/// </para>
/// </remarks>
public interface IModalRunnable<TResult> : IRunnable
{
    /// <summary>
    /// Gets the result data from the modal operation, or <c>null</c> if not accepted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementations should set this property when the modal is accepted (e.g., OK button clicked,
    /// file selected). The result should be extracted from the modal's state before views are disposed.
    /// </para>
    /// <para>
    /// <c>null</c> indicates the modal was stopped without accepting (ESC key, cancel button, close without action).
    /// Non-<c>null</c> contains the type-safe result data.
    /// </para>
    /// <para>
    /// For example:
    /// - <see cref="Dialog"/>: Returns button index (int) or custom result
    /// - <see cref="MessageBox"/>: Returns button index (int)
    /// - <see cref="FileDialog"/>: Returns selected file path (string)
    /// </para>
    /// </remarks>
    TResult? Result { get; set; }
}
