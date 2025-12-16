# Dim.Auto Deep Dive

## See Also

* [Lexicon & Taxonomy](lexicon.md)
* [Layout](layout.md)

## Overview

The `Dim.Auto` type is a specialized `Dim` class in Terminal.Gui v2 that enables automatic sizing of a `View` based on its content. This is particularly useful for dynamically sizing views to accommodate varying content such as text, subviews, or explicitly set content areas. Unlike other `Dim` types like `Dim.Absolute` or `Dim.Fill`, `Dim.Auto` calculates dimensions at runtime based on specified criteria, making it ideal for responsive UI design in terminal applications.

Like all `Dim` types, `Dim.Auto` is used to set the `Width` or `Height` of a view and can be combined with other `Dim` types using addition or subtraction (see `DimCombine`).

The `DimAutoStyle` enum defines the different strategies that `Dim.Auto` can employ to size a view. The `DimAutoStyle` enum has the following values:

- **Text**: The view is sized based on the `Text` property and `TextFormatter` settings. This considers the formatted text dimensions, constrained by any specified maximum dimensions.
- **Content**: The view is sized based on either the value returned by `View.GetContentSize()` or the `Subviews` property. If the content size is explicitly set (via `View.SetContentSize()`), the view is sized based on that value. Otherwise, it considers the subview with the largest relevant dimension plus its position.
- **Auto**: The view is sized based on both the text and content, whichever results in the larger dimension.

## Using Dim.Auto

`Dim.Auto` is defined as:

```cs
public static Dim Auto (DimAutoStyle style = DimAutoStyle.Auto, Dim minimumContentDim = null, Dim maximumContentDim = null)
```

To use `Dim.Auto`, set the `Width` or `Height` property of a view to `Dim.Auto(DimAutoStyle.Text)`, `Dim.Auto(DimAutoStyle.Content)`, or `Dim.Auto(DimAutoStyle.Auto)`.

For example, to create a `View` that is sized based on the `Text` property, you can do this:

```cs
View view = new ()
{
    Text = "Hello, World!",
    Width = Dim.Auto(DimAutoStyle.Text),
    Height = Dim.Auto(DimAutoStyle.Text),
};
```

Note, the built-in `Label` view class does precisely this in its constructor.

To create a `View` that is sized based on its `Subviews`, you can do this:

```cs
View view = new ()
{
    Width = Dim.Auto(DimAutoStyle.Content),
    Height = Dim.Auto(DimAutoStyle.Content),
};
view.Add(new Label() { Text = "Hello, World!" });
```

In this example, the `View` will be sized based on the size of the `Label` that is added to it.

### Specifying a Minimum Size

You can specify a minimum size by passing a `Dim` object to the `minimumContentDim` parameter. For example, to create a `View` that is sized based on the `Text` property, but has a minimum width of 10 columns, you can do this:

```cs
View view = new ()
{
    Text = "Hello, World!",
    Width = Dim.Auto(DimAutoStyle.Text, minimumContentDim: Dim.Absolute(10)), // Same as `minimumContentDim: 10`
    Height = Dim.Auto(DimAutoStyle.Text),
};
```

Sometimes it's useful to have the minimum size be dynamic. Use `Dim.Func` as follows:

```cs
View view = new ()
{
    Width = Dim.Auto(DimAutoStyle.Content, minimumContentDim: Dim.Func(GetDynamicMinSize)),
    Height = Dim.Auto(DimAutoStyle.Text),
};

int GetDynamicMinSize()
{
    return someDynamicInt;
}
```

### Specifying a Maximum Size

It is common to want to constrain how large a View can be sized. The `maximumContentDim` parameter to the `Dim.Auto()` method enables this. Like `minimumContentDim`, it is of type `Dim` and thus can represent a dynamic value. For example, by default, `Dialog` specifies `maximumContentDim` as `Dim.Percent(90)` to ensure a dialog box is never larger than 90% of the screen.

```cs
View dialog = new ()
{
    Width = Dim.Auto(DimAutoStyle.Content, maximumContentDim: Dim.Percent(90)),
    Height = Dim.Auto(DimAutoStyle.Content, maximumContentDim: Dim.Percent(90)),
};
```

