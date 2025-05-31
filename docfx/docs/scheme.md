# Scheme Deep Dive

See [Drawing](drawing.md) for an overview of the drawing system and [Configuration](config.md) for an overview of the configuration system.

## Overview

[!INCLUDE [Scheme Overview](~/includes/scheme-overview.md)]

### Scheme Inheritance

 A `Scheme` enables consistent, semantic theming of UI elements by associating each visual state with a specific style. Each property (e.g., `Normal`  or `Focus`) is an @Terminal.Gui.Drawing.Attribute. 

 Only `Normal` is required. If other properties are not explicitly set, its value is derived from other roles (typically `Normal`) using well-defined inheritance rules. See the source code for the `Scheme` class for more details. 

### Flexible Scheme Management in `Terminal.Gui.View`

A `View`'s appearance is primarily determined by its `Scheme`, which maps semantic `VisualRole`s (like `Normal`, `Focus`, `Disabled`) to specific `Attribute`s (foreground color, background color, and text style). `Terminal.Gui` provides a flexible system for managing these schemes:

1.  **Scheme Inheritance (Default Behavior)**:
    *   By default, if a `View` does not have a `Scheme` explicitly set, it inherits the `Scheme` from its `SuperView` (its parent in the view hierarchy).
    *   This cascading behavior allows for consistent styling across related views. If no `SuperView` has a scheme, (e.g., if the view is a top-level view), it ultimately falls back to the "Base" scheme defined in `SchemeManager.GetCurrentSchemes()`.
    *   The `GetScheme()` method implements this logic:
        *   It first checks if a scheme has been explicitly set via the `_scheme` field (see point 2).
        *   If not, and if `SchemeName` is set, it tries to resolve the scheme by name from `SchemeManager`.
        *   If still no scheme, it recursively calls `SuperView.GetScheme()`.
        *   As a final fallback, it uses `SchemeManager.GetCurrentSchemes()["Base"]`.

2.  **Explicit Scheme Assignment**:
    *   You can directly assign a `Scheme` object to a `View` using the `View.Scheme` property (which calls `SetScheme(value)`). This overrides any inherited scheme. The `HasScheme` property will then return `true`.
    *   Alternatively, you can set the `View.SchemeName` property to the name of a scheme registered in `SchemeManager`. If `Scheme` itself hasn't been directly set, `GetScheme()` will use `SchemeName` to look up the scheme. This is useful for declarative configurations (e.g., from a JSON file).
    *   The `SetScheme(Scheme? scheme)` method updates the internal `_scheme` field. If the new scheme is different from the current one, it marks the view for redraw (`SetNeedsDraw()`) to reflect the visual change. It also handles a special case for `Border` to ensure its scheme is updated if it `HasScheme`.

3.  **Event-Driven Customization**:
    The scheme resolution and application process includes events that allow for fine-grained control and customization:

    *   **`GettingScheme` Event (`View.Scheme.cs`)**:
        *   This event is raised within `GetScheme()` *before* the default logic (inheritance, `SchemeName` lookup, or explicit `_scheme` usage) fully determines the scheme.
        *   Subscribers (which could be the `SuperView`, a `SubView`, or any other interested component) can handle this event.
        *   In the event handler, you can:
            *   **Modify the scheme**: Set `args.NewScheme` to a different `Scheme` object.
            *   **Cancel default resolution**: Set `args.Cancel = true`. If canceled, the `Scheme` provided in `args.NewScheme` (which might have been modified by the handler) is returned directly by `GetScheme()`.
        *   The `OnGettingScheme(out Scheme? scheme)` virtual method is called first, allowing derived classes to provide a scheme directly.

    *   **`SettingScheme` Event (`View.Scheme.cs`)**:
        *   This event is raised within `SetScheme(Scheme? scheme)` *before* the `_scheme` field is actually updated.
        *   Subscribers can cancel the scheme change by setting `args.Cancel = true` in the event handler.
        *   The `OnSettingScheme(in Scheme? scheme)` virtual method is called first, allowing derived classes to prevent the scheme from being set.

4.  **Retrieving and Applying Attributes for Visual Roles (`View.Attribute.cs`)**:
    Once a `View` has determined its active `Scheme` (via `GetScheme()`), it uses this scheme to get specific `Attribute`s for rendering different parts of itself based on their `VisualRole`.

    *   **`GetAttributeForRole(VisualRole role)`**:
        *   This method first retrieves the base `Attribute` for the given `role` from the `View`'s current `Scheme` (`GetScheme()!.GetAttributeForRole(role)`).
        *   It then raises the `GettingAttributeForRole` event (and calls the `OnGettingAttributeForRole` virtual method).
        *   Subscribers to `GettingAttributeForRole` can:
            *   **Modify the attribute**: Change the `args.NewValue` (which is passed by `ref` as `schemeAttribute` to the event).
            *   **Cancel default behavior**: Set `args.Cancel = true`. The (potentially modified) `args.NewValue` is then returned.
        *   Crucially, if the `View` is `Enabled == false` and the requested `role` is *not* `VisualRole.Disabled`, this method will recursively call itself to get the `Attribute` for `VisualRole.Disabled`. This ensures disabled views use their designated disabled appearance.

    *   **`SetAttributeForRole(VisualRole role)`**:
        *   This method is used to tell the `ConsoleDriver` which `Attribute` to use for subsequent drawing operations (like `AddRune` or `AddStr`).
        *   It first determines the appropriate `Attribute` for the `role` from the current `Scheme` by calling `GetAttributeForRole`.

    *   **`SetAttribute(Attribute attribute)`**:
        *   This is a more direct way to set the driver's current attribute, bypassing the scheme and role system. It's generally preferred to use `SetAttributeForRole` to maintain consistency with the `Scheme`.

### Impact of SuperViews and SubViews via Events

*   **SuperView Influence**: A `SuperView` can subscribe to its `SubView`'s `GettingScheme` or `GettingAttributeForRole` events. This would allow a `SuperView` to dynamically alter how its children determine their schemes or specific attributes, perhaps based on the `SuperView`'s state or other application logic. For example, a container view might want all its children to adopt a slightly modified version of its own scheme under certain conditions.

*   **SubView Influence (Less Common for Scheme of Parent)**: While a `SubView` *could* subscribe to its `SuperView`'s scheme events, this is less typical for influencing the `SuperView`'s *own* scheme. It's more common for a `SubView` to react to changes in its `SuperView`'s scheme if needed, or to manage its own scheme independently.

*   **General Event Usage**: These events are powerful for scenarios where:
    *   A specific `View` instance needs a unique, dynamically calculated appearance that isn't easily captured by a static `Scheme` object.
    *   External logic needs to intercept and modify appearance decisions.
    *   Derived `View` classes want to implement custom scheme or attribute resolution logic by overriding the `On...` methods.

In summary, `Terminal.Gui` offers a layered approach to scheme management: straightforward inheritance and explicit setting for common cases, and a robust event system for advanced customization and dynamic control over how views derive and apply their visual attributes. This allows developers to achieve a wide range of visual styles and behaviors.

