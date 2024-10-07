# Navigation Deep Dive

- What are the visual cues that help the user know which element of an application is receiving keyboard and mouse input (which one has focus)? 
- How does the user change which element of an application has focus?
- How does the user change which element of an application has focus?
- What are the visual cues that help the user know what keystrokes will change the focus?
- What are the visual cues that help the user know what keystrokes will cause action in elements of the application that don't currently have focus?
- What is the order in which UI elements are traversed when using keyboard navigation?
- What are the default actions for standard key/mouse input (e.g. Hotkey, `Space`, `Enter`, `MouseClick`)?

## Lexicon & Taxonomy

- **Navigation** refers to the user experience for moving focus between views in the application view-hierarchy. 
- **Focus** - Refers to the state where a particular UI element (`View`), such as a button, input field, or any interactive component, is actively selected and ready to receive user input. When an element has focus, it typically responds to keyboard events and other interactions.
- **Focus Chain** - The ordered sequence of UI elements that can receive focus, starting from the currently focused element and extending to its parent (SuperView) elements up to the root of the focus tree (`Application.Top`). This chain determines the path that focus traversal follows within the application. Only one focus chain in an application can have focus (`top.HasFocus == true`), and there is one, and only one, View in a focus chain that is the most-focused; the one receiving keyboard input. 
- **Cursor** - A visual indicator to the user where keyboard input will have an impact. There is one Cursor per terminal session. See [Cursor](cursor.md) for a deep-dive.
- **Focus Ordering** - The order focusable Views are navigated. Focus Ordering is typically used in UI frameworks to enable screen readers and improve the Accessibility of an application. In v1, `TabIndex`/`TabIndexes` enabled Focus Ordering. 
- **Tab** - Describes the `Tab` key found on all keyboards, a break in text that is wider than a space, or a UI element that is a stop-point for keyboard navigation. The use of the word "Tab" for this comes from the typewriter, and is reinforced by the existence of a `Tab` key on all keyboards.
- **TabStop** - A `View` that is an ultimate stop-point for keyboard navigation. In this usage, ultimate means the `View` has no focusable subviews. The `Application.NextTabStopKey` and `Application.PrevTabStopKey` are `Key.Tab` and `Key.Tab.WithShift` respectively. These keys navigate only between peer-views. 
- **TabGroup** - A `View` that is a container for other focusable views. The `Application.NextTabGroupKey` and `Application.PrevTabGroupKey` are `Key.PageDown.WithCtrl` and `Key.PageUp.WithCtrl` respectively. These keys enable the user to use the keyboard to navigate up and down the view-hierarchy. 
- **Enter** / **Gain** - Means a View that previously was not focused is now becoming focused. "The View is entering focus" is the same as "The View is gaining focus". These terms are legacy terms from v1.
- **Leave** / **Lose** - Means a View that previously was focused is now becoming un-focused. "The View is leaving focus" is the same as "The View is losing focus". These terms are legacy terms from v1.

## Tenets for Terminal.Gui UI Navigation (Unless you know better ones...)

See the [Keyboard Tenets](keyboard.md) as they apply as well.

Tenets higher in the list have precedence over tenets lower in the list.

* **One Focus Per App** - It should not be possible to have two views be the "most focused" view in an application.

* **There's Always a Way With The Keyboard** - The framework strives to ensure users' wanting to use the keyboard can't get into a situation where some element of the application is not accessible via the keyboard. For example, we have unit tests that ensure built-in Views will all have at least one navigation key that advances focus. Another example: As long as a View with a HotKey is visible and enabled, regardless of view-hierarchy, if the user presses that hotkey, the action defined by the hotkey will happen (and, by default the View that defines it will be focused). 

* **Flexible Overrides** - The framework makes it easy for navigation changes to be made from code and enables changing of behavior to be done in flexible ways. For example a view can be prevented from getting focus by setting `CanFocus` to `false` or overriding `OnHasFocusChanging` and returning `true` to cancel. 

* **Decouple Concepts** - In v1 `CanFocus` is tightly coupled with `HasFocus`, `TabIndex`, `TabIndexes`, and `TabStop` and vice-versa. There was a bunch of "magic" logic that automatically attempted to keep these concepts aligned. This resulted in a poorly specified, hard-to-test, and fragile API. In v2 we strive to keep the related navigation concepts decoupled. For example, `CanFocus` and `TabStop` are decoupled. A view with `CanFocus == true` can have `TabStop == NoStop` and still be focusable with the mouse.

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

