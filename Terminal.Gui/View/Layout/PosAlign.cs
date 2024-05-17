#nullable enable

using System.ComponentModel;

namespace Terminal.Gui;

/// <summary>
///     Enables alignment of a set of views.
/// </summary>
/// <remarks>
///     <para>
///         The Group ID is used to identify a set of views that should be alignment together. When only a single
///         set of views is aligned, setting the Group ID is not needed because it defaults to 0.
///     </para>
///     <para>
///         The first view added to the Superview with a given Group ID is used to determine the alignment of the group.
///         The alignment is applied to all views with the same Group ID.
///     </para>
/// </remarks>
public class PosAlign : Pos
{
    /// <summary>
    ///     The cached location. Used to store the calculated location to avoid recalculating it.
    /// </summary>
    private int? _location;

    /// <summary>
    ///     Gets the identifier of a set of views that should be aligned together. When only a single
    ///     set of views is aligned, setting the <see cref="_groupId"/> is not needed because it defaults to 0.
    /// </summary>
    private readonly int _groupId;

    /// <summary>
    ///     Gets the alignment settings.
    /// </summary>
    public Aligner Aligner { get; } = new ();

    /// <summary>
    ///     Aligns the views in <paramref name="views"/> that have the same group ID as <paramref name="groupId"/>.
    /// </summary>
    /// <param name="groupId"></param>
    /// <param name="views"></param>
    /// <param name="dimension"></param>
    /// <param name="size"></param>
    private static void AlignGroup (int groupId, IList<View> views, Dimension dimension, int size)
    {
        if (views is null)
        {
            return;
        }

        Aligner firstInGroup = null;
        List<int> dimensionsList = new ();

        List<View> viewsInGroup = views.Where (
                                               v =>
                                               {
                                                   if (dimension == Dimension.Width && v.X is PosAlign alignX)
                                                   {
                                                       return alignX._groupId == groupId;
                                                   }

                                                   if (dimension == Dimension.Height && v.Y is PosAlign alignY)
                                                   {
                                                       return alignY._groupId == groupId;
                                                   }

                                                   return false;
                                               })
                                       .ToList ();

        if (viewsInGroup.Count == 0)
        {
            return;
        }

        foreach (View view in viewsInGroup)
        {
            PosAlign posAlign = dimension == Dimension.Width ? view.X as PosAlign : view.Y as PosAlign;

            if (posAlign is { })
            {
                if (firstInGroup is null)
                {
                    firstInGroup = posAlign.Aligner;
                }

                dimensionsList.Add (dimension == Dimension.Width ? view.Frame.Width : view.Frame.Height);
            }
        }

        if (firstInGroup is null)
        {
            return;
        }

        firstInGroup.ContainerSize = size;
        int [] locations = firstInGroup.Align (dimensionsList.ToArray ());

        for (var index = 0; index < viewsInGroup.Count; index++)
        {
            View view = viewsInGroup [index];
            PosAlign align = dimension == Dimension.Width ? view.X as PosAlign : view.Y as PosAlign;

            if (align is { })
            {
                align._location = locations [index];
            }
        }
    }

    /// <summary>
    ///     Enables alignment of a set of views.
    /// </summary>
    /// <param name="alignment"></param>
    /// <param name="groupId">The unique identifier for the set of views to align according to <paramref name="alignment"/>.</param>
    public PosAlign (Alignment alignment, int groupId = 0)
    {
        Aligner.SpaceBetweenItems = true;
        Aligner.Alignment = alignment;
        _groupId = groupId;
        Aligner.PropertyChanged += Aligner_PropertyChanged;
    }

    private void Aligner_PropertyChanged (object? sender, PropertyChangedEventArgs e) { _location = null; }

    /// <inheritdoc/>
    public override bool Equals (object other)
    {
        return other is PosAlign align && _groupId == align._groupId && _location == align._location && align.Aligner.Alignment == Aligner.Alignment;
    }

    /// <inheritdoc/>
    public override int GetHashCode () { return Aligner.GetHashCode () ^ _groupId.GetHashCode (); }

    /// <inheritdoc/>
    public override string ToString () { return $"Align(groupId={_groupId}, alignment={Aligner.Alignment})"; }

    internal override int GetAnchor (int width) { return _location ?? 0 - width; }

    internal override int Calculate (int superviewDimension, Dim dim, View us, Dimension dimension)
    {
        if (_location.HasValue && Aligner.ContainerSize == superviewDimension)
        {
            return _location.Value;
        }

        if (us?.SuperView is null)
        {
            return 0;
        }

        AlignGroup (_groupId, us.SuperView.Subviews, dimension, superviewDimension);

        if (_location.HasValue)
        {
            return _location.Value;
        }

        return 0;
    }
}
