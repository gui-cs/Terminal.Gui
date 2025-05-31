# Scrolling

Terminal.Gui provides a rich system for how [View](View.md) users can scroll content with the keyboard and/or mouse.

## See Also

- [View Deep Dive](View.md)
- [Layout](layout.md)
- [Arrangement](arrangement.md)

## Lexicon & Taxonomy

[!INCLUDE [Scrolling Lexicon](./../includes/scrolling-lexicon.md)]

## Overview

The ability to scroll content is built into View. The @Terminal.Gui.ViewBase.View.Viewport represents the scrollable "viewport" into the View's Content Area (which is defined by the return value of @Terminal.Gui.ViewBase.View.GetContentSize ). 

By default, [View](~/api/Terminal.Gui.ViewBase.View.yml), includes no bindings for the typical directional keyboard and mouse input and cause the Content Area.

Terminal.Gui also provides the ability show a visual scroll bar that responds to mouse input. This ability is not enabled by default given how precious TUI screen real estate is.

Scrolling with the mouse and keyboard are enabled by:

1) Making the @Terminal.Gui.ViewBase.View.Viewport size smaller than the size returned by @Terminal.Gui.ViewBase.View.GetContentSize. 
2) Creating key bindings for the appropriate directional keys, and calling @Terminal.Gui.ViewBase.View.ScrollHorizontal(System.Int32) / @Terminal.Gui.ViewBase.View.ScrollVertical(System.Int32) as needed.
3) Subscribing to @Terminal.Gui.ViewBase.View.MouseEvent and calling calling @Terminal.Gui.ViewBase.View.ScrollHorizontal(System.Int32) / @Terminal.Gui.ViewBase.View.ScrollVertical(System.Int32) as needed.
4) Enabling the ScrollBars built into View by making @Terminal.Gui.ViewBase.View.HorizontalScrollBar or @Terminal.Gui.ViewBase.View.VerticalScrollBar visible or by enabling automatic show/hide behavior (seethe @Terminal.Gui.Views.ScrollBar.AutoShow property).

While @Terminal.Gui.Views.ScrollBar can be used in a standalone manner to provide proportional scrolling, it is typically enabled automatically via the @Terminal.Gui.ViewBase.View.HorizontalScrollBar and @Terminal.Gui.ViewBase.View.VerticalScrollBar properties.

## Examples

These `UI Catalog` Scenarios illustrate Terminal.Gui scrolling:

* *Scrolling* - Demonstrates the @Terminal.Gui.Views.ScrollBar objects built into-View.
* *ScrollBar Demo* - Demonstrates using @Terminal.Gui.Views.ScrollBar view in a standalone manner.
* *ViewportSettings* - Demonstrates the various @Terminal.Gui.ViewBase.ViewportSettingsFlags (see below) in an interactive manner. Used by the development team to visually verify that convoluted View layout and arrangement scenarios scroll properly.
* *Character Map* - Demonstrates a sophisticated scrolling use-case. The entire set of Unicode code-points can be scrolled and searched. From a scrolling perspective, this Scenario illustrates how to manually configure Viewport, Content Area, and Viewport Settings to enable horizontal and vertical headers (as might appear in a spreadsheet), full keyboard and mouse support, and more. 
* *ListView* and *HexEdit* - The source code to these built-in Views are good references for how to support scrolling and ScrollBars in a re-usable View sub-class. 

## ViewportSettings

Use @Terminal.Gui.ViewportSettings to adjust the behavior of scrolling. 

* `AllowNegativeX/Y` - If set, Viewport.Size can be set to negative coordinates enabling scrolling beyond the top-left of the content area.

* `AllowX/YGreaterThanContentWidth` - If set, @Terminal.Gui.ViewBase.View.Viewport `.Size` can be set to values greater than @Terminal.Gui.ViewBase.View.GetContentSize enabling scrolling beyond the bottom-right of the Content Area. When not set, @Terminal.Gui.ViewBase.View.Viewport `.Location` is constrained to the dimension of the content area - 1. 

  This means the last column of the content will remain visible even if there is an attempt to scroll the Viewport past the last column. The practical effect of this is that the last column/row of the content will always be visible.

* `ClipContentOnly` - By default, clipping is applied to @Terminal.Gui.ViewBase.View.Viewport. Setting this flag will cause clipping to be applied to the visible content area.

* `ClearContentOnly`- If set @Terminal.Gui.ViewBase.View.ClearViewport will clear only the portion of the content area that is visible within the Viewport. This is useful for views that have a content area larger than the Viewport and want the area outside the content to be visually distinct.