In v2, `HotKey`s can be used to navigate across the entire application view-hierarchy. They work independently of `Focus`. This enables a user to navigate across a complex UI of nested subviews if needed (even in overlapped scenarios). An example use case is the `AllViewsTester` scenario.

Additionally, in v2, multiple Views in an application (even within the same SuperView) can have the same HotKey. Each press of the HotKey will invoke the next HotKey across the View hierarchy (NOT IMPLEMENTED YET see https://github.com/gui-cs/Terminal.Gui/issues/3554).

## Mouse Navigation

Mouse-based navigation is straightforward in comparison to keyboard: If a view is focusable and the user clicks on it, it gains focus. There are some nuances, though:

- If a View is focusable, and it has focusable sub-views, what happens when a user clicks on the `Border` of the View? Which sub-view (if any) will also get focus? 

- If a View is focusable, and it has focusable sub-views, what happens when a user clicks on the `ContentArea` of the View? Which sub-view (if any) will also get focus? 

The answer to both questions is:

If the View was previously focused, the system keeps a record of the Subview that was previously most-focused and restores focus to that Subview (`RestoreFocus()`).

If the View was not previously focused, `AdvanceFocus()` is called.

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
return Application.Current?.AdvanceFocus (direction, behavior);
```

This method is called from the `Command` handlers bound to the application-scoped keybindings created during `Application.Init`. It is `public` as a convenience.

This method replaces about a dozen functions in v1 (scattered across `Application` and `Toplevel`).

## `View`

At the View-level, navigation is encapsulated within `View.Navigation.cs`.


## What makes a View focusable?

First, only Views that are visible and enabled can gain focus. Both `Visible` and `Enabled` must be `true` for a view to be focusable. 

For visible and enabled Views, the `CanFocus` property is then used to determine whether the `View` is focusable. `CanFocus` must be `true` for a View to gain focus. However, even if `CanFocus` is `true`, other factors can prevent the view from gaining focus...

A visible, enabled, and `CanFocus == true` view can be focused if the user uses the mouse to clicks on it or if code explicitly calls `View.SetFocus()`. Of course, the view itself or some other code can cancel the focus (e.g. by overriding `OnEnter`).

For keyboard navigation, the `TabStop` property is a filter for which views are focusable from the current most-focused. `TabStop` has no impact on mouse navigation. `TabStop` is of type `TabBehavior`.

* `null` - This View is still being initialized; acts as a signal to `set_CanFocus` to set `TabStop` to `TabBehavior.TabStop` as convince for the most common use-case. Equivalent to `TabBehavior.NoStop` when determining if a view is focusable by the keyboard or not.
* `TabBehavior.NoStop` - Prevents the user from using keyboard navigation to cause view (and by definition it's subviews) to gain focus. Note: The view can still be focused using code or the mouse.
* `TabBehavior.TabStop` - Indicates a View is a focusable view with no focusable subviews. `Application.Next/PrevTabStopKey` will advance ONLY through the peer-Views (`SuperView.Subviews`).

* `TabBehavior.GroupStop` - Indicates a View is a focusable container for other focusable views and enables keyboard navigation across these containers. This applies to both tiled and overlapped views. For example, `FrameView` is a simple view designed to be a visible container of other views tiled scenarios. It has `TabStop` set to `TabBehavior.GroupStop` (and `Arrangement` set to `ViewArrangement.Fixed`). Likewise, `Window` is a simple view designed to be a visible container of other views in overlapped scenarios. It has `TabStop` set to `TabBehavior.GroupStop` (and `Arrangement` set to `ViewArrangement.Movable | ViewArrangement.Resizable | ViewArrangement.Overlapped`). `Application.Next/PrevGroupStopKey` will advance across all `GroupStop` views in the application (unless blocked by a `NoStop` SuperView).

## How To Tell if a View has focus? And which view is the most-focused?

`View.HasFocus` indicates whether the `View` is focused or not. It is the definitive signal. If the view has no focusable Subviews then this property also indicates the view is the most-focused view in the application. 

Setting this property to `true` has the same effect as calling `View.SetFocus ()`, which also means the focus may not change as a result.

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

## Built-In Views Interactivity

|                |                         |            |               | **Keyboard** |                       |                              |                           | **Mouse**                    |                              |                              |                |               |
|----------------|-------------------------|------------|---------------|--------------|-----------------------|------------------------------|---------------------------|------------------------------|------------------------------|------------------------------|----------------|---------------|
|                | **Number<br>of States** | **Static** | **IsDefault** | **Hotkeys**  | **Select<br>Command** | **Accept<br>Command**        | **Hotkey<br>Command**     | **CanFocus<br>Click**        | **CanFocus<br>DblCLick**     | **!CanFocus<br>Click**       | **RightClick** | **GrabMouse** |
| **View**       | 1                       | Yes        | No            | 1            | OnSelect              | OnAccept                     | Focus                     | Focus                        |                              |                              |                | No            |
| **Label**      | 1                       | Yes        | No            | 1            | OnSelect              | OnAccept                     | FocusNext                 | Focus                        |                              | FocusNext                    |                | No            |
| **Button**     | 1                       | No         | Yes           | 1            | OnSelect              | Focus<br>OnAccept            | Focus<br>OnAccept         | HotKey                       |                              | Select                       |                | No            |
| **Checkbox**   | 3                       | No         | No            | 1            | OnSelect<br>Advance   | OnAccept                     | OnAccept                  | Select                       |                              | Select                       |                | No            |
| **RadioGroup** | > 1                     | No         | No            | 2+           | Advance               | Set SelectedItem<br>OnAccept | Focus<br>Set SelectedItem | SetFocus<br>Set _cursor      |                              | SetFocus<br>Set _cursor      |                | No            |
| **Slider**     | > 1                     | No         | No            | 1            | SetFocusedOption      | SetFocusedOption<br>OnAccept | Focus                     | SetFocus<br>SetFocusedOption |                              | SetFocus<br>SetFocusedOption |                | Yes           |
| **ListView**   | > 1                     | No         | No            | 1            | MarkUnMarkRow         | OpenSelectedItem<br>OnAccept | OnAccept                  | SetMark<br>OnSelectedChanged | OpenSelectedItem<br>OnAccept |                              |                | No            |

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


# Rough Design Notes 

## Accesibilty Tenets

See https://devblogs.microsoft.com/dotnet/the-journey-to-accessible-apps-keyboard-accessible/

https://github.com/dotnet/maui/issues/1646 

## Focus Chain & DOM ideas

The navigation/focus code in `View.Navigation.cs` has been rewritten in v2 (in https://github.com/gui-cs/Terminal.Gui/pull/3627) to simplify and make more robust.

The design is fundamentally the same as in v1: The logic for tracking and updating the focus chain is based on recursion up and down the `View.Subviews`/`View.SuperView` hierarchy. In this model, there is the need for tracking state during recursion, leading to APIs like the following:

```cs
// From v1/early v2: Note the `force` param.
private void SetHasFocus (bool newHasFocus, View view, bool force = false)

// From #3627: Note the `traversingUp` param
 private bool EnterFocus ([CanBeNull] View leavingView, bool traversingUp = false)
```

The need for these "special-case trackers" is clear evidence of poor architecture. Both implementations work, and the #3627 version is far cleaner, but a better design could result in further simplification. 

For example, moving to a model where `Application` is responsible for tracking and updating the focus chain instead `View`. We would introduce a formalization of the *Focus Chain*.

**Focus Chain**: A sequence or hierarchy of UI elements (Views) that determines the order in which keyboard focus is navigated within an application. This chain represents the potential paths that focus can take, ensuring that each element can be reached through keyboard navigation. Instead of using recursion, the Focus Chain can be implemented using lists or trees to maintain and update the focus state efficiently at the `Application` level.

By using lists or trees, you can manage the focus state without the need for recursive traversal, making the navigation model more scalable and easier to maintain. This approach allows you to explicitly define the order and structure of focusable elements, providing greater control over the navigation flow.

Now, the interesting thing about this, is it really starts to look like a DOM!

Designing a DOM (Document Object Model) for UI library involves creating a structured representation of the UI elements and their relationships. 

1. Hierarchy and Structure- Root Node: The top-level node representing the entire application or window.
    - View Nodes: Each UI element (View) is a node in the DOM. These nodes can have child nodes, representing nested or contained elements.
2. Node Properties- Attributes: Each node can have attributes such as id, class, style, and custom properties specific to the View.
    - State: Nodes can maintain state information, such as whether they are focused, visible, enabled, etc.
3. Traversal Methods- Parent-Child Relationships: Nodes maintain references to their parent and children, allowing traversal up and down the hierarchy.
    - Sibling Relationships: Nodes can also maintain references to their previous and next siblings for easier navigation.
4. Event Handling- Event Listeners: Nodes can have event listeners attached to handle user interactions like clicks, key presses, and focus changes.
    - Event Propagation: Events can propagate through the DOM, allowing for capturing and bubbling phases similar to web DOM events.
5. Focus Management- Focus Chain: Maintain a list or tree of focusable nodes to manage keyboard navigation efficiently.
    - Focus Methods: Methods to programmatically set and get focus, ensuring the correct element is focused based on user actions or application logic.
6. Mouse Events - Mouse handling in Terminal.Gui involves capturing and responding to mouse events such as clicks, drags, and scrolls. In v2, mouse events are managed at the View level, but for a DOM-like structure, this should be centralized.
7. Layout - The Pos/Dim system in Terminal.Gui is used for defining the layout of views. It allows for dynamic positioning and sizing based on various constraints. For a DOM-model we'd maintain the Pos/Dim system but ensure the layout calculations are managed by the DOM manager.
8. Drawing  - Drawing in Terminal.Gui involves rendering text, colors, and shapes. This is handled within the View class today. In a DOM model we'd centralize the drawing logic in the DOM manager to ensure consistent rendering.

This is all well and good, however we are NOT going to fully transition to a DOM in v2. But we may start with Focus/Navigation (item 3 above). Would could retain the existing external `View` API for focus (e.g. `View.SetFocus`, `Focused`, `CanFocus`, `TabIndexes`, etc...) but refactor the implementation of those to leverage a `FocusChain` (or `FocusManager`) at the `Application` level.

(Crap code generated by Copilot; but gets the idea across):

```cs
public class FocusChain {
    private List<View> focusableViews = new List<View>();
    private View currentFocusedView;

    public void RegisterView(View view) {
        if (view.CanFocus) {
            focusableViews.Add(view);
            focusableViews = focusableViews.OrderBy(v => v.TabIndex).ToList();
        }
    }

    public void UnregisterView(View view) {
        focusableViews.Remove(view);
    }

    public void SetFocus(View view) {
        if (focusableViews.Contains(view)) {
            currentFocusedView?.LeaveFocus();
            currentFocusedView = view;
            currentFocusedView.EnterFocus();
        }
    }

    public View GetFocusedView() {
        return currentFocusedView;
    }

    public void MoveFocusNext() {
        if (focusableViews.Count == 0) return;
        int currentIndex = focusableViews.IndexOf(currentFocusedView);
        int nextIndex = (currentIndex + 1) % focusableViews.Count;
        SetFocus(focusableViews[nextIndex]);
    }

    public void MoveFocusPrevious() {
        if (focusableViews.Count == 0) return;
        int currentIndex = focusableViews.IndexOf(currentFocusedView);
        int previousIndex = (currentIndex - 1 + focusableViews.Count) % focusableViews.Count;
        SetFocus(focusableViews[previousIndex]);
    }
}
```



# NOTES

v1 was all over the map for how the built-in Views dealt with common keyboard user-interactions such as pressing `Space`, `Enter`, or the `Hotkey`. Same for mouse interactions such as `Click` and`DoubleClick`.

I fixed a bunch of this a while back in v2 for `Accept` and `Hotkey` as part of making `Shortcut` and the new `StatusBar` work. `Shortcut` is a compbound View that needs to be able to host any view as `CommandView` and translate user-actions of those subviews in a consistent way. 

As I've been working on really making `Bar` support a replacement for `Menu`, `ContextMenu`, and `MenuBar` I've found that my work wasn't quite right and didn't go far enough.

This issue is to document and track what I've learned and lay out the design for addressing this correcxtly.

Related Issues:

- #2975 
- #3493 
- #2404 
- #3631 
- #3209 
- #385 

I started fixing this in 

- #3749 

However, I'm going to branch that work off to a new branch derived from `v2_develop` to address this issue separately. 

Here's a deep-dive into the existing built-in Views that indicate the inconsistencies.

|                |                         |            |               | **Keyboard** |                                      |                                                  |                                       | **Mouse**                     |                              |                               |                |               |
|----------------|-------------------------|------------|---------------|--------------|--------------------------------------|--------------------------------------------------|---------------------------------------|-------------------------------|------------------------------|-------------------------------|----------------|---------------|
|                | **Number<br>of States** | **Static** | **IsDefault** | **Hotkeys**  | **Select<br>Command<br>`Space`**     | **Accept<br>Command<br>`Enter`**                 | **Hotkey<br>Command**                 | **CanFocus<br>Click**         | **CanFocus<br>DblCLick**     | **!CanFocus<br>Click**        | **RightClick** | **GrabMouse** |
| **View**       | 1                       | Yes        | No            | 1            |                                      | OnAccept                                         | Focus                                 | Focus                         |                              |                               |                | No            |
| **Label**      | 1                       | Yes        | No            | 1            |                                      | OnAccept                                         | FocusNext                             | Focus                         |                              | FocusNext                     |                | No            |
| **Button**     | 1                       | No         | Yes           | 1            | Focus<br>OnAccept                    | Focus<br>OnAccept                                | Focus<br>OnAccept                     | Focus<br>OnAccept             |                              | OnAccept                      |                | No            |
| **Checkbox**   | 3                       | No         | No            | 1            | AdvanceCheckState<br>OnAccept        | AdvanceCheckState<br>OnAccept                    | AdvanceCheckState<br>OnAccept         | AdvanceCheckState<br>OnAccept |                              | AdvanceCheckState<br>OnAccept |                | No            |
| **RadioGroup** | > 1                     | No         | No            | 2+           | Set SelectedItem<br>OnAccept         | Set SelectedItem<br>OnAccept                     | Focus<br>Set SelectedItem<br>OnAccept | SetFocus<br>Set _cursor       |                              | SetFocus<br>Set _cursor       |                | No            |
| **Slider**     | > 1                     | No         | No            | 1            | SetFocusedOption<br>OnOptionsChanged | SetFocusedOption<br>OnOptionsChanged<br>OnAccept | Focus                                 | SetFocus<br>SetFocusedOption  |                              | SetFocus<br>SetFocusedOption  |                | Yes           |
| **ListView**   | > 1                     | No         | No            | 1            | MarkUnMarkRow                        | OpenSelectedItem<br>OnAccept                     | OnAccept                              | SetMark<br>OnSelectedChanged  | OpenSelectedItem<br>OnAccept |                               |                | No            |

Next, I'll post a table showing the proposed design.

This will involve adding `View.OnSelect` virtual method and a `Select` event to `View`.

## User Interaction Model

Here's what we're really talking about here: What is the correct user interaction model for common actions on Views within a container. See `navigation.md` for the baseline. Here we're going beyond that to focus on:

- What happens when there are bunch of SubViews and the user presses `Enter` with the intention of "accepting the current state".
- What happens when the user presses `Space` with the intention of changing the selection of the currently focused View. E.g. which list item is selected or the check state?
- What happens when the user presses `HotKey` with the intention of causing some non-focused View to EITHER "accept the current state" (`Button`), or "change a selection" (`RadioGroup`). 

Same for mouse interaction: 

- What happens when I click on a non-focused View?
- What if that view has `CanFocus == false`?

This gets really interesting when there's a View like a `Shortcut` that is a composite of several subviews. 

### New Model

|                |                         |            |               | **Keyboard** |                                                                                |                                                  |                                       | **Mouse**                                  |                                                                   |                                 |                |               |
|----------------|-------------------------|------------|---------------|--------------|--------------------------------------------------------------------------------|--------------------------------------------------|---------------------------------------|--------------------------------------------|-------------------------------------------------------------------|---------------------------------|----------------|---------------|
|                | **Number<br>of States** | **Static** | **IsDefault** | **Hotkeys**  | **Select<br>Command<br>`Space`**                                               | **Accept<br>Command<br>`Enter`**                 | **Hotkey<br>Command**                 | **CanFocus<br>Click**                      | **CanFocus<br>DblCLick**                                          | **!CanFocus<br>Click**          | **RightClick** | **GrabMouse** |
| **View**       | 1                       | Yes        | No            | 1            |                                                                                | OnAccept                                         | Focus                                 | SetFocus                                   |                                                                   |                                 |                | No            |
| **Label**      | 1                       | Yes        | No            | 1            |                                                                                | OnAccept                                         | FocusNext                             | SetFocus                                   |                                                                   | FocusNext                       |                | No            |
| **Button**     | 1                       | No         | Yes           | 1            | Focus<br>OnAccept                                                              | Focus<br>OnAccept                                | Focus<br>OnAccept                     | SetFocus<br>OnAccept                       |                                                                   | OnAccept                        |                | No            |
| **Checkbox**   | 3                       | No         | No            | 1            | AdvanceCheckState<br>OnSelect                                                  | OnAccept                                         | AdvanceCheckState<br>OnSelect         | AdvanceCheckState<br>OnSelect              |                                                                   | AdvanceCheckState<br>OnAccept   |                | No            |
| **RadioGroup** | > 1                     | No         | No            | 2+           | If cursor not selected,<br>select. Else, Advance <br>selected item<br>OnSelect | Set SelectedItem<br>OnSelect<br>OnAccept         | Focus<br>Set SelectedItem<br>OnSelect | Set Cursor<br>Set SelectedItem<br>OnSelect | SetFocus<br>SetCursor<br>Set SelectedItem<br>OnSelect<br>OnAccept | AdvanceSelectedItem<br>OnSelect |                | No            |
| **Slider**     | > 1                     | No         | No            | 1            | SetFocusedOption<br>OnOptionsChanged                                           | SetFocusedOption<br>OnOptionsChanged<br>OnAccept | Focus                                 | SetFocus<br>SetFocusedOption               |                                                                   | SetFocus<br>SetFocusedOption    |                | Yes           |
| **ListView**   | > 1                     | No         | No            | 1            | MarkUnMarkRow                                                                  | OpenSelectedItem<br>OnAccept                     | OnAccept                              | SetMark<br>OnSelectedChanged               | OpenSelectedItem<br>OnAccept                                      |                                 |                | No            |

## `View` - base class

### `!HasFocus`

* `Enter` - n/a because no focus
* `Space` - n/a because no focus
* `Hotkey` - `Command.Hotkey` which does `OnHotkey/Hotkey`
* `Click` - If `CanFocus`, sets focus, then invoke `Command.Hotkey`. If `!CanFocus` n/a.

### `HasFocus`

* `Enter` - `Command.Accept` which does `OnAccept/Accept`
* `Space` - `Command.Select` which does `OnSelect/Select`
* `Hotkey` - `Command.Hotkey` which does `OnHotkey/Hotkey`
* `Click` -  `Command.Hotkey`. 

## `Label` - Purpose is to be a "label" for another View. 

Said "label" can contain a Hotkey that will be forward to that other View. 

(Side note, with the `Border` adornment, and the decoupling of `Title` and `Text`, `Label` is not needed if the developer is OK with the Title appearing ABOVE the View... just enable `Border.Thickness.Top`. It is my goal that `Border` will support the `Title` being placed in `Border.Thick.ess.Left` at some point; which will eliminate the need for `Label` in many cases.)

### `!HasFocus`

99% of the time `Label` will be `!HasFocus`.

* `Enter` - n/a because no focus
* `Space` - n/a because no focus
* `Hotkey` - `Command.Hotkey` - Invoke the `Hotkey` Command on the next enabled & visible View (note, today AdvanceFocus is called which is not quite rigtht`
* `Click` - If `CanFocus`, sets focus. If `!CanFocus` Invoke the `Hotkey` Command on the next enabled & visible View (note, today AdvanceFocus is called which is not quite right).

### `HasFocus`

The below is debatable. An alternative is a `Label` with `CanFocus` effectively is a "meld" of the next view and `Enter`, `Space`, `HotKey`, and `Click` all just get forwarded to the next View. 

* `Enter` - `Command.Accept` which does `OnAccept/Accept` 
* `Space` - `Command.Select` which does `OnSelect/Select`
* `Hotkey` - `Command.Hotkey` - 
* `Click` - If `CanFocus`, sets focus. If `!CanFocus` Invoke the `Hotkey` Command on the next enabled & visible View (note, today AdvanceFocus is called which is not quite right).

## `Button` - A View where the user expects some action to happen when pressed.

Note: `Button` has `IsDefault` which does two things: 

1) change how a `Button` appears (adds an indicator indicating it's the default`). 
2) `Window`'s `Command.Accept` handler searches the subviews for the first `Button` with `IsDefault` and invokes `Command.Accept` on that button. If no such `Button` is found, or none do `Handled=true`, the `Window.OnAccept` is invoked. 

