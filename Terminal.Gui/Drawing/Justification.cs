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
    ///     The items will be centered.
    /// </summary>
    Centered,

    /// <summary>
    ///     The items will be justified. Space will be added between the items such that the first item
    ///     is at the start and the right side of the last item against the end.
    /// </summary>
    Justified,

    RightJustified,
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
                currentPosition = (totalSize - totalItemsSize) / 2;

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
