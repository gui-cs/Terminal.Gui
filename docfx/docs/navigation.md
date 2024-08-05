# Navigation Deep Dive

**Navigation** refers to the user-experience for moving Focus between views in the application view-hierarchy. It applies to the following questions:

- What are the visual cues that help the user know which element of an application is receiving keyboard and mouse input (which one has focus)? 
- How does the user change which element of an application has focus?
- How does the user change which element of an application has focus?
- What are the visual cues that help the user know what keystrokes will change the focus?
- What are the visual cues that help the user know what keystrokes will cause action in elements of the application that don't currently have focus?
- What is the order in which UI elements are traversed when using keyboard navigation?

## Lexicon & Taxonomy

- **Navigation** - Refers to the user-experience for moving Focus between views in the application view-hierarchy.
- **Focus** - Indicates which view-hierarchy is receiving keyboard input. Only one view-hierarchy in an application can have focus (`top.HasFocus == true`), and there  one, and only one, View in a focused hierarchy that is the most-focused; the one receiving keyboard input. 
- **Cursor** - A visual indicator to the user where keyboard input will have an impact. There is one Cursor per terminal session. See [Cursor](cursor.md) for a deep-dive.
- **Tab** - Describes the `Tab` key found on all keyboards, a break in text that is wider than a space, or a UI element that is a stop-point for keyboard navigation. The use of the word "Tab" for this comes from the typewriter, and is re-enforced by the existence of a `Tab` key on all keyboards.
- **TabStop** - A `View` that is an ultimate stop-point for keyboard navigation. In this usage, ultimate means the `View` has no focusable subviews. The `Application.NextTabStopKey` and `Application.PrevTabStopKey` are `Key.Tab` and `Key.Tab.WithShift` respectively. These keys navigate only between peer-views. 
- **TabGroup** - A `View` that is a container for other focusable views. The `Application.NextTabGroupKey` and `Application.PrevTabGroupKey` are `Key.PageDown.WithCtrl` and `Key.PageUp.WithCtrl` respectively. These keys enable the user to use the keyboard to navigate up and down the view-hierarchy. 
- **Enter** / **Gain** - Means a View that previously was not focused is now becoming focused. "The View is entering focus" is the same as "The View is gaining focus".
- **Leave** / **Lose** - Means a View that previously was focused is now becoming un-focused. "The View is leaving focus" is the same as "The View is losing focus".

## Tenets for Terminal.Gui UI Navigation (Unless you know better ones...)

See the [Keyboard Tenets](keyboard.md) as they apply as well.

Tenets higher in the list have precedence over tenets lower in the list.

* **One Focus Per App** - It should not be possible to have two views be the "most focused" view in an application.

* **There's Always a Way With The Keyboard** - The framework strives to ensure users' wanting to use the keyboard can't get into a situation where some element of the application is not accessible via the keyboard. For example, we have unit tests that ensure built-in Views will all have at least one navigation key that advances focus. Another example: As long as a View with a HotKey is visible and enabled, regardless of view hierarchy, if the user presses that hotkey, the action defined by the hotkey will happen (and, by default the View that defines it will be focused). 

* **Flexible Overrides** - The framework makes it easy for navigation changes to be made from code and enables changing of behavior to be done in flexible ways. For example a view can be prevented from getting focus by setting `CanFocus` to `false`, overriding `OnEnter` and returning `true` to cancel, or subscribing to `Enter` and setting `Cancel` to `true`. 

* **Decouple Concepts** - In v1 `CanFocus` is tightly coupled with `HasFocus`, `TabIndex`, `TabIndexes`, and `TabStop` and vice-versa. There is a bunch of "magic" logic that automatically attempts to keep these concepts aligned. This results in a bunch of poorly specified, hard to test, and fragile APIs. In v2 we strive to keep the related navigation concepts decoupled. For example, `CanFocus` and `TabStop` completely distinct. A view with `CanFocus == true` can have `TabStop == NoStop` and still be focusable with the mouse.

# Design

## Keyboard Navigation

The majority of the Terminal.Gui Navigation system is dedicated to enabling the keyboard to be used to navigate Views. 

Terminal.Gui defines these keys for keyboard navigation:

