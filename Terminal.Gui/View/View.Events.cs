#nullable enable

namespace Terminal.Gui;

using System.ComponentModel;
using System.Runtime.CompilerServices;

[MustDisposeResource (false)]
public partial class View
{
    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc/>
    public event PropertyChangingEventHandler? PropertyChanging;

    /// <summary>
    ///     Event raised only once, when the <see cref="View"/> is being initialized for the first time.<br/>
    ///     Allows configurations and assignments to be performed before the <see cref="View"/> being shown.
    /// </summary>
    /// <remarks>
    ///     <see cref="View"/> implements <see cref="ISupportInitializeNotification"/> to allow for more sophisticated initialization.
    /// </remarks>
    public event EventHandler? Initialized;

    /// <summary>
    ///     Cancelable event raised when the <see cref="Command.Accept"/> command is invoked. Set
    ///     <see cref="HandledEventArgs.Handled"/>
    ///     to cancel the event.
    /// </summary>
    public event EventHandler<HandledEventArgs>? Accept;

    /// <summary>Event raised when the <see cref="Enabled"/> value is being changed.</summary>
    public event EventHandler? EnabledChanged;

    /// <summary>Event raised after the <see cref="View.Title"/> has been changed.</summary>
    public event EventHandler<EventArgs<string>>? TitleChanged;

    /// <summary>
    ///     Event raised when the <see cref="View.Title"/> is changing.
    /// </summary>
    /// <remarks>
    ///     Set <see cref="CancelEventArgs.Cancel"/> to <see langword="true"/> to cancel the event and prevent <see cref="Title"/>
    ///     change.
    /// </remarks>
    public event EventHandler<CancelEventArgs<string>>? TitleChanging;

    /// <summary>Event fired when the <see cref="Visible"/> value is being changed.</summary>
    public event EventHandler? VisibleChanged;

    /// <summary>
    ///     Called when the <see cref="Command.Accept"/> command is invoked. Raises <see cref="Accept"/>
    ///     event.
    /// </summary>
    /// <returns>
    ///     If <see langword="true"/> the event was canceled. If <see langword="false"/> the event was raised but not canceled.
    ///     If <see langword="null"/> no event was raised.
    /// </returns>
    protected bool? OnAccept ()
    {
        HandledEventArgs args = new ();
        Accept?.Invoke (this, args);

        // BUG: Don't do this without synchronization.
        // If a consumer subscribes with an async method that returns immediately,
        // this will continue on its merry way even though things aren't finished.
        return Accept is null ? null : args.Handled;
    }

    /// <summary>Raises <see cref="EnabledChanged"/> when the <see cref="Enabled"/> property from a view is changed.</summary>
    protected virtual void OnEnabledChanged () { EnabledChanged?.Invoke (this, EventArgs.Empty); }

    /// <summary>Raises <see cref="TitleChanged"/>.</summary>
    /// <remarks><see cref="View"/> calls this method when the <see cref="View.Title"/> property has been changed.</remarks>
    protected void OnTitleChanged () { TitleChanged?.Invoke (this, new (in _title)); }

    /// <summary>
    ///     Raises the <see cref="TitleChanging"/> event, which can be cancelled.
    /// </summary>
    /// <param name="newTitle">The new <see cref="View.Title"/> to be replaced.</param>
    /// <remarks>
    ///     <see cref="View"/> calls this method before the <see cref="View.Title"/> changes.
    /// </remarks>
    /// <returns>`true` if an event handler canceled the Title change.</returns>
    protected bool OnTitleChanging (ref string newTitle)
    {
        CancelEventArgs<string> args = new (ref _title, ref newTitle);
        TitleChanging?.Invoke (this, args);

        return args.Cancel;
    }

