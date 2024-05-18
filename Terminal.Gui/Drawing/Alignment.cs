namespace Terminal.Gui;

/// <summary>
///     Determines the position of items when arranged in a container.
/// </summary>
public enum Alignment
{
    /// <summary>
    ///     The items will be aligned to the start (left or top) of the container.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the container is smaller than the total size of the items, the end items will be clipped (their locations
    ///         will be greater than the container size).
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <c>
    ///         |111 2222 33333    |
    ///     </c>
    /// </example>
    Start = 0,

    /// <summary>
    ///     The items will be aligned to the end (right or bottom) of the container.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the container is smaller than the total size of the items, the start items will be clipped (their locations
    ///         will be negative).
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <c>
    ///         |    111 2222 33333|
    ///     </c>
    /// </example>
    End,

    /// <summary>
    ///     Center in the available space.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     If centering is not possible, the group will be left-aligned.
    ///     </para>
    ///     <para>
    ///         Extra space will be distributed between the items, biased towards the left.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <c>
    ///         |  111 2222 33333  |
    ///     </c>
    /// </example>
    Center,

    /// <summary>
    ///     The items will fill the available space.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Extra space will be distributed between the items, biased towards the end.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <c>
    ///        |111  2222    33333|
    ///     </c>
    /// </example>
    Fill,

    /// <summary>
    ///     The first item will be aligned to the start and the remaining will aligned to the end.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the container is smaller than the total size of the items, the end items will be clipped (their locations
    ///         will be greater than the container size).
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <c>
    ///         |111     2222 33333|
    ///     </c>
    /// </example>
    FirstStartRestEnd,

    /// <summary>
    ///     The last item will be aligned to the end and the remaining will aligned to the start.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the container is smaller than the total size of the items, the start items will be clipped (their locations
    ///         will be negative).
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <c>
    ///         |111 2222      33333|
    ///     </c>
    /// </example>
    LastEndRestStart
}