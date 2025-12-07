# WideGlyphs Draw Code Flow Documentation

This document explains the complete draw code flow from when `Application.Run` is called through to when the `DrawingContent` event handler fires in the `WideGlyphs` scenario.

## Overview

The `WideGlyphs` scenario demonstrates how wide (double-width) Unicode glyphs are rendered and clipped when overlapped by views. The key to understanding this is following the draw flow from the application main loop down to the individual view's `DrawingContent` event handler.

## Complete Call Stack

```
Application.Run(appWindow)
??> ApplicationImpl.Begin(runnable)
    ??> Raises IsRunningChanging/IsRunningChanged events
    ??> Pushes SessionToken onto SessionStack
    ??> Sets TopRunnable = appWindow
    ??> Calls LayoutAndDraw() for first time
??> ApplicationImpl.RunLoop(runnable, errorHandler)
    ??> while (runnable.IsRunning && !StopRequested)
        ??> Coordinator.RunIteration()
            ??> MainLoopCoordinator.Iteration()
                ??> ApplicationMainLoop.IterationImpl()
                    ??> InputProcessor.ProcessQueue() - Handle user input
                    ??> SizeMonitor.Poll() - Check for terminal resize
                    ??> App.LayoutAndDraw(forceRedraw: false) ? DRAWING HAPPENS HERE
                    ??> SetCursor() - Position and show/hide cursor
                    ??> TimedEvents.RunTimers() - Run timeout callbacks
```

## Detailed Flow

### 1. Application Initialization and Run

**File:** `ApplicationImpl.Run.cs`

```csharp
public object? Run(IRunnable runnable, Func<Exception, bool>? errorHandler = null)
{
    // Begin the session (adds to stack, raises events)
    SessionToken? token = Begin(runnable);
    
    try
    {
        // All runnables block until RequestStop() is called
        RunLoop(runnable, errorHandler);
    }
    finally
    {
        // End the session (raises events, pops from stack)
        End(token);
    }
    
    return token.Result;
}
```

**Key Points:**
- `Begin()` calls `LayoutAndDraw()` once to perform initial layout and draw
- `RunLoop()` enters an infinite loop processing iterations until stopped
- Each iteration processes input, checks for resize, and redraws if needed

### 2. Main Loop Iteration

**File:** `ApplicationMainLoop.cs`

```csharp
public void Iteration()
{
    App?.RaiseIteration();  // Fire Iteration event
    
    DateTime dt = DateTime.Now;
    int timeAllowed = 1000 / Math.Max(1, (int)Application.MaximumIterationsPerSecond);
    
    IterationImpl();  // Do the actual work
    
    TimeSpan took = DateTime.Now - dt;
    TimeSpan sleepFor = TimeSpan.FromMilliseconds(timeAllowed) - took;
    
    if (sleepFor.Milliseconds > 0)
    {
        Task.Delay(sleepFor).Wait();  // Throttle to respect MaximumIterationsPerSecond
    }
}

internal void IterationImpl()
{
    // 1. Process input events
    InputProcessor.ProcessQueue();
    
    // 2. Check for terminal size changes
    SizeMonitor.Poll();
    
    // 3. Layout and draw any views that need it ? THIS CALLS DRAWING
    App?.LayoutAndDraw(forceRedraw: false);
    
    // 4. Update the cursor position and visibility
    SetCursor();
    
    // 5. Run any timeout callbacks that are due
    TimedEvents.RunTimers();
}
```

**Key Points:**
- Iterations run in a loop at a maximum rate (default 25/sec)
- `LayoutAndDraw` is called on every iteration but only draws if needed
- Input and resize events are processed before drawing

### 3. Application-Level Layout and Draw

**File:** `ApplicationImpl.Screen.cs`

