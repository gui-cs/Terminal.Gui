# Issue 4150: Finish Implementing `Line`

## Summary of Existing Implementation

This document provides a comprehensive analysis of the current state of line-drawing implementations in Terminal.Gui and outlines the requirements for completing the `Line` view.

### Overview

Terminal.Gui currently has **two separate implementations** for drawing lines:

1. **`LineView`** - A legacy, simpler implementation that directly renders lines using runes
2. **`Line`** - A newer implementation that integrates with the `LineCanvas` system

### Current Implementations

#### 1. LineView (`Terminal.Gui/Views/LineView.cs`)

**Status**: Complete and functional

**Description**: A straightforward View that draws horizontal or vertical lines using runes.

**Key Features**:
- Simple constructor taking `Orientation` (horizontal by default)
- Direct rendering using `Driver?.AddRune()`
- Supports customizable line runes via `LineRune` property
- Supports starting and ending anchors (`StartingAnchor`, `EndingAnchor`)
- Uses `Dim.Auto()` for automatic sizing based on orientation
- Draws directly in `OnDrawingContent()` without using LineCanvas

**Properties**:
```csharp
public Rune LineRune { get; set; }              // The symbol for drawing the line
public Rune? StartingAnchor { get; set; }       // Left/top end symbol
public Rune? EndingAnchor { get; set; }         // Right/bottom end symbol  
public Orientation Orientation { get; set; }    // Direction of the line
```

**Drawing Approach**:
- Iterates through viewport dimensions
- Renders runes directly using `Driver?.AddRune()`
- Applies starting/ending anchors at appropriate positions

**Example Usage**: See `Examples/UICatalog/Scenarios/LineViewExample.cs`

#### 2. Line (`Terminal.Gui/Views/Line.cs`)

**Status**: Partially implemented - Basic structure in place, needs refinement

**Description**: A more sophisticated View that integrates with the Terminal.Gui LineCanvas system for proper line intersection rendering.

**Key Features**:
- Implements `IOrientation` interface using `OrientationHelper`
- Sets `SuperViewRendersLineCanvas = true` to enable parent rendering
- Uses `BorderStyle` property to determine line style (Single, Double, Heavy, etc.)
- Adds lines to `LineCanvas` instead of drawing directly
- Supports proper line intersection rendering via LineCanvas

**Properties**:
```csharp
public Orientation Orientation { get; set; }    // Via IOrientation interface
```

**Drawing Approach**:
- In `OnDrawingContent()`, adds line to parent's `LineCanvas`
- Uses `ViewportToScreen()` to get screen coordinates
- Calculates length based on Frame dimensions and orientation
- LineCanvas handles the actual rendering and intersection logic

**Key Differences from LineView**:
- Uses `BorderStyle` (LineStyle enum) instead of individual Runes
- Integrates with LineCanvas for proper box-drawing character selection
- Does not support custom starting/ending anchors (uses proper line intersections instead)
- Height/Width fixed to 1 for the cross-section dimension (not `Dim.Auto()`)

#### 3. LineCanvas (`Terminal.Gui/Drawing/LineCanvas/LineCanvas.cs`)

**Status**: Complete and functional

**Description**: A sophisticated system for managing multiple lines and automatically determining the correct box-drawing characters at intersections.

**Key Features**:
- Manages collections of `StraightLine` objects
- Calculates proper intersection glyphs for lines of different styles
- Supports multiple line styles: Single, Double, Heavy, Rounded, Dashed, Dotted, etc.
- Handles line merging and exclusion regions
- Provides `GetCellMap()` to get rendered characters with attributes

**Core Functionality**:
- Analyzes all lines at each point to determine intersection type
- Selects appropriate box-drawing character based on:
  - Line styles of intersecting lines
  - Intersection type (corner, tee, cross, straight)
  - Orientation of lines

#### 4. Example Implementation (`Examples/UICatalog/Scenarios/LineViewExample.cs`)

**Status**: Complete - Demonstrates LineView only

**Description**: Shows various uses of `LineView` including:
- Regular horizontal line
- Double-width line (using Unicode)
- Short line with explicit width
- Arrow line with custom anchors
- Vertical lines

**Note**: Currently only demonstrates `LineView`, not the newer `Line` class.

---

## Analysis: Line vs LineView

### Why Two Implementations?

1. **LineView**: Simple, direct, self-contained
   - Good for basic line needs
   - No dependencies on LineCanvas
   - Manual control over appearance
   - Does not participate in line intersection calculations

2. **Line**: Sophisticated, integrated with LineCanvas
   - Proper box-drawing character selection
   - Automatic intersection handling
   - Participates in the View hierarchy's LineCanvas system
   - Consistent with Border rendering approach

### Current Issues with Line Implementation

1. **Incomplete API**:
   - No way to specify custom starting/ending glyphs like LineView
   - Limited documentation in XML comments
   - Empty `<remarks>` section

2. **Missing Example**:
   - No example demonstrating `Line` usage
   - Existing `LineViewExample.cs` doesn't show `Line` capabilities
   - Commented-out code in `LineCanvasExperiment.cs` suggests incomplete testing

3. **Feature Parity**:
   - LineView has anchor support, Line does not
   - Unclear if Line should support anchors (may conflict with intersection logic)

4. **Documentation**:
   - No clear guidance on when to use Line vs LineView
   - Missing architectural documentation

