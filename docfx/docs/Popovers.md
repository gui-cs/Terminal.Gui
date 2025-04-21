# Popovers Deep Dive

Normally Views cannot draw outside of their `Viewport`. Options for influencing content outside of the `Viewport` include:

1) Modifying the `Border` behavior
2) Modifying the `Margin` behavior
3) Using @Terminal.Gui.Application.Popover

Popovers are useful for scenarios such as menus, autocomplete popups, and drop-down combo boxes.

A Popover is any View that meets these characteristics"

- Implements the @Terminal.Gui.IPopover interface 
- Is Focusable (`CetFocus = true`)
- Is Transparent (`ViewportSettings = ViewportSettings.Transparent | ViewportSettings.TransparentMouse`
- Sets `Visible = false` when it receives `Application.QuitKey`

@Terminal.Gui.PopoverMenu provides a sophisticated implementation.