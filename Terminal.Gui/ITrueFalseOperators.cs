namespace Terminal.Gui;
/// <summary>
///     Defines a mechanism for defining when a type is considered to be equivalent to <see langword="true"/> or
///     <see langword="false"/>.
/// </summary>
/// <typeparam name="TSelf">The type for which truth is defined.</typeparam>
public interface ITrueFalseOperators<in TSelf> where TSelf : ITrueFalseOperators<TSelf>
{
  /// <summary>
  ///     Determines when an instance of a type is considered <see langword="false"/>.
  /// </summary>
  /// <param name="value">The value.</param>
  /// <returns>
  ///     <see langword="true"/>, if the value should be considered <see langword="false"/>;<br/>
  ///     <see langword="false"/>, otherwise.
  /// </returns>
  /// <remarks>
  ///     A return value of <see langword="true"/> means that the value is considered <see langword="false"/>.
  /// </remarks>
  static abstract bool operator false (TSelf value);

  /// <summary>
  ///     Determines when an instance of a type is considered <see langword="true"/>.
  /// </summary>
  /// <param name="value">The value.</param>
  /// <returns>
  ///     <see langword="true"/>, if the value should be considered <see langword="true"/>;<br/>
  ///     <see langword="false"/>, otherwise.
  /// </returns>
  /// <remarks>
  ///     A return value of <see langword="true"/> means that the value is considered <see langword="true"/>.
  /// </remarks>
  static abstract bool operator true (TSelf value);
}