```csharp
public void LayoutAndDraw(bool forceRedraw = false)
{
    if (ClearScreenNextIteration)
    {
        forceRedraw = true;
        ClearScreenNextIteration = false;
    }
    
    if (forceRedraw)
    {
        Driver?.ClearContents();
    }
    
    // Get all views to layout/draw (SessionStack runnables + active Popover)
    List<View?> views = [.. SessionStack!.Select(r => r.Runnable! as View)!];
    
    if (Popover?.GetActivePopover() as View is { Visible: true } visiblePopover)
    {
        visiblePopover.SetNeedsDraw();
        visiblePopover.SetNeedsLayout();
        views.Insert(0, visiblePopover);
    }
    
    // Layout phase - computes positions and sizes
    bool neededLayout = View.Layout(views.ToArray().Reverse()!, Screen.Size);
    
    // Draw phase - renders to output buffer
    bool needsDraw = forceRedraw || views.Any(v => v is { NeedsDraw: true } or { SubViewNeedsDraw: true });
    
    if (Driver is { } && (neededLayout || needsDraw))
    {
        Driver.Clip = new Region(Screen);  // ? INITIALIZE DRIVER CLIP
        
        // Draw all views (static method that iterates through views)
        View.Draw(views: views.ToArray().Cast<View>(), neededLayout || forceRedraw);
        
        Driver.Clip = new Region(Screen);  // Reset clip
        
        // Flush updates to the terminal
        Driver?.Refresh();
    }
}
```

**Key Points:**
- `Driver.Clip` is initialized to the full screen before drawing
- Layout computes positions/sizes without modifying the screen
- Draw only happens if layout changed or a view needs drawing
- All runnables in the `SessionStack` are drawn (bottom to top)
- Active popovers are drawn on top

### 4. Static View Draw Method

**File:** `View.Drawing.cs`

```csharp
internal static void Draw(IEnumerable<View> views, bool force)
{
    // Snapshot once — every recursion level gets its own frozen array
    View[] viewsArray = views.Snapshot();
    
    // The draw context tracks the region drawn by each view
    DrawContext context = new DrawContext();
    
    foreach (View view in viewsArray)
    {
        if (force)
        {
            view.SetNeedsDraw();
        }
        
        view.Draw(context);  // ? CALL INSTANCE DRAW METHOD
    }
    
    // Draw the margins last (for shadows on top)
    Margin.DrawMargins(viewsArray);
    
    // Clear NeedsDraw flags on all views
    foreach (View view in viewsArray)
    {
        view.ClearNeedsDraw();
    }
    
    // Clear SuperView.SubViewNeedsDraw if all subviews are drawn
    // (logic omitted for brevity)
}
```

**Key Points:**
- This is a static method that draws multiple peer views
- A `DrawContext` is created to track what regions were drawn
- Each view's instance `Draw(context)` method is called
- Margins with shadows are drawn in a second pass
- All `NeedsDraw` flags are cleared after drawing

### 5. Instance View Draw Method

**File:** `View.Drawing.cs`

```csharp
public void Draw(DrawContext? context = null)
{
    if (!CanBeVisible(this))
    {
        return;
    }
    
    Region? originalClip = GetClip();  // ? GET CURRENT CLIP (typically Screen)
    
    if (NeedsDraw || SubViewNeedsDraw)
    {
        // 1. Draw Border and Padding
        DoDrawAdornments(originalClip);
        SetClip(originalClip);
        
        // 2. Set clip to Viewport (prevent drawing outside it)
        originalClip = AddViewportToClip();  // ? INTERSECT WITH VIEWPORT
        // Now: Driver.Clip = previousClip ? ViewportToScreen(Viewport)
        
        context ??= new();
        
        SetAttributeForRole(Enabled ? VisualRole.Normal : VisualRole.Disabled);
        
        // 3. Clear the Viewport background
        DoClearViewport(context);
        
        // 4. Draw subviews first (recursive)
        if (SubViewNeedsDraw)
        {
            DoDrawSubViews(context);
        }
        
        // 5. Draw the text (View.Text property)
        SetAttributeForRole(Enabled ? VisualRole.Normal : VisualRole.Disabled);
        DoDrawText(context);
        
        // 6. Draw the content ? THIS FIRES DrawingContent EVENT
        DoDrawContent(context);
        
        // 7. Restore clip, draw LineCanvas, redraw adornment subviews
        SetClip(originalClip);
        originalClip = AddFrameToClip();
        DoRenderLineCanvas(context);
        DoDrawAdornmentsSubViews();
        
        // 8. Advance diagnostics indicator
        Border?.AdvanceDrawIndicator();
        
        ClearNeedsDraw();
    }
    
    // Cache clip for margin shadows
    Margin?.CacheClip();
    
    // Reset the clip to what it was when we started
    SetClip(originalClip);
    
    // We're done drawing
    DoDrawComplete(context);
}
```

