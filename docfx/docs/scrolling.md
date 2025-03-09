# Scrolling

Terminal.Gui provides a rich system for how [View](View.md) users can scroll content with the keyboard and/or mouse.

## Lexicon & Taxonomy

See [View Deep Dive](View.md) for broader definitions.

* *Scroll* (Verb) - The act of causing content to move either horizontally or vertically within the @Terminal.Gui.View.Viewport. Also referred to as "Content Scrolling".
* *ScrollSlider* - A visual indicator that shows the proportion of the scrollable content to the size of the @Terminal.Gui.View.Viewport and allows the user to use the mouse to scroll. 
* *[ScrollBar](~/api/Terminal.Gui.ScrollBar.yml)* -  Indicates the size of scrollable content and controls the position of the visible content, either vertically or horizontally. At each end, a @Terminal.Gui.Button is provided, one to scroll up or left and one to scroll down or right. Between the
 buttons is a @Terminal.Gui.ScrollSlider that can be dragged to control the position of the visible content. The ScrollSlider is sized to show the proportion of the scrollable content to the size of the @Terminal.Gui.View.Viewport.

## Overview

The ability to scroll content is built into View. The @Terminal.Gui.View.Viewport represents the scrollable "viewport" into the View's Content Area (which is defined by the return value of @Terminal.Gui.View.GetContentSize ). 

By default, [View](~/api/Terminal.Gui.View.yml), includes no bindings for the typical directional keyboard and mouse input and cause the Content Area.

Terminal.Gui also provides the ability show a visual scroll bar that responds to mouse input. This ability is not enabled by default given how precious TUI screen real estate is.

Scrolling with the mouse and keyboard are enabled by:

1) Making the @Terminal.Gui.View.Viewport size smaller than the size returned by @Terminal.Gui.View.GetContentSize. 
2) Creating key bindings for the appropriate directional keys, and calling @Terminal.Gui.View.ScrollHorizontal(System.Int32) / @Terminal.Gui.View.ScrollVertical(System.Int32) as needed.
3) Subscribing to @Terminal.Gui.View.MouseEvent and calling calling @Terminal.Gui.View.ScrollHorizontal(System.Int32) / @Terminal.Gui.View.ScrollVertical(System.Int32) as needed.
4) Enabling the ScrollBars built into View by making @Terminal.Gui.View.HorizontalScrollBar or @Terminal.Gui.View.VerticalScrollBar visible or by enabling automatic show/hide behavior (seethe @Terminal.Gui.ScrollBar.AutoShow property).

While @Terminal.Gui.ScrollBar can be used in a standalone manner to provide proportional scrolling, it is typically enabled automatically via the @Terminal.Gui.View.HorizontalScrollBar and @Terminal.Gui.View.VerticalScrollBar properties.

## Examples

These Scenarios illustrate Terminal.Gui scrolling:

* *Scrolling* - Demonstrates the @Terminal.Gui.ScrollBar objects built into-View.
* *ScrollBar Demo* - Demonstrates using @Terminal.Gui.ScrollBar view in a standalone manner.
* *ViewportSettings* - Demonstrates the various @Terminal.Gui.ViewportSettings (see below) in an interactive manner. Used by the development team to visually verify that convoluted View layout and arrangement scenarios scroll properly.
* *Character Map* - Demonstrates a sophisticated scrolling use-case. The entire set of Unicode code-points can be scrolled and searched. From a scrolling perspective, this Scenario illustrates how to manually configure Viewport, Content Area, and Viewport Settings to enable horizontal and vertical headers (as might appear in a spreadsheet), full keyboard and mouse support, and more. 
* *ListView* and *HexEdit* - The source code to these built-in Views are good references for how to support scrolling and ScrollBars in a re-usable View sub-class. 

## ViewportSettings

Use @Terminal.Gui.ViewportSettings to adjust the behavior of scrolling. 

* `AllowNegativeX/Y` - If set, Viewport.Size can be set to negative coordinates enabling scrolling beyond the top-left of the content area.

* `AllowX/YGreaterThanContentWidth` - If set, @Terminal.Gui.View.Viewport `.Size` can be set to values greater than @Terminal.Gui.View.GetContentSize enabling scrolling beyond the bottom-right of the Content Area. When not set, @Terminal.Gui.View.Viewport `.Location` is constrained to the dimension of the content area - 1. This means the last column of the content will remain visible even if there is an attempt to scroll the Viewport past the last column. The practical effect of this is that the last column/row of the content will always be visible.

* `ClipContentOnly` - By default, clipping is applied to @Terminal.Gui.View.Viewport. Setting this flag will cause clipping to be applied to the visible content area.

* `ClearContentOnly`- If set @Terminal.Gui.View.ClearViewport will clear only the portion of the content area that is visible within the Viewport. This is useful for views that have a content area larger than the Viewport and want the area outside the content to be visually distinct.

