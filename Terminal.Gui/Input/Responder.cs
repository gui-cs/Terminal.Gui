//
// Core.cs: The core engine for gui.cs
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
// Pending:
//   - Check for NeedDisplay on the hierarchy and repaint
//   - Layout support
//   - "Colors" type or "Attributes" type?
//   - What to surface as "BackgroundCOlor" when clearing a window, an attribute or colors?
//
// Optimziations
//   - Add rendering limitation to the exposed area

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Terminal.Gui;
/// <summary>
/// Responder base class implemented by objects that want to participate on keyboard and mouse input.
/// </summary>
public class Responder : IDisposable {
	bool disposedValue;

#if DEBUG_IDISPOSABLE
	/// <summary>
	/// For debug purposes to verify objects are being disposed properly
	/// </summary>
	public bool WasDisposed = false;
	/// <summary>
	/// For debug purposes to verify objects are being disposed properly
	/// </summary>
	public int DisposedCount = 0;
	/// <summary>
	/// For debug purposes
	/// </summary>
	public static List<Responder> Instances = new List<Responder> ();
	/// <summary>
	/// For debug purposes
	/// </summary>
	public Responder ()
	{
		Instances.Add (this);
	}
#endif

	/// <summary>
	/// Event raised when <see cref="Dispose()"/> has been called to signal that this object is being disposed.
	/// </summary>
	public event EventHandler Disposing;

	/// <summary>
	/// Gets or sets a value indicating whether this <see cref="Responder"/> can focus.
	/// </summary>
	/// <value><c>true</c> if can focus; otherwise, <c>false</c>.</value>
	public virtual bool CanFocus { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether this <see cref="Responder"/> has focus.
	/// </summary>
	/// <value><c>true</c> if has focus; otherwise, <c>false</c>.</value>
	public virtual bool HasFocus { get; }

	/// <summary>
	/// Gets or sets a value indicating whether this <see cref="Responder"/> can respond to user interaction.
	/// </summary>
	public virtual bool Enabled { get; set; } = true;

	/// <summary>
	/// Gets or sets a value indicating whether this <see cref="Responder"/> and all its child controls are displayed.
	/// </summary>
	public virtual bool Visible { get; set; } = true;

	/// <summary>
	/// Method invoked when a mouse event is generated
	/// </summary>
	/// <returns><c>true</c>, if the event was handled, <c>false</c> otherwise.</returns>
	/// <param name="mouseEvent">Contains the details about the mouse event.</param>
	public virtual bool MouseEvent (MouseEvent mouseEvent)
	{
		return false;
	}

	/// <summary>
	/// Called when the mouse first enters the view; the view will now
	/// receives mouse events until the mouse leaves the view. At which time, <see cref="OnMouseLeave(Gui.MouseEvent)"/>
	/// will be called.
	/// </summary>
	/// <param name="mouseEvent"></param>
	/// <returns><c>true</c>, if the event was handled, <c>false</c> otherwise.</returns>
	public virtual bool OnMouseEnter (MouseEvent mouseEvent)
	{
		return false;
	}

	/// <summary>
	/// Called when the mouse has moved outside of the view; the view will no longer receive mouse events (until
	/// the mouse moves within the view again and <see cref="OnMouseEnter(Gui.MouseEvent)"/> is called).
	/// </summary>
	/// <param name="mouseEvent"></param>
	/// <returns><c>true</c>, if the event was handled, <c>false</c> otherwise.</returns>
	public virtual bool OnMouseLeave (MouseEvent mouseEvent)
	{
		return false;
	}

	/// <summary>
	/// Method invoked when a view gets focus.
	/// </summary>
	/// <param name="view">The view that is losing focus.</param>
	/// <returns><c>true</c>, if the event was handled, <c>false</c> otherwise.</returns>
	public virtual bool OnEnter (View view)
	{
		return false;
	}

	/// <summary>
	/// Method invoked when a view loses focus.
	/// </summary>
	/// <param name="view">The view that is getting focus.</param>
	/// <returns><c>true</c>, if the event was handled, <c>false</c> otherwise.</returns>
	public virtual bool OnLeave (View view)
	{
		return false;
	}

	/// <summary>
	/// Method invoked when the <see cref="CanFocus"/> property from a view is changed.
	/// </summary>
	public virtual void OnCanFocusChanged () { }

	/// <summary>
	/// Method invoked when the <see cref="Enabled"/> property from a view is changed.
	/// </summary>
	public virtual void OnEnabledChanged () { }

	/// <summary>
	/// Method invoked when the <see cref="Visible"/> property from a view is changed.
	/// </summary>
	public virtual void OnVisibleChanged () { }

	// TODO: v2 - nuke this
	/// <summary>
	/// Utilty function to determine <paramref name="method"/> is overridden in the <paramref name="subclass"/>.
	/// </summary>
	/// <param name="subclass">The view.</param>
	/// <param name="method">The method name.</param>
	/// <returns><see langword="true"/> if it's overridden, <see langword="false"/> otherwise.</returns>
	internal static bool IsOverridden (Responder subclass, string method)
	{
		MethodInfo m = subclass.GetType ().GetMethod (method,
			BindingFlags.Instance
			| BindingFlags.Public
			| BindingFlags.NonPublic
			| BindingFlags.DeclaredOnly);
		if (m == null) {
			return false;
		}
		return m.GetBaseDefinition ().DeclaringType != m.DeclaringType;
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <remarks>
	/// If disposing equals true, the method has been called directly
	/// or indirectly by a user's code. Managed and unmanaged resources
	/// can be disposed.
	/// If disposing equals false, the method has been called by the
	/// runtime from inside the finalizer and you should not reference
	/// other objects. Only unmanaged resources can be disposed.
	/// </remarks>
	/// <param name="disposing"></param>
	protected virtual void Dispose (bool disposing)
	{
		if (!disposedValue) {
			if (disposing) {
				// TODO: dispose managed state (managed objects)
			}

			disposedValue = true;
		}
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resource.
	/// </summary>
	public void Dispose ()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Disposing?.Invoke (this, EventArgs.Empty);
		Dispose (disposing: true);
		GC.SuppressFinalize (this);
#if DEBUG_IDISPOSABLE
		WasDisposed = true;

		foreach (var instance in Instances.Where (x => x.WasDisposed).ToList ()) {
			Instances.Remove (instance);
		}
#endif
	}
}