## Technical Details

### Calculation Logic

The `Dim.Auto` class calculates dimensions dynamically during the layout process. Here's how it works under the hood, based on the codebase analysis:

- **Text-Based Sizing (`DimAutoStyle.Text`)**: When using `Text` style, the dimension is determined by the formatted text size as computed by `TextFormatter`. For width, it uses `ConstrainToWidth`, and for height, it uses `ConstrainToHeight`. These values are set based on the formatted text size, constrained by any maximum dimensions provided.
- **Content-Based Sizing (`DimAutoStyle.Content`)**: For `Content` style, if `ContentSizeTracksViewport` is `false` and there are no subviews, it uses the explicitly set content size from `GetContentSize()`. Otherwise, it iterates through subviews to calculate the maximum dimension needed based on their positions and sizes.
- **Auto Sizing (`DimAutoStyle.Auto`)**: This combines both `Text` and `Content` strategies, taking the larger of the two calculated dimensions.

The calculation in `DimAuto.Calculate` method also respects `minimumContentDim` and `maximumContentDim`:
- The final size is at least the minimum specified (if any), and at most the maximum specified (if any).
- Adornments (like margins, borders, and padding) are added to the calculated content size to ensure the view's frame includes these visual elements.

### Handling Subviews

When sizing based on subviews, `Dim.Auto` employs a sophisticated approach to handle dependencies:
- It categorizes subviews based on their `Pos` and `Dim` types to manage layout dependencies. For instance, it processes subviews with absolute positions and dimensions first, then handles more complex cases like `PosAnchorEnd` or `DimView`.
- This ensures that views dependent on other views' sizes or positions are calculated correctly, avoiding circular dependencies and ensuring accurate sizing.

### Adornments Consideration

The size calculation includes the thickness of adornments (margin, border, padding) to ensure the view's total frame size accounts for these elements. This is evident in the code where `adornmentThickness` is added to the computed content size.

## Limitations

`Dim.Auto` is not always the best choice for sizing a view. Consider the following limitations:

- **Performance Overhead**: `Dim.Auto` can introduce performance overhead due to the dynamic calculation of sizes, especially with many subviews or complex text formatting. If the size is known and static, `Dim.Absolute(n)` might be more efficient.
- **Not Suitable for Full-Screen Layouts**: If you want a view to fill the entire width or height of the superview, `Dim.Fill()` is more appropriate than `Dim.Auto(DimAutoStyle.Content)` as it directly uses the superview's dimensions without content-based calculations.
- **Dependency Complexity**: When subviews themselves use `Dim.Auto` or other dependent `Dim` types, the layout process can become complex and may require multiple iterations to stabilize, potentially leading to unexpected results if not carefully managed.

## Behavior of Other Pos/Dim Types When Used Within a Dim.Auto-Sized View

The table below describes the behavior of various `Pos` and `Dim` types when used by subviews of a view that uses `Dim.Auto` for its `Width` or `Height`. This reflects how these types influence the automatic sizing:

| Type          | Impacts Dimension | Notes                                                                                             |
|---------------|-------------------|---------------------------------------------------------------------------------------------------|
| **PosAlign**  | Yes               | The subviews with the same `GroupId` will be aligned at the maximum dimension to enable them to not be clipped. This dimension plus the group's position will determine the minimum `Dim.Auto` dimension. |
| **PosView**   | Yes               | The position plus the dimension of `subview.Target` will determine the minimum `Dim.Auto` dimension. |
| **PosCombine**| Yes               | Impacts dimension if it includes a `Pos` type that affects dimension (like `PosView` or `PosAnchorEnd`). |
| **PosAnchorEnd**| Yes            | The `Dim.Auto` dimension will be increased by the dimension of the subview to accommodate its anchored position. |
| **PosCenter** | No                | Does not impact the dimension as it centers based on superview size, not content.                |
| **PosPercent**| No                | Does not impact dimension unless combined with other impacting types; based on superview size.   |
| **PosAbsolute**| Yes              | Impacts dimension if the absolute position plus subview dimension exceeds current content size.  |
| **PosFunc**   | Yes               | Impacts dimension if the function returns a value that, combined with subview dimension, exceeds content size. |
| **DimView**   | Yes               | The dimension of `subview.Target` will contribute to the minimum `Dim.Auto` dimension.          |
| **DimCombine**| Yes               | Impacts dimension if it includes a `Dim` type that affects dimension (like `DimView` or `DimAuto`). |
| **DimFill**   | No                | Does not impact dimension as it fills remaining space, not contributing to content-based sizing. |
| **DimPercent**| No                | Does not impact dimension as it is based on superview size, not content.                       |
| **DimAuto**   | Yes               | Contributes to dimension based on its own content or text sizing, potentially increasing the superview's size. |
| **DimAbsolute**| Yes              | Impacts dimension if the absolute size plus position exceeds current content size.              |
| **DimFunc**   | Yes               | Impacts dimension if the function returns a size that, combined with position, exceeds content size. |

