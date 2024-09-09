# Scrolling

Terminal.Gui provides a rich system for how [View](View.md) users can scroll content with the keyboard and/or mouse.

## Lexicon & Taxonomy

See [View Deep Dive](View.md) for broader definitions.

* *Scroll* (Verb) - The act of causing content to move either horizontally or vertically within the [View.Viewport](~/api/Terminal.Gui.View.Viewport.yml). Also referred to as "Content Scrolling".
* *[Scroll](~/api/Terminal.Gui.Scroll.yml)* (Noun) - Indicates the size of scrollable content and provides a visible element, referred to as the "ScrollSlider" that that is sized to show the proportion of the scrollable content to the size of the [View.Viewport](~/api/Terminal.Gui.View.Viewport.yml) and can be dragged with the mouse. A Scroll can be oriented either vertically or horizontally and is used within a [ScrollBar](~/api/Terminal.Gui.ScrollBar.yml).
* *ScrollSlider* - The visual indicator that shows the proportion of the scrollable content to the size of the [View.Viewport](~/api/Terminal.Gui.View.Viewport.yml) and allows the user to use the mouse to scroll. The Scroll Slider is not exposed publicly. 
* *[ScrollBar](~/api/Terminal.Gui.ScrollBar.yml)* - Provides a visual indicator that content can be scrolled. ScrollBars consist of two buttons, one each for scrolling forward or backwards, a Scroll that can be clicked to scroll large amounts, and a ScrollSlider that can be dragged to scroll continuously. ScrollBars can be oriented either horizontally or vertically and support the user dragging and clicking with the mouse to scroll.


## Overview

The ability to scroll content is built into View. The [View.Viewport](~/api/Terminal.Gui.View.Viewport.yml) represents the scrollable "viewport" into the View's Content Area (which is defined by the return value of [View.GetContentSize()](~/api/Terminal.Gui.View.GetContentSize.yml)). 

By default, [View](~/api/Terminal.Gui.View.yml), includes no bindings for the typical directional keyboard and mouse input and cause the Content Area.

Terminal.Gui also provides the ability show a visual scroll bar that responds to mouse input. This ability is not enabled by default given how precious TUI screen real estate is.

Scrolling with the mouse and keyboard are enabled by:

1) Making the [View.Viewport](~/api/Terminal.Gui.View.Viewport.yml) size smaller than the size returned by [View.GetContentSize()](~/api/Terminal.Gui.View.GetContentSize.yml). 
2) Creating key bindings for the appropriate directional keys (e.g. [Key.CursorDown](~/api/Terminal.Gui.Key)), and calling [View.ScrollHorizontal()](~/api/Terminal.Gui.View.ScrollHorizontal.yml)/[ScrollVertical()](~/api/Terminal.Gui.View.ScrollVertical.yml) as needed.
3) Subscribing to [View.MouseEvent](~/api/Terminal.Gui.View.MouseEvent.yml) and calling calling [View.ScrollHorizontal()](~/api/Terminal.Gui.View.ScrollHorizontal.yml)/[ScrollVertical()](~/api/Terminal.Gui.View.ScrollVertical.yml) as needed.
4) Enabling the [ScrollBar](~/api/Terminal.Gui.ScrollBar.yml)s built into View ([View.HorizontalScrollBar/VerticalScrollBar](~/api/Terminal.Gui.View.HorizontalScrollBar.yml)) by setting the flag [ViewportSettings.EnableScrollBars](~/api/Terminal.Gui.ViewportSettings.EnableScrollBars.yml) on [View.ViewportSettings](~/api/Terminal.Gui.View.ViewportSettings.yml). 

## Examples

These Scenarios illustrate Terminal.Gui scrolling:

* *Content Scrolling* - Demonstrates the various [Viewport Settings](~/api/Terminal.Gui.ViewportSettings.yml) (see below) in an interactive manner. Used by the development team to visually verify that convoluted View layout and arrangement scenarios scroll properly.
* *Character Map* - Demonstrates a sophisticated scrolling use-case. The entire set of Unicode code-points can be scrolled and searched. From a scrolling perspective, this Scenario illustrates how to manually configure `Viewport`, `SetContentArea()`, and `ViewportSettings` to enable horizontal and vertical headers (as might appear in a spreadsheet), full keyboard and mouse support, and more. 
* *Scroll Demo* - Designed to demonstrate using the `Scroll` view in a standalone manner.
* *ScrollBar Demo* - Designed to demonstrate using the `ScrollBar` view in a standalone manner.
* *Scrolling* - A legacy Scenario from v1 that is used to visually test that scrolling is working properly.
* *ListView* and *TableView* - The source code to these built-in Views are good references for how to support scrolling and ScrollBars in a re-usable View sub-class. 

## [Viewport Settings](~/api/Terminal.Gui.ViewportSettings.yml)

Use [View.ViewportSettings](~/api/Terminal.Gui.View.ViewportSettings.yml) to adjust the behavior of scrolling. 

* [AllowNegativeX/Y](~/api/Terminal.Gui.ViewportSettings.AllowNegativeXyml) - If set, Viewport.Size can be set to negative coordinates enabling scrolling beyond the top-left of the content area.

* [AllowX/YGreaterThanContentWidth](~/api/Terminal.Gui.ViewportSettings.AllowXGreaterThanContentWidth) - If set, Viewport.Size can be set values greater than GetContentSize() enabling scrolling beyond the bottom-right of the Content Area. When not set, `Viewport.X/Y` are constrained to the dimension of the content area - 1. This means the last column of the content will remain visible even if there is an attempt to scroll the Viewport past the last column. The practical effect of this is that the last column/row of the content will always be visible.

* [ClipContentOnly](~/api/Terminal.Gui.ViewportSettings.ClipContentOnly) - By default, clipping is applied to [Viewport](~/api/Terminal.Gui.View.Viewport.yml). Setting this flag will cause clipping to be applied to the visible content area.

* [ClearContentOnly](~/api/Terminal.Gui.ViewportSettings.ClearContentOnly) - If set [View.Clear()](~/api/Terminal.Gui.View.Clear.yml) will clear only the portion of the content area that is visible within the Viewport. This is useful for views that have a content area larger than the Viewport and want the area outside the content to be visually distinct.

* [EnableHorizontal/VerticalScrollBar](~/api/Terminal.Gui.ViewportSettings.EnableHorizontalScrollBar) - If set, the scroll bar will be enabled and automatically made visible when the corresponding dimension of [View.Viewport](~/api/Terminal.Gui.View.Viewport.yml) is smaller than the dimension of [View.GetContentSize()](~/api/Terminal.Gui.View.GetContentSize.yml).


## [ScrollBar](~/api/Terminal.Gui.ScrollBar.yml)

Provides a visual indicator that content can be scrolled. ScrollBars consist of two buttons, one each for scrolling forward or backwards, a Scroll that can be clicked to scroll large amounts, and a ScrollSlider that can be dragged to scroll continuously. ScrollBars can be oriented either horizontally or vertically and support the user dragging and clicking with the mouse to scroll.

While the *[Scroll](~/api/Terminal.Gui.Scroll.yml)* *[ScrollBar](~/api/Terminal.Gui.ScrollBar.yml)* Views can be used in a standalone manner to provide proportional scrolling, they are typically enabled automatically via the [View.HorizontalScrollBar](~/api/Terminal.Gui.View.HorizontalScrollBar.yml) and  [View.VerticalScrollBar](~/api/Terminal.Gui.View.VerticalScrollBar.yml) properties.