**Key Points:**
- `GetClip()` returns the current `Driver.Clip` (initially `Screen`)
- `AddViewportToClip()` intersects the clip with this view's Viewport
- The clip ensures drawing operations stay within the visible area
- Drawing order: Adornments ? Clear ? SubViews ? Text ? **Content** ? LineCanvas
- The clip is restored after content is drawn

### 6. DoDrawContent - Fires DrawingContent Event

**File:** `View.Drawing.cs`

```csharp
private void DoDrawContent(DrawContext? context = null)
{
    if (!NeedsDraw || OnDrawingContent(context))
    {
        return;
    }
    
    var dev = new DrawEventArgs(Viewport, Rectangle.Empty, context);
    DrawingContent?.Invoke(this, dev);  // ? FIRE EVENT - WE ARE HERE!
    
    if (dev.Cancel)
    {
        return;
    }
    
    // No default drawing; let event handlers or overrides handle it
}
```

**Key Points:**
- Virtual `OnDrawingContent(context)` is called first (can cancel)
- `DrawingContent` event is fired with `DrawEventArgs`
- Event handler can set `e.Cancel = true` to prevent further processing
- No default implementation - this is where custom drawing happens

## Driver Clip State at DrawingContent Event

### What is Driver.Clip?

`Driver.Clip` is a `Region` object (not a simple `Rectangle`) that represents the area of the screen where drawing operations are allowed. It can be non-rectangular (e.g., when views overlap).

### Clip State When DrawingContent Fires

At the point where the `DrawingContent` event handler executes:

```
Driver.Clip = originalClip ? ViewportToScreen(Viewport)
```

Where:
- `originalClip` = The clip when `Draw()` started (typically `Screen` for top-level views)
- `Viewport` = The view's visible content area (view-relative coordinates)
- `ViewportToScreen(Viewport)` = Viewport converted to screen coordinates
- `?` = Intersection operation (only the overlapping area)

### For appWindow (in WideGlyphs):

```csharp
Window appWindow = new()
{
    Title = GetQuitKeyAndName(),
    BorderStyle = LineStyle.None  // No border
};
```

**Clip Calculation:**
- `originalClip` = `Screen` (e.g., `Rectangle(0, 0, 80, 25)`)
- `Viewport` = `Rectangle(0, 0, Width, Height)` - fills entire Frame (no border)
- `ViewportToScreen(Viewport)` = `Viewport` + `Frame.Location` offset
- `Driver.Clip` = `Screen` ? `ViewportToScreen(Viewport)`

**Result:**
- The clip is the intersection of the screen and the window's viewport
- Since `appWindow` typically fills the screen, the clip is effectively unchanged
- Any `AddRune()` calls are automatically clipped to this region

## Coordinate Systems

### Viewport-Relative Coordinates

Used by drawing methods like `AddRune()`, `AddStr()`, `FillRect()`:

```csharp
view.AddRune(c, r, codepoint);  // (c, r) is relative to Viewport
```

