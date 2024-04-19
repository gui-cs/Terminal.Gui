namespace Terminal.Gui;

/// <summary>
///     Controls how items are justified within a container. Used by <see cref="Justifier"/>.
/// </summary>
public enum Justification
{
    /// <summary>
    ///     The items will be left-justified.
    /// </summary>
    Left,

    /// <summary>
    ///     The items will be right-justified.
    /// </summary>
    Right,

    /// <summary>
    ///     The items will be arranged such that there is no more than 1 space between them. The group will be centered in the container.
    /// </summary>
    Centered,

    /// <summary>
    ///     The items will be justified. Space will be added between the items such that the first item
    ///     is at the start and the right side of the last item against the end.
    /// </summary>
    /// <example>
    /// <c>
    /// 111 2222        33333
    /// </c>
    /// </example>
    Justified,

    /// <summary>
    ///    The items will be left-justified. The first item will be at the start and the last item will be at the end.
    ///    Those in between will be tight against the right item.
    /// </summary>
    /// <example>
    /// <c>
    /// 111 2222        33333
    /// </c>
    /// </example>
    RightJustified,

    /// <summary>
    ///    The items will be left-justified. The first item will be at the start and the last item will be at the end.
    ///    Those in between will be tight against the right item.
    /// </summary>
    /// <example>
    /// <c>
    /// 111        2222 33333
    /// </c>
    /// </example>
    LeftJustified
}

/// <summary>
///     Justifies items within a container based on the specified <see cref="Justification"/>.
/// </summary>
public class Justifier
{
    /// <summary>
    ///     Justifies the <paramref name="sizes"/> within a container <see cref="totalSize"/> wide based on the specified
    ///     <see cref="Justification"/>.
    /// </summary>
    /// <param name="sizes"></param>
    /// <param name="justification"></param>
    /// <param name="totalSize"></param>
    /// <returns></returns>
    public static int [] Justify (int [] sizes, Justification justification, int totalSize)
    {
        var positions = new int [sizes.Length];
        int totalItemsSize = sizes.Sum ();

        if (totalItemsSize > totalSize)
        {
            throw new ArgumentException ("The sum of the sizes is greater than the total size.");
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

                    positions [i] = currentPosition;
                    currentPosition += sizes [i];
                }

                break;
            case Justification.Right:
                currentPosition = totalSize - totalItemsSize;

                for (var i = 0; i < sizes.Length; i++)
                {
                    if (sizes [i] < 0)
                    {
                        throw new ArgumentException ("The size of an item cannot be negative.");
                    }

                    positions [i] = currentPosition;
                    currentPosition += sizes [i];
                }

                break;

            case Justification.Centered:
                if (sizes.Length > 1)
                {
                    totalItemsSize = sizes.Sum (); // total size of items
                    int totalGaps = sizes.Length - 1; // total gaps (0 or 1 space)
                    int totalItemsAndSpaces = totalItemsSize + totalGaps; // total size of items and spaces

                    int spaces = totalGaps;

                    if (totalItemsSize >= totalSize)
                    {
                        spaces = 0;
                    } 
                    else if (totalItemsAndSpaces > totalSize)
                    {
                        spaces = totalItemsAndSpaces - totalSize;
                    }

                    int remainingSpace = Math.Max(0, totalSize - totalItemsSize - spaces); // remaining space to be distributed before and after the items
                    int spaceBefore = remainingSpace / 2; // space before the items

                    positions [0] = spaceBefore; // first item position
                    for (var i = 1; i < sizes.Length; i++)
                    {
                        int aSpace = 0;
                        if (spaces > 0)
                        {
                            spaces--;
                            aSpace = 1;
                        }
                        // subsequent items are placed one space after the previous item
                        positions [i] = positions [i - 1] + sizes [i - 1] + aSpace;
                    }
                    // Adjust the last position if there is an extra space
                    if (positions [sizes.Length - 1] + sizes [sizes.Length - 1] > totalSize)
                    {
                        positions [sizes.Length - 1]--;
                    }
                }
                else if (sizes.Length == 1)
                {
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

            case Justification.LeftJustified:
                if (sizes.Length > 1)
                {
                    int spaceBetweenLeft = totalSize - sizes.Sum () + 1; // +1 for the extra space
                    currentPosition = 0;
                    for (var i = 0; i < sizes.Length - 1; i++)
                    {
                        if (sizes [i] < 0)
                        {
                            throw new ArgumentException ("The size of an item cannot be negative.");
                        }
                        positions [i] = currentPosition;
                        currentPosition += sizes [i] + 1; // +1 for the extra space
                    }
                    positions [sizes.Length - 1] = totalSize - sizes [sizes.Length - 1];
                }
                else if (sizes.Length == 1)
                {
                    positions [0] = 0;
                }
                break;

            case Justification.RightJustified:
                if (sizes.Length > 1)
                {
                    totalItemsSize = sizes.Sum ();
                    int totalSpaces = totalSize - totalItemsSize;
                    int bigSpace = totalSpaces - (sizes.Length - 2);

                    positions [0] = 0; // first item is flush left
                    positions [1] = sizes [0] + bigSpace; // second item has the big space before it

                    // remaining items have one space between them
                    for (var i = 2; i < sizes.Length; i++)
                    {
                        positions [i] = positions [i - 1] + sizes [i - 1] + 1;
                    }
                }
                else if (sizes.Length == 1)
                {
                    positions [0] = 0; // single item is flush left
                }
                break;



        }

        return positions;
    }
}
