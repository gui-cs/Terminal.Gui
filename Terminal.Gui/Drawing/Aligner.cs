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
    /// <remarks>
    ///     <para>
    ///         <see cref="AlignmentMode"/> provides additional options for aligning items in a container.
    ///     </para>
    /// </remarks>
    public Alignment Alignment
    {
        get => _alignment;
        set
        {
            _alignment = value;
            PropertyChanged?.Invoke (this, new (nameof (Alignment)));
        }
    }

    private AlignmentModes _alignmentMode = AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems;

    /// <summary>
    ///     Gets or sets the modes controlling <see cref="Alignment"/>.
    /// </summary>
    public AlignmentModes AlignmentMode
    {
        get => _alignmentMode;
        set
        {
            _alignmentMode = value;
            PropertyChanged?.Invoke (this, new (nameof (AlignmentMode)));
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

    /// <inheritdoc/>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    ///     Takes a list of item sizes and returns a list of the positions of those items when aligned within <see name="ContainerSize"/>
    ///     using the <see cref="Alignment"/> and <see cref="AlignmentMode"/> settings.
    /// </summary>
    /// <param name="sizes">The sizes of the items to align.</param>
    /// <returns>The locations of the items, from left/top to right/bottom.</returns>
    public int [] Align (int [] sizes) { return Align (Alignment, AlignmentMode, ContainerSize, sizes); }

    /// <summary>
    ///     Takes a list of item sizes and returns a list of the  positions of those items when aligned within <paramref name="containerSize"/>
    ///     using specified parameters.
    /// </summary>
    /// <param name="alignment">Specifies how the items will be aligned.</param>
    /// <param name="alignmentMode"></param>
    /// <param name="containerSize">The size of the container.</param>
    /// <param name="sizes">The sizes of the items to align.</param>
    /// <returns>The positions of the items, from left/top to right/bottom.</returns>
    public static int [] Align (in Alignment alignment, in AlignmentModes alignmentMode, int containerSize, int [] sizes)
    {
        if (sizes.Length == 0)
        {
            return new int [] { };
        }

        int maxSpaceBetweenItems = alignmentMode.HasFlag (AlignmentModes.AddSpaceBetweenItems) ? 1 : 0;

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

        var currentPosition = 0;

        switch (alignment)
        {
            case Alignment.Start:
                switch (alignmentMode & ~AlignmentModes.AddSpaceBetweenItems)
                {
                    case AlignmentModes.StartToEnd:
                        Start (sizes, positions, ref spaces, maxSpaceBetweenItems);

                        break;

                    case AlignmentModes.StartToEnd | AlignmentModes.IgnoreFirstOrLast:
                        IgnoreLast (sizes, containerSize, positions, maxSpaceBetweenItems, totalItemsSize, spaces, currentPosition);

                        break;

                    case AlignmentModes.EndToStart:
                    case AlignmentModes.EndToStart | AlignmentModes.IgnoreFirstOrLast:
                        throw new NotImplementedException ("EndToStart is not implemented.");

                        break;
                }

                break;

            case Alignment.End:
                switch (alignmentMode & ~AlignmentModes.AddSpaceBetweenItems)
                {
                    case AlignmentModes.StartToEnd:
                        End (containerSize, sizes, totalItemsSize, spaces, maxSpaceBetweenItems, positions);

                        break;

                    case AlignmentModes.StartToEnd | AlignmentModes.IgnoreFirstOrLast:
                        IgnoreFirst (sizes, containerSize, positions, maxSpaceBetweenItems, totalItemsSize, spaces, currentPosition);

                        break;

                    case AlignmentModes.EndToStart:
                    case AlignmentModes.EndToStart | AlignmentModes.IgnoreFirstOrLast:
                        throw new NotImplementedException ("EndToStart is not implemented.");

                        break;

                }

                break;

            case Alignment.Center:
                Center (containerSize, sizes, totalItemsSize, spaces, positions, maxSpaceBetweenItems);

                break;

            case Alignment.Fill:
                Fill (containerSize, sizes, totalItemsSize, positions);

                break;

            default:
                throw new ArgumentOutOfRangeException (nameof (alignment), alignment, null);
        }

        return positions;
    }

    private static void Start (int [] sizes, int [] positions, ref int spaces, int maxSpaceBetweenItems)
    {
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
    }

    private static void IgnoreFirst (int [] sizes, int containerSize, int [] positions, int maxSpaceBetweenItems, int totalItemsSize, int spaces, int currentPosition)
    {
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
    }

    private static void IgnoreLast (int [] sizes, int containerSize, int [] positions, int maxSpaceBetweenItems, int totalItemsSize, int spaces, int currentPosition)
    {
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
    }

    private static void Fill (int containerSize, int [] sizes, int totalItemsSize, int [] positions)
    {
        int currentPosition;
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
    }

    private static void Center (int containerSize, int [] sizes, int totalItemsSize, int spaces, int [] positions, int maxSpaceBetweenItems)
    {
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
    }

    private static void End (int containerSize, int [] sizes, int totalItemsSize, int spaces, int maxSpaceBetweenItems, int [] positions)
    {
        int currentPosition;
        currentPosition = containerSize - totalItemsSize - spaces;

        for (var i = 0; i < sizes.Length; i++)
        {
            CheckSizeCannotBeNegative (i, sizes);
            int spaceBefore = spaces-- > 0 ? maxSpaceBetweenItems : 0;

            positions [i] = currentPosition;
            currentPosition += sizes [i] + spaceBefore;
        }
    }

    private static void CheckSizeCannotBeNegative (int i, int [] sizes)
    {
        if (sizes [i] < 0)
        {
            throw new ArgumentException ("The size of an item cannot be negative.");
        }
    }
}