The practical impact of the above is devs have a choice for how to tell if the user "accepts" a superview:

a) Set `IsDefault` on one button, and subscribe to `Accept` on that button.
b) Subscribe to `Accept` on the superview. 

The `Dialogs` Scenario is illustrative:

For the `app` (Window):

```cs
        showDialogButton.Accepting += (s, e) =>
                                   {
                                       Dialog dlg = CreateDemoDialog (
                                                                      widthEdit,
                                                                      heightEdit,
                                                                      titleEdit,
                                                                      numButtonsEdit,
                                                                      glyphsNotWords,
                                                                      alignmentGroup,
                                                                      buttonPressedLabel
                                                                     );
                                       Application.Run (dlg);
                                       dlg.Dispose ();
                                   };
```

Changing this to 

```cs
        app.Accepting += (s, e) =>
                                   {
                                       Dialog dlg = CreateDemoDialog (
                                                                      widthEdit,
                                                                      heightEdit,
                                                                      titleEdit,
                                                                      numButtonsEdit,
                                                                      glyphsNotWords,
                                                                      alignmentGroup,
                                                                      buttonPressedLabel
                                                                     );
                                       Application.Run (dlg);
                                       dlg.Dispose ();
                                   };
```

... should do exactly the same thing. However, there's a bug in `v2_develop` where the `Command.Accept` handler for `Window` ignores the return value of `defaultBtn.InvokeCommand (Command.Accept)`. Fixing this bug makes this work as I would expect.

