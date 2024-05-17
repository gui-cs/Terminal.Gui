using System.ComponentModel;

namespace Terminal.Gui;

/// <summary>
///     Aligns items within a container based on the specified <see cref="Gui.Alignment"/>. Both horizontal and vertical alignments are supported.
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

    private bool _spaceBetweenItems;

    /// <summary>
    ///     Gets or sets whether <see cref="Aligner"/> adds at least one space between items. Default is
    ///     <see langword="false"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the total size of the items is greater than the container size, the space between items will be ignored
    ///         starting from the right or bottom.
    ///     </para>
    /// </remarks>
    public bool SpaceBetweenItems
    {
        get => _spaceBetweenItems;
        set
        {
            _spaceBetweenItems = value;
            PropertyChanged?.Invoke (this, new (nameof (SpaceBetweenItems)));
        }
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    ///     Takes a list of item sizes and returns a list of the positions of those items when aligned within <see name="ContainerSize"/>
    ///     using the <see cref="Alignment"/> and <see cref="SpaceBetweenItems"/> settings.
    /// </summary>
    /// <param name="sizes">The sizes of the items to align.</param>
    /// <returns>The locations of the items, from left/top to right/bottom.</returns>
    public int [] Align (int [] sizes) { return Align (Alignment, SpaceBetweenItems, ContainerSize, sizes); }

    /// <summary>
    ///     Takes a list of item sizes and returns a list of the  positions of those items when aligned within <paramref name="containerSize"/>
    ///     using specified parameters.
    /// </summary>
    /// <param name="sizes">The sizes of the items to align.</param>
    /// <param name="alignment">Specifies how the items will be aligned.</param>
    /// <param name="spaceBetweenItems">
    ///     <para>
    ///         Indicates whether at least one space should be added between items.
    ///     </para>
    ///     <para>
    ///         If the total size of the items is greater than the container size, the space between items will be ignored
    ///         starting from the right or bottom.
    ///     </para>
    /// </param>
    /// <param name="containerSize">The size of the container.</param>
    /// <returns>The positions of the items, from left/top to right/bottom.</returns>
    public static int [] Align (Alignment alignment, bool spaceBetweenItems, int containerSize, int [] sizes)
    {
        if (sizes.Length == 0)
        {
            return new int [] { };
        }

        int maxSpaceBetweenItems = spaceBetweenItems ? 1 : 0;

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
