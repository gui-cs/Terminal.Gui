namespace Terminal.Gui;

/// <summary>
///     Controls how items are justified within a container. Used by <see cref="Justifier"/>.
/// </summary>
public enum Justification
{
    /// <summary>
    ///     The items will be aligned to the left.
    ///     The items will be arranged such that there is no more than <see cref="Justifier.MaxSpaceBetweenItems"/> space between each.
    /// </summary>
    /// <example>
    /// <c>
    /// 111 2222 33333
    /// </c>
    /// </example>
    Left,

    /// <summary>
    ///     The items will be aligned to the right.
    ///     The items will be arranged such that there is no more than <see cref="Justifier.MaxSpaceBetweenItems"/> space between each.
    /// </summary>
    /// <example>
    /// <c>
    ///    111 2222 33333
    /// </c>
    /// </example>
    Right,

    /// <summary>
    ///     The group will be centered in the container.
    ///     If centering is not possible, the group will be left-justified.
    ///     The items will be arranged such that there is no more than <see cref="Justifier.MaxSpaceBetweenItems"/> space between each.
    /// </summary>
    /// <example>
    /// <c>
    ///    111 2222 33333
    /// </c>
    /// </example>
    Centered,

    /// <summary>
    ///     The items will be justified. Space will be added between the items such that the first item
    ///     is at the start and the right side of the last item against the end.
    ///     The items will be arranged such that there is no more than <see cref="Justifier.MaxSpaceBetweenItems"/> space between each.
    /// </summary>
    /// <example>
    /// <c>
    /// 111    2222     33333
    /// </c>
    /// </example>
    Justified,

    /// <summary>
    ///    The first item will be aligned to the left and the remaining will aligned to the right with no more than <see cref="Justifier.MaxSpaceBetweenItems"/> between each.
    /// </summary>
    /// <example>
    /// <c>
    /// 111        2222 33333
    /// </c>
    /// </example>
    OneLeftRestRight,

    /// <summary>
    ///    The last item will be aligned to right and the remaining will aligned to the left with no more than <see cref="Justifier.MaxSpaceBetweenItems"/> between each.
    /// </summary>
    /// <example>
    /// <c>
    /// 111 2222        33333
    /// </c>
    /// </example>
    OneRightRestLeft
}

/// <summary>
///     Justifies items within a container based on the specified <see cref="Justification"/>.
/// </summary>
public class Justifier
{
    /// <summary>
    /// Gets or sets the maximum space between items. The default is 0. For text, this is usually 1.
    /// </summary>
    public int MaxSpaceBetweenItems { get; set; } = 0;

    /// <summary>
    ///     Justifies the <paramref name="sizes"/> within a container <see cref="totalSize"/> wide based on the specified
    ///     <see cref="Justification"/>.
    /// </summary>
    /// <param name="sizes"></param>
    /// <param name="justification"></param>
    /// <param name="totalSize"></param>
    /// <returns></returns>
    public int [] Justify (int [] sizes, Justification justification, int totalSize)
    {
        if (sizes.Length == 0)
        {
            return new int []{};
        }
        int totalItemsSize = sizes.Sum ();

        if (totalItemsSize > totalSize)
        {
            throw new ArgumentException ("The sum of the sizes is greater than the total size.");
        }

        var positions = new int [sizes.Length];
        totalItemsSize = sizes.Sum (); // total size of items
        int totalGaps = sizes.Length - 1; // total gaps (MinimumSpaceBetweenItems)
        int totalItemsAndSpaces = totalItemsSize + (totalGaps * MaxSpaceBetweenItems); // total size of items and spaces if we had enough room
        int spaces = totalGaps * MaxSpaceBetweenItems; // We'll decrement this below to place one space between each item until we run out
        if (totalItemsSize >= totalSize)
        {
            spaces = 0;
        }
        else if (totalItemsAndSpaces > totalSize)
        {
            spaces = totalSize - totalItemsSize;
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

                    var spaceBefore = spaces-- > 0 ? MaxSpaceBetweenItems : 0;

                    // subsequent items are placed one space after the previous item
                    positions [i] = positions [i - 1] + sizes [i - 1] + spaceBefore;
                }

                break;
            case Justification.Right:
                currentPosition = Math.Max (0, totalSize - totalItemsSize - spaces);

                for (var i = 0; i < sizes.Length; i++)
                {
                    if (sizes [i] < 0)
                    {
                        throw new ArgumentException ("The size of an item cannot be negative.");
                    }

                    var spaceBefore = spaces-- > 0 ? MaxSpaceBetweenItems : 0;

                    positions [i] = currentPosition;
                    currentPosition += sizes [i] + spaceBefore;
                }

                break;

            case Justification.Centered:
                if (sizes.Length > 1)
                {
                    // remaining space to be distributed before first and after the items
                    int remainingSpace = Math.Max(0, totalSize - totalItemsSize - spaces);

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

                        var spaceBefore = spaces-- > 0 ? MaxSpaceBetweenItems : 0;

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
                    positions [0] = (totalSize - sizes [0]) / 2; // single item is centered
                }
                break;


            case Justification.Justified:
                int spaceBetween = sizes.Length > 1 ? (totalSize - totalItemsSize) / (sizes.Length - 1) : 0;
                int remainder = sizes.Length > 1 ? (totalSize - totalItemsSize) % (sizes.Length - 1) : 0;
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

            /// 111 2222        33333
            case Justification.OneRightRestLeft:
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
                            var spaceBefore = spaces-- > 0 ? MaxSpaceBetweenItems : 0;

                            positions [i] = currentPosition;
                            currentPosition += sizes [i] + spaceBefore; 
                        }
                    }
                    positions [sizes.Length - 1] = totalSize - sizes [sizes.Length - 1];
                }
                else if (sizes.Length == 1)
                {
                    if (sizes [0] < 0)
                    {
                        throw new ArgumentException ("The size of an item cannot be negative.");
                    }
                    positions [0] = totalSize - sizes [0]; // single item is flush right
                }
                break;

            /// 111        2222 33333
            case Justification.OneLeftRestRight:
                if (sizes.Length > 1)
                {
                    currentPosition = 0;
                    positions [0] = currentPosition; // first item is flush left

                    for (var i = sizes.Length - 1 ; i >= 0; i--)
                    {
                        if (sizes [i] < 0)
                        {
                            throw new ArgumentException ("The size of an item cannot be negative.");
                        }

                        if (i == sizes.Length - 1)
                        {
                            // start at right
                            currentPosition = totalSize - sizes [i];
                            positions [i] = currentPosition;
                        }

                        if (i < sizes.Length - 1 && i > 0)
                        {
                            var spaceBefore = spaces-- > 0 ? MaxSpaceBetweenItems : 0;

                            positions [i] = currentPosition - sizes [i] - spaceBefore;
                            currentPosition -= sizes [i + 1];
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



        }

        return positions;
    }
}