    /// <summary>Raises <see cref="VisibleChanged"/>.</summary>
    /// <remarks>
    ///     <see cref="View"/> calls this method when the <see cref="Visible"/> property is changed.<br/>
    ///     Types overriding this method MUST call <see langword="base"/>.<see cref="OnVisibleChanged"/> to ensure
    ///     <see cref="VisibleChanged"/> is raised.
    /// </remarks>
    protected virtual void OnVisibleChanged () { VisibleChanged?.Invoke (this, EventArgs.Empty); }

    /// <summary>
    ///     Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="sender">The specific object requesting that the event be raised.</param>
    /// <param name="e">
    ///     Data for the <see cref="PropertyChanged"/> event. If this method is called explicitly (ie not by calling
    ///     <see cref="SetField{T}"/>), you MUST supply the correct and exact
    ///     name of the property that was changed.
    /// </param>
    /// <remarks>
    ///     Do not hide this method. To perform your own actions before and after raising the event, implement your own method that calls
    ///     this method when it is ready for the event to be
    ///     raised.
    /// </remarks>
    protected void RaisePropertyChanged (View? sender, PropertyChangedEventArgs e) { PropertyChanged?.Invoke (sender, e); }

    /// <summary>
    ///     Raises the <see cref="PropertyChanging"/> event.
    /// </summary>
    /// <param name="sender">The specific object requesting that the event be raised.</param>
    /// <param name="e">
    ///     Data for the <see cref="PropertyChanging"/> event. If this method is called explicitly (ie not by calling
    ///     <see cref="SetField{T}"/>), you MUST supply the correct and exact
    ///     name of the property that is about to change.
    /// </param>
    /// <remarks>
    ///     Do not hide this method. To perform your own actions before and after raising the event, implement your own method that calls
    ///     this method when it is ready for the event to be
    ///     raised.
    /// </remarks>
    protected void RaisePropertyChanging (View? sender, PropertyChangingEventArgs e) { PropertyChanging?.Invoke (sender, e); }

    /// <summary>
    ///     Sets the referenced <paramref name="field"/> to <paramref name="value"/> **BY VALUE** and raises
    ///     <see cref="PropertyChanged"/>.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the field and value. The value passed to <paramref name="value"/> can be another type, so long as it is
    ///     assignable to the exact type of <paramref name="field"/>.
    /// </typeparam>
    /// <param name="field">
    ///     A reference to the field to be changed. Note that this is a ref parameter and may access anything up to the scope of the
    ///     caller context at time of assignment.
    /// </param>
    /// <param name="value">
    ///     The new value to assign to <paramref name="field"/>. Value types will be copied by the compiler. Reference types will copy
    ///     only the reference.
    /// </param>
    /// <param name="propertyName">
    ///     The name of the property being changed. Values supplied explicitly will be ignored by the compiler and will be replaced by
    ///     the unqualified name of the member that called this
    ///     method.
    /// </param>
    /// <returns>
    ///     A <see langowrd="bool"/> value indicating whether the value was changed and an event raised.<br/>
    ///     <see langword="true"/>, If changed and event was raised;<br/>
    ///     <see langword="false"/>, otherwise.
    /// </returns>
    /// <remarks>
    ///     Argument passed to the <paramref name="value"/> parameter is a normal by-value parameter.<br/>
    ///     Argument passed to the <paramref name="field"/> parameter is a <see langword="ref"/> <typeparamref name="T"/> and must be an
    ///     assignable field, parameter, or local variable in
    ///     a reachable from the caller's ref-safe context.<br/>
    ///     While you may be able to fool the compiler into letting you _compile_ code that breaks the rules, in certain situations, tht
    ///     code WILL break at run-time.<br/>
    ///     So, just use this to set fields of the class you're in and everything will be great.
    /// </remarks>
    protected bool SetField<T> (ref T field, T value, [CallerMemberName] string? propertyName = null) where T : IEquatable<T>
    {
        if (field.Equals (value))
        {
            return false;
        }

        RaisePropertyChanging (this, new (propertyName));
        field = value;
        RaisePropertyChanged (this, new (propertyName));

        return true;
    }
}