## Building Dim.Auto Friendly Views

It is common to build view classes that have a natural size based on their content. For example, the `Label` class is sized based on the `Text` property.

`Slider` is a good example of a sophisticated `Dim.Auto`-friendly view. Developers using these views shouldn't need to know the details of how the view is sized; they should just be able to use the view and have it size itself correctly.

For example, a vertical `Slider` with 3 options may be created like this, sized based on the number of options, its orientation, etc.:

```cs
List<object> options = new() { "Option 1", "Option 2", "Option 3" };
Slider slider = new(options)
{
    Orientation = Orientation.Vertical,
    Type = SliderType.Multiple,
};
view.Add(slider);
```

Note the developer does not need to specify the size of the `Slider`; it will size itself based on the number of options and the orientation.

Views like `Slider` achieve this by setting `Width` and `Height` to `Dim.Auto(DimAutoStyle.Content)` in the constructor and calling `SetContentSize()` whenever the desired content size changes. The view will then be sized to be big enough to fit the content.

Views that use `Text` for their content can set `Width` and `Height` to `Dim.Auto(DimAutoStyle.Text)`. It is recommended to use `Height = Dim.Auto(DimAutoStyle.Text, minimumContentDim: 1)` to ensure the view can show at least one line of text.

### Best Practices for Custom Views

- **Set Appropriate DimAutoStyle**: Choose `Text`, `Content`, or `Auto` based on what drives the view's size. Use `Text` for text-driven views like labels, `Content` for container-like views with subviews or explicit content sizes, and `Auto` for mixed content.
- **Update Content Size Dynamically**: If your view's content changes (e.g., text updates or subviews are added/removed), call `SetContentSize()` or ensure properties like `Text` are updated to trigger re-layout.
- **Consider Minimum and Maximum Constraints**: Use `minimumContentDim` to prevent views from becoming too small to be usable, and `maximumContentDim` to prevent them from growing excessively large, especially in constrained terminal environments.
- **Handle Adornments**: Be aware that `Dim.Auto` accounts for adornments in its sizing. If your view has custom adornments, ensure they are properly factored into the layout by the base `View` class.

## Debugging Dim.Auto Issues

If you encounter unexpected sizing with `Dim.Auto`, consider the following debugging steps based on the codebase's diagnostic capabilities:

- **Enable Validation**: Set `ValidatePosDim` to `true` on the view to enable runtime validation of `Pos` and `Dim` settings. This will throw exceptions if invalid configurations are detected, helping identify issues like circular dependencies or negative sizes.
- **Check Content Size**: Verify if `ContentSizeTracksViewport` is behaving as expected. If set to `false`, ensure `SetContentSize()` is called with the correct dimensions. Use logging to track `GetContentSize()` outputs.
- **Review Subview Dependencies**: Look for subviews with `Pos` or `Dim` types that impact dimension (like `PosAnchorEnd` or `DimView`). Ensure their target views are laid out before the current view to avoid incorrect sizing.
- **Inspect Text Formatting**: For `Text` style, check `TextFormatter` settings and constraints (`ConstrainToWidth`, `ConstrainToHeight`). Ensure text is formatted correctly before sizing calculations.

By understanding the intricacies of `Dim.Auto` as implemented in Terminal.Gui v2, developers can create responsive and adaptive terminal UIs that automatically adjust to content changes, enhancing user experience and maintainability.
