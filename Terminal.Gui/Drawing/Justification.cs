namespace Terminal.Gui;

/// <summary>
///     Controls how the <see cref="Justifier"/> justifies items within a container. 
/// </summary>
public enum Justification
{
    /// <summary>
    ///     The items will be aligned to the left.
    ///     Set <see cref="Justifier.PutSpaceBetweenItems"/> to <see langword="true"/> to ensure at least one space between
    ///     each item.
    /// </summary>
    /// <example>
    ///     <c>
    ///         111 2222 33333
    ///     </c>
    /// </example>
    Left,

    /// <summary>
    ///     The items will be aligned to the right.
    ///     Set <see cref="Justifier.PutSpaceBetweenItems"/> to <see langword="true"/> to ensure at least one space between
    ///     each item.
    /// </summary>
    /// <example>
    ///     <c>
    ///         111 2222 33333
    ///     </c>
    /// </example>
    Right,

    /// <summary>
    ///     The group will be centered in the container.
    ///     If centering is not possible, the group will be left-justified.
    ///     Set <see cref="Justifier.PutSpaceBetweenItems"/> to <see langword="true"/> to ensure at least one space between
    ///     each item.
    /// </summary>
    /// <example>
    ///     <c>
    ///         111 2222 33333
    ///     </c>
    /// </example>
    Centered,

    /// <summary>
    ///     The items will be justified. Space will be added between the items such that the first item
    ///     is at the start and the right side of the last item against the end.
    ///     Set <see cref="Justifier.PutSpaceBetweenItems"/> to <see langword="true"/> to ensure at least one space between
    ///     each item.
    /// </summary>
    /// <example>
    ///     <c>
    ///         111    2222     33333
    ///     </c>
    /// </example>
    Justified,

    /// <summary>
    ///     The first item will be aligned to the left and the remaining will aligned to the right.
    ///     Set <see cref="Justifier.PutSpaceBetweenItems"/> to <see langword="true"/> to ensure at least one space between
    ///     each item.
    /// </summary>
    /// <example>
    ///     <c>
    ///         111        2222 33333
    ///     </c>
    /// </example>
    FirstLeftRestRight,

    /// <summary>
    ///     The last item will be aligned to the right and the remaining will aligned to the left.
    ///     Set <see cref="Justifier.PutSpaceBetweenItems"/> to <see langword="true"/> to ensure at least one space between
    ///     each item.
    /// </summary>
    /// <example>
    ///     <c>
    ///         111 2222        33333
    ///     </c>
    /// </example>
    LastRightRestLeft
}

/// <summary>
///     Justifies items within a container based on the specified <see cref="Justification"/>.
/// </summary>
public class Justifier
{
    /// <summary>
    /// Gets or sets how the <see cref="Justifier"/> justifies items within a container.
    /// </summary>
    public Justification Justification { get; set; }

    /// <summary>
    /// The size of the container.
    /// </summary>
    public int ContainerSize { get; set; }

    /// <summary>
    ///     Gets or sets whether <see cref="Justify(int[])"/> puts a space is placed between items. Default is <see langword="false"/>. If <see langword="true"/>, a space will be
    ///     placed between each item, which is useful for justifying text.
    /// </summary>
    public bool PutSpaceBetweenItems { get; set; }

    /// <summary>
    ///     Takes a list of items and returns their positions when justified within a container <see name="ContainerSize"/> wide based on the specified
    ///     <see cref="Justification"/>.
    /// </summary>
    /// <param name="sizes">The sizes of the items to justify.</param>
    /// <returns>The locations of the items, from left to right.</returns>
    public int [] Justify (int [] sizes)
    {
        return Justify (Justification, PutSpaceBetweenItems, ContainerSize, sizes);
    }

    /// <summary>
    ///     Takes a list of items and returns their positions when justified within a container <paramref name="containerSize"/> wide based on the specified
    ///     <see cref="Justification"/>.
    /// </summary>
    /// <param name="sizes">The sizes of the items to justify.</param>
    /// <param name="justification">The justification style.</param>
    /// <param name="putSpaceBetweenItems"></param>
    /// <param name="containerSize">The size of the container.</param>
    /// <returns>The locations of the items, from left to right.</returns>
    public static int [] Justify (Justification justification, bool putSpaceBetweenItems, int containerSize, int [] sizes)
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

