using System;
using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui;

public partial class Toplevel {
	/// <summary>
	/// Gets or sets if this Toplevel is a container for overlapped children.
	/// </summary>
	public bool IsOverlappedContainer { get; set; }

	/// <summary>
	/// Gets or sets if this Toplevel is in overlapped mode within a Toplevel container.
	/// </summary>
	public bool IsOverlapped => Application.OverlappedTop != null && Application.OverlappedTop != this && !Modal;
}

public static partial class Application {
	/// <summary>
	/// Gets the list of the Overlapped children which are not modal <see cref="Toplevel"/> from the
	/// <see cref="OverlappedTop"/>.
	/// </summary>
	public static List<Toplevel> OverlappedChildren {
		get {
			if (OverlappedTop != null) {
				var _overlappedChildren = new List<Toplevel> ();
				foreach (var top in _topLevels) {
					if (top != OverlappedTop && !top.Modal) {
						_overlappedChildren.Add (top);
					}
				}
				return _overlappedChildren;
			}
			return null;
		}
	}

	/// <summary>
	/// The <see cref="Toplevel"/> object used for the application on startup which
	/// <see cref="Toplevel.IsOverlappedContainer"/> is true.
	/// </summary>
	public static Toplevel OverlappedTop {
		get {
			if (Top is { IsOverlappedContainer: true }) {
				return Top;
			}
			return null;
		}
	}

	static bool OverlappedChildNeedsDisplay ()
	{
		if (OverlappedTop == null) {
			return false;
		}

		foreach (var top in _topLevels) {
			if (top != Current && top.Visible && (top.NeedsDisplay || top.SubViewNeedsDisplay || top.LayoutNeeded)) {
				OverlappedTop.SetSubViewNeedsDisplay ();
				return true;
			}
		}
		return false;
	}


	static bool SetCurrentOverlappedAsTop ()
	{
		if (OverlappedTop == null && Current != Top && Current?.SuperView == null && Current?.Modal == false) {
			Top = Current;
			return true;
		}
		return false;
	}

	/// <summary>
	/// Move to the next Overlapped child from the <see cref="OverlappedTop"/>.
	/// </summary>
	public static void OverlappedMoveNext ()
	{
		if (OverlappedTop != null && !Current.Modal) {
			lock (_topLevels) {
				_topLevels.MoveNext ();
				var isOverlapped = false;
				while (_topLevels.Peek () == OverlappedTop || !_topLevels.Peek ().Visible) {
					if (!isOverlapped && _topLevels.Peek () == OverlappedTop) {
						isOverlapped = true;
					} else if (isOverlapped && _topLevels.Peek () == OverlappedTop) {
						MoveCurrent (Top);
						break;
					}
					_topLevels.MoveNext ();
				}
				Current = _topLevels.Peek ();
			}
		}
	}

	/// <summary>
	/// Move to the previous Overlapped child from the <see cref="OverlappedTop"/>.
	/// </summary>
	public static void OverlappedMovePrevious ()
	{
		if (OverlappedTop != null && !Current.Modal) {
			lock (_topLevels) {
				_topLevels.MovePrevious ();
				var isOverlapped = false;
				while (_topLevels.Peek () == OverlappedTop || !_topLevels.Peek ().Visible) {
					if (!isOverlapped && _topLevels.Peek () == OverlappedTop) {
						isOverlapped = true;
					} else if (isOverlapped && _topLevels.Peek () == OverlappedTop) {
						MoveCurrent (Top);
						break;
					}
					_topLevels.MovePrevious ();
				}
				Current = _topLevels.Peek ();
			}
		}
	}

	/// <summary>
	/// Move to the next Overlapped child from the <see cref="OverlappedTop"/> and set it as the
	/// <see cref="Top"/> if it is not already.
	/// </summary>
	/// <param name="top"></param>
	/// <returns></returns>
	public static bool MoveToOverlappedChild (Toplevel top)
	{
		if (top.Visible && OverlappedTop != null && Current?.Modal == false) {
			lock (_topLevels) {
				_topLevels.MoveTo (top, 0, new ToplevelEqualityComparer ());
				Current = top;
			}
			return true;
		}
		return false;
	}


	/// <summary>
	/// Brings the superview of the most focused overlapped view is on front.
	/// </summary>
	public static void BringOverlappedTopToFront ()
	{
		if (OverlappedTop != null) {
			return;
		}
		var top = FindTopFromView (Top?.MostFocused);
		if (top != null && Top.Subviews.Count > 1 && Top.Subviews [Top.Subviews.Count - 1] != top) {
			Top.BringSubviewToFront (top);
		}
	}


	/// <summary>
	/// Gets the current visible Toplevel overlapped child that matches the arguments pattern.
	/// </summary>
	/// <param name="type">The type.</param>
	/// <param name="exclude">The strings to exclude.</param>
	/// <returns>The matched view.</returns>
	public static Toplevel GetTopOverlappedChild (Type type = null, string [] exclude = null)
	{
		if (OverlappedTop == null) {
			return null;
		}

		foreach (var top in OverlappedChildren) {
			if (type != null && top.GetType () == type && exclude?.Contains (top.Data.ToString ()) == false) {
				return top;
			}
			if (type != null && top.GetType () != type || exclude?.Contains (top.Data.ToString ()) == true) {
				continue;
			}
			return top;
		}
		return null;
	}
}