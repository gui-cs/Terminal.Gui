using System.ComponentModel;
using Microsoft.CodeAnalysis;
using static Terminal.Gui.Pos;

namespace Terminal.Gui;

/// <summary>
///     Controls how the <see cref="Aligner"/> aligns items within a container.
/// </summary>
public enum Alignment
{
    /// <summary>
    ///     The items will be aligned to the left.
    ///     Set <see cref="Aligner.PutSpaceBetweenItems"/> to <see langword="true"/> to ensure at least one space between
    ///     each item.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the container is smaller than the total size of the items, the right items will be clipped (their locations
    ///         will be greater than the container size).
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <c>
    ///         111 2222 33333
    ///     </c>
    /// </example>
    Left,

    /// <summary>
    ///     The items will be aligned to the top.
    ///     Set <see cref="Aligner.PutSpaceBetweenItems"/> to <see langword="true"/> to ensure at least one line between
    ///     each item.
    /// </summary>
    Top,

    /// <summary>
    ///     The items will be aligned to the right.
    ///     Set <see cref="Aligner.PutSpaceBetweenItems"/> to <see langword="true"/> to ensure at least one space between
    ///     each item.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the container is smaller than the total size of the items, the left items will be clipped (their locations
    ///         will be negative).
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
    ///     Set <see cref="Aligner.PutSpaceBetweenItems"/> to <see langword="true"/> to ensure at least one line between
    ///     each item.
    /// </summary>
    Bottom,

    /// <summary>
    ///     The group will be centered in the container.
    ///     If centering is not possible, the group will be left-aligned.
    ///     Set <see cref="Aligner.PutSpaceBetweenItems"/> to <see langword="true"/> to ensure at least one space between
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
        ///     Set <see cref="Aligner.PutSpaceBetweenItems"/> to <see langword="true"/> to ensure at least one space between
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
    ///     Set <see cref="Aligner.PutSpaceBetweenItems"/> to <see langword="true"/> to ensure at least one space between
    ///     each item.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the container is smaller than the total size of the items, the right items will be clipped (their locations
    ///         will be greater than the container size).
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
    ///     Set <see cref="Aligner.PutSpaceBetweenItems"/> to <see langword="true"/> to ensure at least one line between
    ///     each item.
    /// </summary>
    FirstTopRestBottom,

    /// <summary>
    ///     The last item will be aligned to the right and the remaining will aligned to the left.
    ///     Set <see cref="Aligner.PutSpaceBetweenItems"/> to <see langword="true"/> to ensure at least one space between
    ///     each item.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the container is smaller than the total size of the items, the left items will be clipped (their locations
    ///         will be negative).
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
    ///     Set <see cref="Aligner.PutSpaceBetweenItems"/> to <see langword="true"/> to ensure at least one line between
    ///     each item.
    /// </summary>
    LastBottomRestTop
}

/// <summary>
///     Aligns items within a container based on the specified <see cref="Gui.Alignment"/>.
/// </summary>
public class Aligner : INotifyPropertyChanged
{
    private Alignment _alignment;

    /// <summary>
    ///     Gets or sets how the <see cref="Aligner"/> aligns items within a container.
    /// </summary>
    public Alignment Alignment
    {
        get => _alignment;
        set
        {
            _alignment = value;
            PropertyChanged?.Invoke (this, new (nameof (Alignment)));
        }
    }

    private int _containerSize;

    /// <summary>
    ///     The size of the container.
    /// </summary>
    public int ContainerSize
    {
        get => _containerSize;
        set
        {
            _containerSize = value;
            PropertyChanged?.Invoke (this, new (nameof (ContainerSize)));
        }
    }

    private bool _putSpaceBetweenItems;