        switch (justification)
        {
            case Justification.Left:
                var currentPosition = 0;

                for (var i = 0; i < sizes.Length; i++)
                {
                    if (sizes [i] < 0)
                    {
                        throw new ArgumentException ("The size of an item cannot be negative.");
                    }

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
            case Justification.Right:
                currentPosition = Math.Max (0, containerSize - totalItemsSize - spaces);

                for (var i = 0; i < sizes.Length; i++)
                {
                    if (sizes [i] < 0)
                    {
                        throw new ArgumentException ("The size of an item cannot be negative.");
                    }

                    int spaceBefore = spaces-- > 0 ? maxSpaceBetweenItems : 0;

                    positions [i] = currentPosition;
                    currentPosition += sizes [i] + spaceBefore;
                }

                break;

            case Justification.Centered:
                if (sizes.Length > 1)
                {
                    // remaining space to be distributed before first and after the items
                    int remainingSpace = Math.Max (0, containerSize - totalItemsSize - spaces);

                    for (var i = 0; i < sizes.Length; i++)
                    {
                        if (sizes [i] < 0)
                        {
                            throw new ArgumentException ("The size of an item cannot be negative.");
                        }

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
                    if (sizes [0] < 0)
                    {
                        throw new ArgumentException ("The size of an item cannot be negative.");
                    }

                    positions [0] = (containerSize - sizes [0]) / 2; // single item is centered
                }

                break;

            case Justification.Justified:
                int spaceBetween = sizes.Length > 1 ? (containerSize - totalItemsSize) / (sizes.Length - 1) : 0;
                int remainder = sizes.Length > 1 ? (containerSize - totalItemsSize) % (sizes.Length - 1) : 0;
                currentPosition = 0;

                for (var i = 0; i < sizes.Length; i++)
                {
                    if (sizes [i] < 0)
                    {
                        throw new ArgumentException ("The size of an item cannot be negative.");
                    }

                    positions [i] = currentPosition;
                    int extraSpace = i < remainder ? 1 : 0;
                    currentPosition += sizes [i] + spaceBetween + extraSpace;
                }

                break;

            // 111 2222        33333
            case Justification.LastRightRestLeft:
                if (sizes.Length > 1)
                {
                    currentPosition = 0;

                    for (var i = 0; i < sizes.Length; i++)
                    {
                        if (sizes [i] < 0)
                        {
                            throw new ArgumentException ("The size of an item cannot be negative.");
                        }

                        if (i < sizes.Length - 1)
                        {
                            int spaceBefore = spaces-- > 0 ? maxSpaceBetweenItems : 0;

                            positions [i] = currentPosition;
                            currentPosition += sizes [i] + spaceBefore;
                        }
                    }

                    positions [sizes.Length - 1] = containerSize - sizes [sizes.Length - 1];
                }
                else if (sizes.Length == 1)
                {
                    if (sizes [0] < 0)
                    {
                        throw new ArgumentException ("The size of an item cannot be negative.");
                    }

                    positions [0] = containerSize - sizes [0]; // single item is flush right
                }

                break;

            // 111        2222 33333
            case Justification.FirstLeftRestRight:
                if (sizes.Length > 1)
                {
                    currentPosition = 0;
                    positions [0] = currentPosition; // first item is flush left

                    for (int i = sizes.Length - 1; i >= 0; i--)
                    {
                        if (sizes [i] < 0)
                        {
                            throw new ArgumentException ("The size of an item cannot be negative.");
                        }

                        if (i == sizes.Length - 1)
                        {
                            // start at right
                            currentPosition = containerSize - sizes [i];
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
                    if (sizes [0] < 0)
                    {
                        throw new ArgumentException ("The size of an item cannot be negative.");
                    }

                    positions [0] = 0; // single item is flush left
                }

                break;

            default:
                throw new ArgumentOutOfRangeException (nameof (justification), justification, null);
        }

        return positions;
    }
}