However, for `Dialog` the `Dialogs` scenario illustrates why a dev might actually want multiple buttons and to have one be `Default`:

```cs
                button.Accepting += (s, e) =>
                                 {
                                     clicked = buttonId;
                                     Application.RequestStop ();
                                 };

...

dialog.Closed += (s, e) => { buttonPressedLabel.Text = $"{clicked}"; };
```

With this, the `Accept` handler sets `clicked` so the dev can tell what button the user clicked to end the Dialog. 

Removing the code in `Window`'s `Command.Accept` handler that special-cases `IsDefault` changes nothing. Any subview that `Handles = true` `Accept` will, BY DEFINITION be the "default" `Enter` handler. 

If `Enter` is pressed and no Subview handles `Accept` with `Handled = true`, the Superview (e..g `Dialog` or `Window`) will get `Command.Accept`. Thus developers need to do nothing to make it so `Enter` "accepts". 

ANOTHER BUG in v2_develop: This code in `View.Mouse` is incorect as it ignores if an `MouseClick` handler sets `Handled = true`. 

```cs
           // If mouse is still in bounds, generate a click
           if (!WantContinuousButtonPressed && Viewport.Contains (mouseEvent.Position))
           {
                return OnMouseClick (new (MouseEvent));
           }

           return mouseEvent.Handled = true;
```