- `(0, 0)` = Top-left corner of the view's `Viewport`
- `(Viewport.Width-1, Viewport.Height-1)` = Bottom-right corner
- The driver automatically converts to screen coordinates

### Screen-Relative Coordinates

Used by the clip region and `Driver` methods:

```csharp
Rectangle screenRect = view.ViewportToScreen(new Rectangle(0, 0, 10, 10));
```

- `(0, 0)` = Top-left corner of the terminal
- Clip regions are always in screen coordinates
- Use `ViewportToScreen()` to convert from viewport to screen

## WideGlyphs Scenario Specifics

### Purpose

Demonstrates how wide (double-width) glyphs are clipped when overlapped by:
1. Vertical lines at even and odd column positions
2. Movable/resizable views at even and odd positions

### Key Code

```csharp
appWindow.DrawingContent += (s, e) =>
{
    View? view = s as View;
    if (view is null || _codepoints is null)
    {
        return;
    }
    
    // Draw random wide glyphs using viewport-relative coordinates
    for (int r = 0; r < view.Viewport.Height && r < _codepoints.GetLength(0); r++)
    {
        for (int c = 0; c < view.Viewport.Width && c < _codepoints.GetLength(1); c += 2)
        {
            Rune codepoint = _codepoints[r, c];
            if (codepoint != default(Rune))
            {
                view.AddRune(c, r, codepoint);  // Viewport-relative coords
            }
        }
    }
};
```

### Why c += 2?

Wide glyphs occupy two columns. We increment by 2 to avoid overlapping glyphs.

### What Gets Tested

1. **Clipping at even columns** - `Line` at `X = 10`
2. **Clipping at odd columns** - `Line` at `X = 25`
3. **View overlaps at even X** - `View` at `X = 30`
4. **View overlaps at odd X** - `View` at `X = 31`

This tests whether the clipping system correctly handles wide glyphs that might be partially occluded.

## Summary

The draw flow from `Application.Run` to `DrawingContent`:

1. **Application.Run** ? Enters main loop
2. **MainLoop.Iteration** ? Processes one iteration
3. **App.LayoutAndDraw** ? Initializes `Driver.Clip = Screen`
4. **View.Draw (static)** ? Iterates through views
5. **view.Draw (instance)** ? Sets `Clip = Viewport ? Clip`
6. **DoDrawContent** ? Fires `DrawingContent` event
7. **Your handler** ? Draws content using viewport-relative coords

**At DrawingContent:**
- `Driver.Clip` = Intersection of original clip and viewport
- Drawing uses viewport-relative coordinates `(0,0)` = top-left of viewport
- The driver automatically clips all operations to `Driver.Clip`
- The clip is automatically restored after your handler returns

## References

### Key Files

- `Terminal.Gui\App\ApplicationImpl.Run.cs` - Session management and run loop
- `Terminal.Gui\App\MainLoop\ApplicationMainLoop.cs` - Main loop iteration
- `Terminal.Gui\App\ApplicationImpl.Screen.cs` - LayoutAndDraw implementation
- `Terminal.Gui\ViewBase\View.Drawing.cs` - View drawing methods
- `Terminal.Gui\ViewBase\View.Layout.cs` - Layout system

### Key Classes

- `ApplicationImpl` - Main application implementation
- `MainLoopCoordinator` - Coordinates main loop and input thread
- `ApplicationMainLoop` - Main loop that processes iterations
- `View` - Base class for all UI elements
- `DrawContext` - Tracks regions drawn for transparency support
- `Region` - Represents a potentially non-rectangular screen area

### Key Concepts

- **Session** - A running instance of an `IRunnable`
- **SessionStack** - Stack of all running sessions
- **TopRunnable** - The runnable at the top of the stack (gets input)
- **Driver.Clip** - Region where drawing is allowed
- **Viewport** - The visible content area of a view
- **NeedsDraw** - Flag indicating a view needs redrawing
- **DrawContext** - Tracks drawn regions for transparency
