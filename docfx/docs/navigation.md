# Navigation Deep Dive

**Navigation** refers to the user-experience for moving Focus between views in the application view-hierarchy. It applies to the following questions:

- What are the visual cues that help the user know which element of an application will receive keyboard and mouse input (which one has focus)? 
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

* **There's Always a Way With The Keyboard** - The framework strives to ensure users' wanting to use the keyboard can't get into a situation where some element of the application is not accessible via the keyboard. For example, we have unit tests that ensure built-in Views will all have at least one navigation key that advances focus.

* **Flexible Overrides** - The framework makes it easy for navigation changes to be made from code and enables changing of behavior to be done in flexible ways. For example a view can be prevented from getting focus by setting `CanFocus` to `false`, overriding `OnEnter` and returning `true` to cancel, or subscribing to `Enter` and setting `Cancel` to `true`. 

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
- `Application.NextTabGroupKey` (`Key.PageDown.WithCtrl`) - Navigates to the next view in the view-hierarchy that is a `TabGroup` (see below). If there is no next, the first view that is a `TabGroup` will gain focus.
- `Application.PrevTabGroupKey` (`Key.PageUp.WithCtrl`) - Opposite of `Application.NextTabGroupKey`.

These keys are all registered as `KeyBindingScope.Application` key bindings by `Application`. Because application-scoped key bindings have the lowest priority, Views can override the behaviors of these keys (e.g. `TextView` overrides `Key.Tab` by default, enabling the user to enter `\t` into text). The `AllViews_AtLeastOneNavKey_Leaves` unit test ensures all built-in Views have at least one of the above keys that can advance. 

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

Gets the most-focused View in the application. Will return `null` if there is no view with focus (an extremely rare situation).

### `Application.Navigation.Focused`

A cancelable event raised when the most-focused View in the application changes. Useful for apps that want to do something with the most-focused view (e.g. see `AdornmentsEditor`) or apps that want to override what view can be focused across an entire app. 

### `Application.Navigation.Advance (NavigationDirection direction, TabBehavior? behavior)`

Causes the focus to advance (forward or backwards) to the next View in the application view-hierarchy, using `behavior` as a filter.

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

* `TabBehavior.GroupStop` - Indicates a View is a focusable container for other focusable views and enables keyboard navigation across these containers. This applies to both tiled and overlapped views. For example, `FrameView` is a simple view designed to be a visible container of other views tiled scenarios. It has `TabStop` set to `TabBehavior.GroupStop` (and `Arrangement` set to `ViewArrangement.Fixed`). Likewise, `Window` is a simple view designed to be a visible container of other views in overlapped scenarios. It has `TabStop` set to `TabBehavior.GroupStop` (and `Arrangement` set to `ViewArrangement.Movable | ViewArrangement.Resizable | ViewArrangement.Overlapped`). `Application.Next/PrevGroupStopKey` will advance across all `GroupStop` views in the application (unless blocked by a `NoStop` Superview).

## How To Tell if a View has focus? And which view is the most-focused?

`View.HasFocus` indicates whether the `View` is focused or not. It is the definitive signal. If the view has no focusable Subviews then this property also indicates the view is the most-focused view in the application. 

Setting this property to `true` has the same effect as calling `View.SetFocus ()`, which also means the focus may not actually change as a result.

If `v.HasFocus == true` then

- All views down `v`'s view-hierarchy must be focusable.
- All views down `v`'s view-hierarchy will also have `HasFocus == true`.
- The deepest-subview of `v` that is focusable will also have `HasFocus == true`

In other words, `v.HasFocus == true` does not necessarily mean `v` is the most-focused view, receiving input. If it has focusable sub-views, one of those (or a further subview) will be the most-focused (`Application.Navigation.Focused`).

The `private bool _hasFocus` field backs `HasFocus` and is the ultimate source of truth whether a View has focus or not.

## How to make a View become focused?

The primary `public` method for developers to cause a view to get focus is `View.SetFocus()`. 

Unlike v1, in v2, this method can return `false` if the focus change doesn't happen (e.g. because the view wasn't focusable, or the focus change was cancelled).

## How to make a View become NOT focused?

The typical method to make a view lose focus is to have another View gain focus. 

## Determining the Most Focused SubView

In v1 `View` had `MostFocused` property that traversed up the view-hierachy returning the last view found with `HasFocus == true`. In v2, `Application.Focused` provides the same functionality with less overhead.