This is more correct:

```cs
            // If mouse is still in bounds, generate a click
            if (!WantContinuousButtonPressed && Viewport.Contains (mouseEvent.Position))
            {
                var meea = new MouseEventEventArgs (mouseEvent);

                // We can ignore the return value of OnMouseClick; if the click is handled
                // meea.Handled and meea.MouseEvent.Handled will be true
                OnMouseClick (meea);
            }
```

AND, `Dialogs` should set `e.Handled = true` in the `Accept` handler. 

Finally, `Button`'s (or any View that wants to be an explicit-"IsDefault" view) `HotKey` handler needs to do this:

```cs
        AddCommand (
                    Command.HotKey,
                    () =>
                    {
                        bool cachedIsDefault = IsDefault; // Supports "Swap Default" in Buttons scenario

                        bool? handled = OnAccept ();

                        if (handled == true)
                        {
                            return true;
                        }

                        SetFocus ();

                        // TODO: If `IsDefault` were a property on `View` *any* View could work this way. That's theoretical as 
                        // TODO: no use-case has been identified for any View other than Button to act like this.
                        // If Accept was not handled...
                        if (cachedIsDefault && SuperView is { })
                        {
                            return SuperView.InvokeCommand (Command.Accept);
                        }

                        return false;
                    });
```

