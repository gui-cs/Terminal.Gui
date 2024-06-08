#nullable enable

using System.ComponentModel;
using System.Drawing;

namespace Terminal.Gui;

/// <summary>
///     Enables alignment of a set of views.
/// </summary>
/// <remarks>
///     <para>
///         Updating the properties of <see cref="Aligner"/> is supported, but will not automatically cause re-layout to
///         happen. <see cref="View.LayoutSubviews"/>
///         must be called on the SuperView.
///     </para>
///     <para>
///         Views that should be aligned together must have a distinct <see cref="GroupId"/>. When only a single
///         set of views is aligned within a SuperView, setting <see cref="GroupId"/> is optional because it defaults to 0.
///     </para>
///     <para>
///         The first view added to the Superview with a given <see cref="GroupId"/> is used to determine the alignment of
///         the group.
///         The alignment is applied to all views with the same <see cref="GroupId"/>.
///     </para>
/// </remarks>
public class PosAlign : Pos
{
    /// <summary>
    ///     The cached location. Used to store the calculated location to minimize recalculating it.
    /// </summary>
    private int? _cachedLocation;

    /// <summary>
    ///     Gets the identifier of a set of views that should be aligned together. When only a single
    ///     set of views in a SuperView is aligned, setting <see cref="GroupId"/> is not needed because it defaults to 0.
    /// </summary>
    public int GroupId { get; init; }

    private readonly Aligner? _aligner;

    /// <summary>
    ///     Gets the alignment settings.
    /// </summary>
    public required Aligner Aligner
    {
        get => _aligner!;
        init
        {
            if (_aligner is { })
            {
                _aligner.PropertyChanged -= Aligner_PropertyChanged;
            }

            _aligner = value;
            _aligner.PropertyChanged += Aligner_PropertyChanged;
        }
    }

    /// <summary>
    ///     Aligns the views in <paramref name="views"/> that have the same group ID as <paramref name="groupId"/>.
    ///     Updates each view's cached _location.
    /// </summary>
    /// <param name="groupId"></param>
    /// <param name="views"></param>
    /// <param name="dimension"></param>
    /// <param name="size"></param>
    private static void AlignAndUpdateGroup (int groupId, IList<View> views, Dimension dimension, int size)
    {
        List<int> dimensionsList = new ();

        // PERF: If this proves a perf issue, consider caching a ref to this list in each item
        List<View> viewsInGroup = views.Where (
                                               v =>
                                               {
                                                   return dimension switch
                                                   {
                                                       Dimension.Width when v.X is PosAlign alignX => alignX.GroupId == groupId,
                                                       Dimension.Height when v.Y is PosAlign alignY => alignY.GroupId == groupId,
                                                       _ => false
                                                   };
                                               })
                                       .ToList ();

        if (viewsInGroup.Count == 0)
        {
            return;
        }

        // PERF: We iterate over viewsInGroup multiple times here.

        Aligner? firstInGroup = null;

        // Update the dimensionList with the sizes of the views
        for (var index = 0; index < viewsInGroup.Count; index++)
        {
            View view = viewsInGroup [index];
            PosAlign? posAlign = dimension == Dimension.Width ? view.X as PosAlign : view.Y as PosAlign;

            if (posAlign is { })
            {
                if (index == 0)
                {
                    firstInGroup = posAlign.Aligner;
                }

                dimensionsList.Add (dimension == Dimension.Width ? view.Frame.Width : view.Frame.Height);
            }
        }

        // Update the first item in the group with the new container size.
        firstInGroup!.ContainerSize = size;

        // Align
        int [] locations = firstInGroup.Align (dimensionsList.ToArray ());

        // Update the cached location for each item
        for (var index = 0; index < viewsInGroup.Count; index++)
        {
            View view = viewsInGroup [index];
            PosAlign? align = dimension == Dimension.Width ? view.X as PosAlign : view.Y as PosAlign;

            if (align is { })
            {
                align._cachedLocation = locations [index];
            }
        }
    }

    private void Aligner_PropertyChanged (object? sender, PropertyChangedEventArgs e) { _cachedLocation = null; }

    /// <inheritdoc/>
    public override bool Equals (object? other)
    {
        return other is PosAlign align
               && GroupId == align.GroupId
               && align.Aligner.Alignment == Aligner.Alignment
               && align.Aligner.AlignmentModes == Aligner.AlignmentModes;
    }

    /// <inheritdoc/>
    public override int GetHashCode () { return HashCode.Combine (Aligner, GroupId); }

    /// <inheritdoc/>
    public override string ToString () { return $"Align(alignment={Aligner.Alignment},modes={Aligner.AlignmentModes},groupId={GroupId})"; }

    internal override int GetAnchor (int width) { return _cachedLocation ?? 0 - width; }

    internal override int Calculate (int superviewDimension, Dim dim, View us, Dimension dimension)
    {
        if (_cachedLocation.HasValue && Aligner.ContainerSize == superviewDimension)
        {
            return _cachedLocation.Value;
        }

        if (us?.SuperView is null)
        {
            return 0;
        }

        AlignAndUpdateGroup (GroupId, us.SuperView.Subviews, dimension, superviewDimension);

        if (_cachedLocation.HasValue)
        {
            return _cachedLocation.Value;
        }

        return 0;
    }

    internal int CalculateMinDimension (int groupId, IList<View> views, Dimension dimension)
    {
        List<int> dimensionsList = new ();

        // PERF: If this proves a perf issue, consider caching a ref to this list in each item
        List<View> viewsInGroup = views.Where (
                                               v =>
                                               {
                                                   return dimension switch
                                                          {
                                                              Dimension.Width when v.X is PosAlign alignX => alignX.GroupId == groupId,
                                                              Dimension.Height when v.Y is PosAlign alignY => alignY.GroupId == groupId,
                                                              _ => false
                                                          };
                                               })
                                       .ToList ();

        if (viewsInGroup.Count == 0)
        {
            return 0;
        }

        // PERF: We iterate over viewsInGroup multiple times here.

        Aligner? firstInGroup = null;

        // Update the dimensionList with the sizes of the views
        for (var index = 0; index < viewsInGroup.Count; index++)
        {
            View view = viewsInGroup [index];
            PosAlign? posAlign = dimension == Dimension.Width ? view.X as PosAlign : view.Y as PosAlign;

            if (posAlign is { })
            {
                if (index == 0)
                {
                    firstInGroup = posAlign.Aligner;
                }

                dimensionsList.Add (dimension == Dimension.Width ? view.Frame.Width : view.Frame.Height);
            }
        }

        // Align
        var aligner = firstInGroup;
        aligner.ContainerSize = dimensionsList.Sum();
        int [] locations = aligner.Align (dimensionsList.ToArray ());

        return locations.Sum ();
    }
}
