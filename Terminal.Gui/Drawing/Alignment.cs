namespace Terminal.Gui;

/// <summary>
///     Controls how the <see cref="Aligner"/> aligns items within a container.
/// </summary>
public enum Alignment
{
    /// <summary>
    ///     The items will be aligned to the left.
    ///     Set <see cref="Aligner.SpaceBetweenItems"/> to <see langword="true"/> to ensure at least one space between
    ///     each item.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the container is smaller than the total size of the items, the right items will be clipped (their locations
    ///         will be greater than the container size).
    ///     </para>
    ///     <para>
    ///         <see cref="Left"/> and <see cref="Top"/> have equivalent behavior.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <c>
    ///         111 2222 33333
    ///     </c>
    /// </example>
    Left = 0,

    /// <summary>
    ///     The items will be aligned to the top.
    ///     Set <see cref="Aligner.SpaceBetweenItems"/> to <see langword="true"/> to ensure at least one line between
    ///     each item.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the container is smaller than the total size of the items, the bottom items will be clipped (their locations
    ///         will be greater than the container size).
    ///     </para>
    ///     <para>
    ///         <see cref="Left"/> and <see cref="Top"/> have equivalent behavior.
    ///     </para>
    /// </remarks>
    Top,

    /// <summary>
    ///     The items will be aligned to the right.
    ///     Set <see cref="Aligner.SpaceBetweenItems"/> to <see langword="true"/> to ensure at least one space between
    ///     each item.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the container is smaller than the total size of the items, the left items will be clipped (their locations
    ///         will be negative).
    ///     </para>
    ///     <para>
    ///         <see cref="Right"/> and <see cref="Bottom"/> have equivalent behavior.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <c>
    ///         111 2222 33333
    ///     </c>
    /// </example>
    Right,

    /// <summary>
    ///     The items will be aligned to the bottom.
    ///     Set <see cref="Aligner.SpaceBetweenItems"/> to <see langword="true"/> to ensure at least one line between
    ///     each item.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the container is smaller than the total size of the items, the top items will be clipped (their locations
    ///         will be negative).
    ///     </para>
    ///     <para>
    ///         <see cref="Right"/> and <see cref="Bottom"/> have equivalent behavior.
    ///     </para>
    /// </remarks>
    Bottom,

    /// <summary>
    ///     The group will be centered in the container.
    ///     If centering is not possible, the group will be left-aligned.
    ///     Set <see cref="Aligner.SpaceBetweenItems"/> to <see langword="true"/> to ensure at least one space between
    ///     each item.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Extra space will be distributed between the items, biased towards the left.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <c>
    ///         111 2222 33333
    ///     </c>
    /// </example>
    Centered,

    /// <summary>
    ///     The items will be justified. Space will be added between the items such that the first item
    ///     is at the start and the right side of the last item against the end.
    ///     Set <see cref="Aligner.SpaceBetweenItems"/> to <see langword="true"/> to ensure at least one space between
    ///     each item.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Extra space will be distributed between the items, biased towards the left.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <c>
    ///         111    2222     33333
    ///     </c>
    /// </example>
    Justified,

    /// <summary>
    ///     The first item will be aligned to the left and the remaining will aligned to the right.
    ///     Set <see cref="Aligner.SpaceBetweenItems"/> to <see langword="true"/> to ensure at least one space between
    ///     each item.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the container is smaller than the total size of the items, the right items will be clipped (their locations
    ///         will be greater than the container size).
    ///     </para>
    ///     <para>
    ///         <see cref="FirstLeftRestRight"/> and <see cref="FirstTopRestBottom"/> have equivalent behavior.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <c>
    ///         111        2222 33333
    ///     </c>
    /// </example>
    FirstLeftRestRight,

    /// <summary>
    ///     The first item will be aligned to the top and the remaining will aligned to the bottom.
    ///     Set <see cref="Aligner.SpaceBetweenItems"/> to <see langword="true"/> to ensure at least one line between
    ///     each item.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the container is smaller than the total size of the items, the bottom items will be clipped (their locations
    ///         will be greater than the container size).
    ///     </para>
    ///     <para>
    ///         <see cref="FirstLeftRestRight"/> and <see cref="FirstTopRestBottom"/> have equivalent behavior.
    ///     </para>
    /// </remarks>
    FirstTopRestBottom,

    /// <summary>
    ///     The last item will be aligned to the right and the remaining will aligned to the left.
    ///     Set <see cref="Aligner.SpaceBetweenItems"/> to <see langword="true"/> to ensure at least one space between
    ///     each item.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the container is smaller than the total size of the items, the left items will be clipped (their locations
    ///         will be negative).
    ///     </para>
    ///     <para>
    ///         <see cref="LastRightRestLeft"/> and <see cref="LastBottomRestTop"/> have equivalent behavior.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <c>
    ///         111 2222        33333
    ///     </c>
    /// </example>
    LastRightRestLeft,

    /// <summary>
    ///     The last item will be aligned to the bottom and the remaining will aligned to the left.
    ///     Set <see cref="Aligner.SpaceBetweenItems"/> to <see langword="true"/> to ensure at least one line between
    ///     each item.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the container is smaller than the total size of the items, the top items will be clipped (their locations
    ///         will be negative).
    ///     </para>
    ///     <para>
    ///         <see cref="LastRightRestLeft"/> and <see cref="LastBottomRestTop"/> have equivalent behavior.
    ///     </para>
    /// </remarks>
    LastBottomRestTop
}