With these changes, both mouse and keyboard "default accept" handling work without `View`, `Window` or anyone else knowing about `Button.IsDefault`.

## `CheckBox` - An interesting use case because it has potentially 3 states...

Here's what it SHOULD do:

### `!HasFocus`

* `Enter` - n/a because no focus
* `Space` - n/a because no focus
* `Hotkey` - `Command.Hotkey` -> does NOT set focus, but advances state
* `Click` - If `CanFocus`, sets focus AND advances state
* `Double Click` - Advances state and then raises `Accept` (this is what Office does; it's pretty nice. Windows does nothing).

### `HasFocus`

* `Enter` - `Command.Accept` -> Raises `Accept` 
* `Space` - `Command.Select` -> Advances state
* `Hotkey` - `Command.Hotkey` -> Advances state
* `Click` - Advances state
* `Double Click` - Advances state and then raises `Accept` (this is what Office does; it's pretty nice. Windows does nothing).

An interesting tid-bit about the above is for `Checkbox` the right thing to do is for Hotkey to NOT set focus. Why? If the user is in a TextField and wants to change a setting via a CheckBox, they should be able to use the hotkey and NOT have to then re-focus back on the TextView. The `TextView` in `Text Input Controls` Scenario is a good example of this.

## `RadioGroup` - Has > 1 state AND multiple hotkeys

In v2_develop it's all kinds of confused. Here's what it SHOULD do:

### `!HasFocus`

* `Enter` - n/a because no focus
* `Space` - n/a because no focus
* `Title.Hotkey` - `Command.Hotkey` -> Set focus. Do NOT advance state.
* `RadioItem.Hotkey` - `Command.Select` -> DO NOT set Focus. Advance State to RadioItem with hotkey.
* `Click` - `Command.Hotkey` -> If `CanFocus`, sets focus and advances state to clicked RadioItem.
* `Double Click` - Advances state to clicked RadioItem and then raises `Accept` (this is what Office does; it's pretty nice. Windows does nothing).

### `HasFocus`

* `Enter` - `Command.Accept` -> Advances state to selected RadioItem and Raises `Accept` 
* `Space` - `Command.Select` -> Advances state
* `Title.Hotkey` - `Command.Hotkey` -> Advance state
* `RadioItem.Hotkey` - `Command.Select` -> Advance State to RadioItem with hotkey.
* `Click` - advances state to clicked RadioItem.
* `Double Click` - Advances state to clicked RadioItem and then raises `Accept` (this is what Office does; it's pretty nice. Windows does nothing).

Like `Checkbox` the right thing to do is for Hotkey to NOT set focus. Why? If the user is in a TextField and wants to change a setting via a RadioGroup, they should be able to use the hotkey and NOT have to then re-focus back on the TextView. The `TextView` in `Text Input Controls` Scenario is a good example of this.

## `Slider` - Should operate just like RadioGroup

- BUGBUG: Slider should support Hotkey w/in Legends

## `NumericUpDown`

## `ListView`

### `!HasFocus`

* `Enter` - n/a because no focus
* `Space` - n/a because no focus
* `Title.Hotkey` - `Command.Hotkey` -> Set focus. Do NOT advance state.
* `Click` - `Command.Select` -> If `CanFocus`, sets focus and advances state to clicked ListItem.
* `Double Click` - Sets focus and advances state to clicked ListItem and then raises `Accept`.

### `HasFocus`

* `Enter` - `Command.Accept` -> Raises `Accept` 
* `Space` - `Command.Select` -> Advances state
* `Title.Hotkey` - `Command.Hotkey` -> does nothing
* `RadioItem.Hotkey` - `Command.Select` -> Advance State to RadioItem with hotkey.
* `Click` - `Command.Select` -> If `CanFocus`, sets focus and advances state to clicked ListItem.
* `Double Click` - Sets focus and advances state to clicked ListItem and then raises `Accept`.

What about `ListView.MultiSelect` and `ListViews.AllowsMarking`?