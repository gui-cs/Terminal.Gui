# Dim.Auto Deep Dive

## See Also

* [Lexicon & Taxonomy](lexicon.md)
* [Layout](layout.md)

## Overview

The `Dim.Auto` type is a type of `Dim` that automatically sizes the view based on its content. This is useful when you want to size a view based on the content it contains. That content can either be the `Text`, the `SubViews`, or something else defined by the view.

Like all `Dim` types, `Dim.Auto` is used to set the `Width` or `Height` of a view and can be combined with other `Dim` types using addition or subtraction (see. `DimCombine`).

The `DimAutoStyle` enum defines the different ways that `Dim.Auto` can be used to size a view. The `DimAutoStyle` enum has the following values:

* `Text` - The view is sized based on the `Text` property and `TextFormatter` settings.
* `Content` - The view is sized based on either the value returned by `View.SetContentSize()` or the `Subviews` property. If the content size is not explicitly set (via `View.SetContentSize()`), the view is sized based on the Subview with the largest relvant dimension plus location. If the content size is explicitly set, the view is sized based on the value returned by `View.SetContentSize()`.
* `Auto` -  The view is sized based on both the text and content, whichever is larger.

## Using Dim.Auto

`Dim.Auto` is defined as:

```cs
public static Dim Auto (DimAutoStyle style = DimAutoStyle.Auto, Dim minimumContentDim = null, Dim max = null)
```

To use `Dim.Auto`, set the `Width` or `Height` property of a view to `Dim.Auto (DimAutoStyle.Text)` or `Dim.Auto (DimAutoStyle.Content)`.


For example, to create a `View` that is sized based on the `Text` property, you can do this:

```cs
View view = new ()
{
    Text = "Hello, World!",
    Width = Dim.Auto (DimAutoStyle.Text),
    Height = Dim.Auto (DimAutoStyle.Text),
};
```

Note, the built-in `Label` view class does precisely this in its constructor.

To create a `View` that is sized based on its `Subviews`, you can do this:

```cs
View view = new ()
{
    Width = Dim.Auto (DimAutoStyle.Content),
    Height = Dim.Auto (DimAutoStyle.Content),
};
view.Add (new Label () { Text = "Hello, World!" });
```

In this example, the `View` will be sized based on the size of the `Label` that is added to it.

### Specifying a miniumum size

You can specify a minimum size by passing a `Dim` object to the `minimumContentDim` parameter. For example, to create a `View` that is sized based on the `Text` property, but has a minimum width of 10 columns, you can do this:

```cs
View view = new ()
{
    Text = "Hello, World!",
    Width = Dim.Auto (DimAutoStyle.Text, minimumContentDim: Dim.Absolute (10)), // Same as `minimumContentDim: 10`
    Height = Dim.Auto (DimAutoStyle.Text),
};
```

Sometimes it's useful to have the minimum size be dynamic. Use `Dim.Func` as follows:

```cs
View view = new ()
{
    Width = Dim.Auto (DimAutoStyle.Content, minimumContentDim: Dim.Func (GetDynamicMinSize)),
    Height = Dim.Auto (DimAutoStyle.Text),
};

int GetDynamicMinSize () 
{
    return someDynamicInt;
}
```

### Specifying a maximum size

It is common to want to constrain how large a View can be sized. The `maximumContentDim` parameter to the `Dim.Auto ()` method enables this. Like `minimumContentDim` it is of type `Dim` and thus can represent a dynamic value. For example, by default `Dialog` specifies `maximumContentDim` as `Dim.Percent (90)` to ensure a Dialog box is never larger than 90% of the screen.

## Limitations

`Dim.Auto` is not always the best choice for sizing a view. For example, if you want a view to fill the entire width of the Superview, you should use `Dim.Fill ()` instead of `Dim.Auto (DimAutoStyle.Content)`.

`Dim.Auto` is also not always the most efficient way to size a view. If you know the size of the content ahead of time, you can set the `Width` and `Height` properties to `Dim.Absolute (n)` instead of using `Dim.Auto`.

## Behavior of other Pos/Dim Types when used within a Dim.Auto-sized View

The table below descibes the behavior of the various Pos/Dim types when used by subviews of a View that uses `Dim.Auto` for it's `Width` or `Height`:

| Type        | Impacts Dimension | Notes                                                                                             |
|-------------|-------------------|---------------------------------------------------------------------------------------------------------|
| PosAlign    | Yes               | The subviews with the same `GroupId` will be aligned at the maximimum dimension to enable them to not be clipped. This dimension plus the group's position will determine the minimum `Dim.Auto` dimension. |
| PosView     | Yes               | The position plus the dimension of `subview.Target` will determine the minimum `Dim.Auto` dimension. |
| PosCombine  | Yes               | <needs clarification> |
| PosAnchorEnd| Yes               | The `Dim.Auto` dimension will be increased by the dimension of the subview. |
| PosCenter   | No                |  |
| PosPercent  | No                |  |
| PosAbsolute | Yes               |  |
| PosFunc     | Yes               |  |
| DimView     | Yes               | The position plus the dimension of `subview.Target` will determine the minimum `Dim.Auto` dimension. |
| DimCombine  | Yes               | <needs clarification>  |
| DimFill     | No                |  |
| DimPercent  | No                |  |
| DimAuto     | Yes               |  |
| DimAbsolute | Yes               |  |
| DimFunc     | Yes               | <needs clarification> |


## Building Dim.Auto friendly View

It is common to build View classes that have a natrual size based on their content. For example, the `Label` class is a view that is sized based on the `Text` property. 

`Slider` is a good example of sophsticated Dim.Auto friendly view.

Developers using these views shouldn't need to know the details of how the view is sized, they should just be able to use the view and have it size itself correctly.

For example, a vertical `Slider` with 3 options may be created like this: which is size based on the number of options it has, it's orientation, etc... 

```cs
List<object> options = new () { "Option 1", "Option 2", "Option 3" };
Slider slider = new (options)
{
    Orientation = Orientation.Vertical,
    Type = SliderType.Multiple,
};
view.Add (slider);
```

Note the developer does not need to specify the size of the `Slider`, it will size itself based on the number of options and the orientation. 

Views like `Slider` do this by setting `Width` and `Height` to `Dim.Auto (DimAutoStyle.Content)` in the constructor and calling `SetContentSize()` whenever the desired content size changes. The View will then be sized to be big enough to fit the content.

Views that use `Text` for their content can just set `Width` and `Height` to `Dim.Auto (DimAutoStyle.Text)`. It is recommended to use `Height = Dim.Auto (DimAutoStyle.Text, minimumContentDim: 1)` to ensure the View can show at least one line of text.