    /// <summary>
    ///     Gets or sets whether <see cref="Aligner"/> puts a space is placed between items. Default is
    ///     <see langword="false"/>. If <see langword="true"/>, a space will be
    ///     placed between each item, which is useful for justifying text.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the total size of the items is greater than the container size, the space between items will be ignored
    ///         starting
    ///         from the right.
    ///     </para>
    /// </remarks>
    public bool PutSpaceBetweenItems
    {
        get => _putSpaceBetweenItems;
        set
        {
            _putSpaceBetweenItems = value;
            PropertyChanged?.Invoke (this, new (nameof (PutSpaceBetweenItems)));
        }
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    ///     Takes a list of items and returns their positions when aligned within a container <see name="ContainerSize"/>
    ///     wide based on the specified
    ///     <see cref="Alignment"/>.
    /// </summary>
    /// <param name="sizes">The sizes of the items to align.</param>
    /// <returns>The locations of the items, from left to right.</returns>
    public int [] Align (int [] sizes) { return Align (Alignment, PutSpaceBetweenItems, ContainerSize, sizes); }

    /// <summary>
    ///     Takes a list of items and returns their positions when aligned within a container
    ///     <paramref name="containerSize"/> wide based on the specified
    ///     <see cref="Alignment"/>.
    /// </summary>
    /// <param name="sizes">The sizes of the items to align.</param>
    /// <param name="alignment">Specifies how the items will be aligned.</param>
    /// <param name="putSpaceBetweenItems">Puts a space is placed between items.</param>
    /// <param name="containerSize">The size of the container.</param>
    /// <returns>The locations of the items, from left to right.</returns>
    public static int [] Align (Alignment alignment, bool putSpaceBetweenItems, int containerSize, int [] sizes)
    {
        if (sizes.Length == 0)
        {
            return new int [] { };
        }

        int maxSpaceBetweenItems = putSpaceBetweenItems ? 1 : 0;

        var positions = new int [sizes.Length]; // positions of the items. the return value.
        int totalItemsSize = sizes.Sum ();
        int totalGaps = sizes.Length - 1; // total gaps between items
        int totalItemsAndSpaces = totalItemsSize + totalGaps * maxSpaceBetweenItems; // total size of items and spaces if we had enough room

        int spaces = totalGaps * maxSpaceBetweenItems; // We'll decrement this below to place one space between each item until we run out

        if (totalItemsSize >= containerSize)
        {
            spaces = 0;
        }
        else if (totalItemsAndSpaces > containerSize)
        {
            spaces = containerSize - totalItemsSize;
        }

        switch (alignment)
        {
            case Alignment.Left:
            case Alignment.Top:
                var currentPosition = 0;

                for (var i = 0; i < sizes.Length; i++)
                {
                    CheckSizeCannotBeNegative (i, sizes);

                    if (i == 0)
                    {
                        positions [0] = 0; // first item position

                        continue;
                    }

                    int spaceBefore = spaces-- > 0 ? maxSpaceBetweenItems : 0;

                    // subsequent items are placed one space after the previous item
                    positions [i] = positions [i - 1] + sizes [i - 1] + spaceBefore;
                }

                break;

            case Alignment.Right:
            case Alignment.Bottom:

                currentPosition = containerSize - totalItemsSize - spaces;

                for (var i = 0; i < sizes.Length; i++)
                {
                    CheckSizeCannotBeNegative (i, sizes);
                    int spaceBefore = spaces-- > 0 ? maxSpaceBetweenItems : 0;

                    positions [i] = currentPosition;
                    currentPosition += sizes [i] + spaceBefore;
                }

                break;

            case Alignment.Centered:
                if (sizes.Length > 1)
                {
                    // remaining space to be distributed before first and after the items
                    int remainingSpace = Math.Max (0, containerSize - totalItemsSize - spaces);

                    for (var i = 0; i < sizes.Length; i++)
                    {
                        CheckSizeCannotBeNegative (i, sizes);

                        if (i == 0)
                        {
                            positions [i] = remainingSpace / 2; // first item position

                            continue;
                        }

                        int spaceBefore = spaces-- > 0 ? maxSpaceBetweenItems : 0;

                        // subsequent items are placed one space after the previous item
                        positions [i] = positions [i - 1] + sizes [i - 1] + spaceBefore;
                    }
                }
                else if (sizes.Length == 1)
                {
                    CheckSizeCannotBeNegative (0, sizes);
                    positions [0] = (containerSize - sizes [0]) / 2; // single item is centered
                }

                break;

            case Alignment.Justified:
                int spaceBetween = sizes.Length > 1 ? (containerSize - totalItemsSize) / (sizes.Length - 1) : 0;
                int remainder = sizes.Length > 1 ? (containerSize - totalItemsSize) % (sizes.Length - 1) : 0;
                currentPosition = 0;

                for (var i = 0; i < sizes.Length; i++)
                {
                    CheckSizeCannotBeNegative (i, sizes);
                    positions [i] = currentPosition;
                    int extraSpace = i < remainder ? 1 : 0;
                    currentPosition += sizes [i] + spaceBetween + extraSpace;
                }

                break;

            // 111 2222        33333
            case Alignment.LastRightRestLeft:
            case Alignment.LastBottomRestTop:
                if (sizes.Length > 1)
                {
                    if (totalItemsSize > containerSize)
                    {
                        currentPosition = containerSize - totalItemsSize - spaces;
                    }
                    else
                    {
                        currentPosition = 0;
                    }

                    for (var i = 0; i < sizes.Length; i++)
                    {
                        CheckSizeCannotBeNegative (i, sizes);

                        if (i < sizes.Length - 1)
                        {
                            int spaceBefore = spaces-- > 0 ? maxSpaceBetweenItems : 0;

                            positions [i] = currentPosition;
                            currentPosition += sizes [i] + spaceBefore;
                        }
                    }

                    positions [sizes.Length - 1] = containerSize - sizes [^1];
                }
                else if (sizes.Length == 1)
                {
                    CheckSizeCannotBeNegative (0, sizes);

                    positions [0] = containerSize - sizes [0]; // single item is flush right
                }

                break;

            // 111        2222 33333
            case Alignment.FirstLeftRestRight:
            case Alignment.FirstTopRestBottom:
                if (sizes.Length > 1)
                {
                    currentPosition = 0;
                    positions [0] = currentPosition; // first item is flush left

                    for (int i = sizes.Length - 1; i >= 0; i--)
                    {
                        CheckSizeCannotBeNegative (i, sizes);

                        if (i == sizes.Length - 1)
                        {
                            // start at right
                            currentPosition = Math.Max (totalItemsSize, containerSize) - sizes [i];
                            positions [i] = currentPosition;
                        }

                        if (i < sizes.Length - 1 && i > 0)
                        {
                            int spaceBefore = spaces-- > 0 ? maxSpaceBetweenItems : 0;

                            positions [i] = currentPosition - sizes [i] - spaceBefore;
                            currentPosition = positions [i];
                        }
                    }
                }
                else if (sizes.Length == 1)
                {
                    CheckSizeCannotBeNegative (0, sizes);
                    positions [0] = 0; // single item is flush left
                }

                break;

            default:
                throw new ArgumentOutOfRangeException (nameof (alignment), alignment, null);
        }

        return positions;
    }

    private static void CheckSizeCannotBeNegative (int i, int [] sizes)
    {
        if (sizes [i] < 0)
        {
            throw new ArgumentException ("The size of an item cannot be negative.");
        }
    }
}