- `Application.NextTabStopKey` (`Key.Tab`) - Navigates to the next subview that is a `TabStop` (see below). If there is no next, the first subview that is a `TabStop` will gain focus.
- `Application.PrevTabStopKey` (`Key.Tab.WithShift`) - Opposite of `Application.NextTabStopKey`.
- `Key.CursorRight` - Operates identically to `Application.NextTabStopKey`.
- `Key.CursorDown` - Operates identically to `Application.NextTabStopKey`.
- `Key.CursorLeft` - Operates identically to `Application.PrevTabStopKey`.
- `Key.CursorUp` - Operates identically to `Application.PrevTabStopKey`.
- `Application.NextTabGroupKey` (`Key.F6`) - Navigates to the next view in the view-hierarchy that is a `TabGroup` (see below). If there is no next, the first view that is a `TabGroup` will gain focus.
- `Application.PrevTabGroupKey` (`Key.F6.WithShift`) - Opposite of `Application.NextTabGroupKey`.

`F6` was chosen to match [Windows](https://learn.microsoft.com/en-us/windows/apps/design/input/keyboard-accelerators#common-keyboard-accelerators)

These keys are all registered as `KeyBindingScope.Application` key bindings by `Application`. Because application-scoped key bindings have the lowest priority, Views can override the behaviors of these keys (e.g. `TextView` overrides `Key.Tab` by default, enabling the user to enter `\t` into text). The `AllViews_AtLeastOneNavKey_Leaves` unit test ensures all built-in Views have at least one of the above keys that can advance. 

### `HotKey`

See also [Keyboard](keyboard.md) where HotKey is covered more deeply...

In v2, `HotKey`s can be used to navigate across the entire application view-hierarchy. They work independently of `Focus`. This enables a user to navigate across a complex UI of nested subviews if needed (even in overlapped scenarios). An example use-case is the `AllViewsTester` scenario.

Additionally, in v2, multiple Views in an application (even within the same SuperView) can have the same HotKey. Each press of the HotKey will invoke the next HotKey across the View hierarchy (NOT IMPLEMENTED YET - And may be too complex to actually implement for v2.)

## Mouse Navigation

Mouse-based navigation is straightforward in comparison to keyboard: If a view is focusable and the user clicks on it, it gains focus. There are some nuances, though:

- If a View is focusable, and it has focusable sub-views, what happens when a user clicks on the `Border` of the View? Which sub-view (if any) will also get focus? 

- If a View is focusable, and it has focusable sub-views, what happens when a user clicks on the `ContentArea` of the View? Which sub-view (if any) will also get focus? 

The answer to both questions is:

If the View was previously focused, and focus left, the system keeps a record of the Subview that was previously most-focused and restores focus to that Subview (`RestoreFocus()`).

If the View was not previously focused, `FindDeepestFocusableView()` is used to find the deepest focusable view and call `SetFocus()` on it.

For this to work properly, there must be logic that removes the focus-cache used by `RestoreFocus()` if something changes that makes the previously-focusable view not focusable (e.g. if Visible has changed).

## `Application`

At the application level, navigation is encapsulated within the `ApplicationNavigation` helper class which is publicly exposed via the `Application.Navigation` property.

### `Application.Navigation.GetFocused ()`

Gets the most-focused View in the application. Will return `null` if there is no view with focus (an extremely rare situation). This replaces `View.MostFocused` in v1.

### `Application.Navigation.FocusedChanged` and `Application.Navigation.FocusedChanging`

Events raised when the most-focused View in the application is changing or has changed. `FocusedChanged` is useful for apps that want to do something with the most-focused view (e.g. see `AdornmentsEditor`). `FocusChanging` is useful apps that want to override what view can be focused across an entire app. 

### `Application.Navigation.AdvanceFocus (NavigationDirection direction, TabBehavior? behavior)`

Causes the focus to advance (forward or backwards) to the next View in the application view-hierarchy, using `behavior` as a filter.

The implementation is simple:

```cs
return Application.GetFocused()?.AdvanceFocus (direction, behavior) ?? false;
```

This method is called from the `Command` handlers bound to the application-scoped keybindings created during `Application.Init`. It is `public` as a convenience.

This method replaces about a dozen functions in v1 (scattered across `Application` and `Toplevel`).

## `View`

At the View-level, navigation is encapsulated within `View.Navigation.cs`.

## What makes a View focusable?

First, only Views that are visible and enabled can gain focus. Both `Visible` and `Enabled` must be `true` for a view to be focusable. 

For visible and enabled Views, the `CanFocus` property is then used to determine whether the `View` is focusable. `CanFocus` must be `true` for a View to gain focus. However, even if `CanFocus` is `true`, other factor can prevent the view from gaining focus...

A visible, enabled, and `CanFocus == true` view can be focused if the user uses the mouse to clicks on it or if code explicitly calls `View.SetFocus()`. Of course, the view itself or some other code can cancel the focus (e.g. by overriding `OnEnter`).

For keyboard navigation, the `TabStop` property is a filter for which views are focusable from the current most-focused. `TabStop` has no impact on mouse navigation. `TabStop` is of type `TabBehavior`.

* `null` - This View is still being initialized; acts as a signal to `set_CanFocus` to set `TabStop` to `TabBehavior.TabStop` as convince for the most common use-case. Equivalent to `TabBehavior.NoStop` when determining if a view is focusable by the keyboard or not.
* `TabBehavior.NoStop` - Prevents the user from using keyboard navigation to cause view (and by definition it's subviews) to gain focus. Note: The view can still be focused using code or the mouse.
* `TabBehavior.TabStop` - Indicates a View is a focusable view with no focusable subviews. `Application.Next/PrevTabStopKey` will advance ONLY through the peer-Views (`SuperView.Subviews`). 

* `TabBehavior.GroupStop` - Indicates a View is a focusable container for other focusable views and enables keyboard navigation across these containers. This applies to both tiled and overlapped views. For example, `FrameView` is a simple view designed to be a visible container of other views tiled scenarios. It has `TabStop` set to `TabBehavior.GroupStop` (and `Arrangement` set to `ViewArrangement.Fixed`). Likewise, `Window` is a simple view designed to be a visible container of other views in overlapped scenarios. It has `TabStop` set to `TabBehavior.GroupStop` (and `Arrangement` set to `ViewArrangement.Movable | ViewArrangement.Resizable | ViewArrangement.Overlapped`). `Application.Next/PrevGroupStopKey` will advance across all `GroupStop` views in the application (unless blocked by a `NoStop` SuperView).

## How To Tell if a View has focus? And which view is the most-focused?

`View.HasFocus` indicates whether the `View` is focused or not. It is the definitive signal. If the view has no focusable Subviews then this property also indicates the view is the most-focused view in the application. 

Setting this property to `true` has the same effect as calling `View.SetFocus ()`, which also means the focus may not actually change as a result.

If `v.HasFocus == true` then

- All views up `v`'s superview-hierarchy must be focusable.
- All views up `v`'s superview-hierarchy will also have `HasFocus == true`.
- The deepest-subview of `v` that is focusable will also have `HasFocus == true`

In other words, `v.HasFocus == true` does not necessarily mean `v` is the most-focused view, receiving input. If it has focusable sub-views, one of those (or a further subview) will be the most-focused (`Application.Navigation.Focused`).

The `private bool _hasFocus` field backs `HasFocus` and is the ultimate source of truth whether a View has focus or not.

### How does a user tell?

In short: `ColorScheme.Focused`.

(More needed for HasFocus SuperViews. The current `ColorScheme` design is such that this is awkward. See [Issue #2381](https://github.com/gui-cs/Terminal.Gui/issues/2381#issuecomment-1890814959))

## How to make a View become focused?

The primary `public` method for developers to cause a view to get focus is `View.SetFocus()`. 

Unlike v1, in v2, this method can return `false` if the focus change doesn't happen (e.g. because the view wasn't focusable, or the focus change was cancelled).

## How to make a View become NOT focused?

The typical method to make a view lose focus is to have another View gain focus. 

## Determining the Most Focused SubView

In v1 `View` had `MostFocused` property that traversed up the view-hierarchy returning the last view found with `HasFocus == true`. In v2, `Application.Focused` provides the same functionality with less overhead.

## How Does `View.Add/Remove` Work?

In v1, calling `super.Add (view)` where `view.CanFocus == true` caused all views up the hierarchy (all SuperViews) to get `CanFocus` set to `true` as well. 

Also, in v1, if `view.CanFocus == true`, `Add` would automatically set `TabStop`. 

In v2, developers need to explicitly set `CanFocus` for any view in the view-hierarchy where focus is desired. This simplifies the implementation significantly and removes confusing behavior. 

In v2, the automatic setting of `TabStop` in `Add` is retained because it is not overly complex to do so and is a nice convenience for developers to not have to set both `Tabstop` and `CanFocus`. Note we do NOT automatically change `CanFocus` if `TabStop` is changed.

## Overriding `HasFocus` changes - `OnEnter/OnLeave` and `Enter/Leave`

These virtual methods and events are raised when a View's `HasFocus` property is changing. In v1 they were poorly defined and weakly implemented. For example, `OnEnter` was `public virtual OnEnter` and it raised `Enter`. This meant overrides needed to know that the base raised the event and remember to call base. Poor API design. 

`FocusChangingEventArgs.Handled` in v1 was documented as

```cs
    /// <summary>
    ///     Indicates if the current focus event has already been processed and the driver should stop notifying any other
    ///     event subscriber. It's important to set this value to true specially when updating any View's layout from inside the
    ///     subscriber method.
    /// </summary>
```

This is clearly copy/paste documentation from keyboard code and describes incorrect behavior. In practice this is not what the implementation does. Instead the system never even checks the return value of `OnEnter` and `OnLeave`.

Additionally, in v1 `private void SetHasFocus (bool newHasFocus, View view, bool force = false)` is confused too complex. 

In v2, `SetHasFocus ()` is replaced by `private bool EnterFocus (View view)` and `private bool LeaveFocus (View view)`. These methods follow the standard virtual/event pattern:

- Check pre-conditions:
    - For `EnterFocus` - If the view is not focusable (not visible, not enabled, or `CanFocus == false`) returns `true` indicating the change was cancelled.
    - For `EnterFocus` - If `CanFocus == true` but the `SuperView.CanFocus == false` throws an invalid operation exception.
    - For `EnterFocus` - If `HasFocus` is already `true` throws an invalid operation exception.
    - For `LeaveFocus` - If `HasFocus` is already `false` throws an invalid operation exception.
- Call the `protected virtual bool OnEnter/OnLeave (View?)` method. If the return value is `true` stop and return `true`, preventing the focus change. The base implementations of these simply return `false`.
- Otherwise, raise the cancelable event (`Enter`/`Leave`). If `args.Cancel == true` stop and return `true`, preventing the focus change. 
- Check post-conditions: If `HasFocus` has not changed, throw an invalid operation exception.
- Return `false` indicating the change was not cancelled (or invalid).

The `Enter` and `Leave` events use `FocusChangingEventArgs` which provides both the old and new Views. `FocusChangingEventArgs.Handled` changes to `Cancel` to be more clear on intent.

These could also be named `Gain/Lose`. They could also be combined into a single method/event: `HasFocusChanging`. 

QUESTION: Should we retain the same names as in v1 to simplify porting? Or, given the semantics of `Handled` v. `Cancel` are reversed would it be better to rename and/or combine?

## `TabIndex` and `TabIndexes`

### v1 Behavior

In v1, within a set of focusable subviews that are TabStops, and within a view hierarchy containing TabGroups, the default order in which views gain focus is the same as the order the related views were added to the SuperView. As `superView.Add (view)` is called, each view is added to the end of the `TabIndexes` list. 

`TabIndex` allows this order to be changed without changing the order in `SubViews`. When `view.TabIndex` is set, the `TabIndexes` list is re-ordered such that `view` is placed in the list after the peer-view with `TabIndex-1` and before the peer-view with `TabIndex+1`. 

QUESTION: With this design, devs are required to ensure `TabIndex` is unique. It also means that `set_TabIndex` almost always will change the passed value. E.g. this code will almost always assert:

```cs
view.TabIndex = n;
Debug.Assert (view.TabIndex == n);
```

This is horrible API design. 

### Proposed New Design

In `Win32` there is no concept of tab order beyond the Z-order (the equivalent to the order superview.Add was called).

In `WinForms` the `Control.TabIndex` property:

> can consist of any valid integer greater than or equal to zero, lower numbers being earlier in the tab order. If more than one control on the same parent control has the same tab index, the z-order of the controls determines the order to cycle through the controls.

In `WPF` the `UserControl.Tabindex` property:

> When no value is specified, the default value is MaxValue. The system then attempts a tab order based on the declaration order in the XAML or child collections.

Terminal.Gui v2 should adopt the `WinForms` model.

# Implementation Plan

A bunch of the above is the proposed design. Eventually `Toplevel` will be deleted. Before that happens, the implementation will retain dual code paths:

- The old `Toplevel` and `OverlappedTop` code. Only utilized when `IsOverlappedContainer == true`
- The new code path that treats all Views the same but relies on the appropriate combination of `TabBehavior` and `ViewArrangement` settings as well as `IRunnable`.