---

## Requirements for Completing Line Implementation

### 1. Core Functionality ✅ (Already Implemented)

- [x] Basic line drawing via LineCanvas
- [x] Horizontal and vertical orientation support
- [x] Integration with IOrientation interface
- [x] SuperViewRendersLineCanvas support
- [x] BorderStyle property support

### 2. Documentation Requirements

#### 2.1 Code Documentation
- [ ] Complete XML `<remarks>` section in Line.cs explaining:
  - Purpose and use cases
  - Differences from LineView
  - Integration with LineCanvas
  - When to use Line vs LineView
  
- [ ] Add XML examples showing basic usage

- [ ] Document the relationship between BorderStyle and line appearance

#### 2.2 Architectural Documentation
- [ ] Create or update developer documentation explaining:
  - The LineCanvas rendering system
  - How SuperViewRendersLineCanvas works
  - Line intersection logic
  - Performance considerations

### 3. Example Implementation

- [ ] Create `LineExample.cs` in `Examples/UICatalog/Scenarios/` demonstrating:
  - Basic horizontal and vertical lines
  - Different BorderStyle values (Single, Double, Heavy, Rounded, etc.)
  - Line intersections
  - Integration with Borders
  - Comparison with LineView
  
- [ ] Update or complete `LineCanvasExperiment.cs` with working Line examples

### 4. Testing

- [ ] Create `LineTests.cs` in `Tests/UnitTests/Views/` with tests for:
  - Constructor and default values
  - Orientation changes
  - BorderStyle changes
  - Layout behavior (Width/Height constraints)
  - LineCanvas integration
  - Rendering behavior
  
- [ ] Add integration tests showing Line interactions with:
  - Multiple Line instances
  - Border class
  - Other Views using LineCanvas

### 5. API Refinement (Optional - Consider for Future)

These are potential enhancements that may or may not be appropriate:

- [ ] **Decision needed**: Should Line support starting/ending anchors like LineView?
  - Pros: Feature parity, more flexibility
  - Cons: May interfere with automatic intersection detection
  
- [ ] Consider adding properties:
  - `LineStyle Style { get; set; }` - alias/convenience for BorderStyle?
  - `Attribute? LineAttribute { get; set; }` - custom color support?
  
- [ ] Consider constructor overloads:
  ```csharp
  public Line(Orientation orientation)
  public Line(Orientation orientation, LineStyle style)
  ```

### 6. Migration Guide

- [ ] Document migration path from LineView to Line
- [ ] Provide clear guidance on when to use each implementation
- [ ] Consider deprecation strategy for LineView (or keep both?)

---

## Recommended Completion Order

1. **Documentation** (Highest Priority)
   - Complete XML documentation in Line.cs
   - Add remarks explaining purpose and usage
   - Add code examples in XML comments

2. **Example Implementation** (High Priority)
   - Create comprehensive LineExample.cs
   - Show all BorderStyle options
   - Demonstrate intersection handling
   - Compare with LineView

3. **Testing** (High Priority)
   - Create LineTests.cs with comprehensive unit tests
   - Test all orientations and styles
   - Test LineCanvas integration

4. **API Refinement** (Medium Priority - After Discussion)
   - Decide on anchor support
   - Review property naming
   - Consider constructor overloads

5. **Architectural Documentation** (Medium Priority)
   - Document LineCanvas system
   - Explain rendering pipeline
   - Performance considerations

6. **Migration Guide** (Low Priority)
   - Can be done after API is stable
   - Helps users transition from LineView

---

## Questions for Discussion

1. **Feature Scope**: Should `Line` support starting/ending anchors like `LineView`, or is automatic intersection handling sufficient?

2. **Naming**: Is `BorderStyle` the right property name for controlling line appearance in `Line`, or should there be a `LineStyle` property?

3. **Deprecation**: Should `LineView` eventually be deprecated in favor of `Line`, or should both coexist for different use cases?

4. **Performance**: Are there any performance concerns with LineCanvas vs direct rendering for simple use cases?

5. **Default Style**: Should `Line` have a default `BorderStyle` of `Single` instead of `None`?

---

## Related Code Files

- `Terminal.Gui/Views/Line.cs` - The main Line view implementation
- `Terminal.Gui/Views/LineView.cs` - The legacy LineView implementation
- `Terminal.Gui/Drawing/LineCanvas/LineCanvas.cs` - Line rendering and intersection system
- `Terminal.Gui/Drawing/LineCanvas/StraightLine.cs` - Individual line representation
- `Terminal.Gui/ViewBase/Adornment/Border.cs` - Uses LineCanvas for border rendering
- `Terminal.Gui/ViewBase/View.Drawing.cs` - LineCanvas rendering in View lifecycle
- `Examples/UICatalog/Scenarios/LineViewExample.cs` - LineView examples
- `Examples/UICatalog/Scenarios/LineCanvasExperiment.cs` - Experimental Line usage
- `Tests/UnitTests/Views/LineViewTests.cs` - LineView tests

---

## Conclusion

The `Line` class has a solid foundation but requires:
1. **Complete documentation** to explain its purpose and usage
2. **Working examples** to demonstrate capabilities
3. **Comprehensive tests** to ensure reliability
4. **Clear guidance** on when to use Line vs LineView

The core functionality is implemented and working. The main gap is in documentation, examples, and testing to make it production-ready and user-friendly.
