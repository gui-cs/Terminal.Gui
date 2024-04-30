# Dim.Auto Deep Dive

The `Dim.Auto` type is a type of `Dim` that automatically sizes the view based on its content. This is useful when you want to size a view based on the content it contains. That content can either be the `Text`, the `SubViews`, or something else defined by the view.

Like all `Dim` types, `Dim.Auto` is used to set the `Width` or `Height` of a view. 

The `DimAutoStyle` enum defines the different ways that `Dim.Auto` can be used to size a view. The `DimAutoStyle` enum has the following values:

* `Text` - The view is sized based on the `Text` property and `TextFormatter` settings.
* `Content` - The view is sized based on either the `ContentSize` or the `Subviews` property. If `ContentSize` is null, the view is sized based on the size of the subviews (the Subview with the largest relvant dimension plus location will dictate the size). If `ContentSize` is not null, the view is sized based on the `ContentSize` property.
* `Auto` -  The view is sized based on both `Text` and `Content`, whichever is larger.

## Using Dim.Auto

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

## Limitations

`Dim.Auto` is not always the best choice for sizing a view. For example, if you want a view to fill the entire width of the Superview, you should use `Dim.Fill ()` instead of `Dim.Auto (DimAutoStyle.Content)`.

`Dim.Auto` is also not always the most efficient way to size a view. If you know the size of the content ahead of time, you can set the `Width` and `Height` properties to `Dim.Absolute (n)` instead of using `Dim.Auto`.

> TODO: Verify accuracy of the following paragraphs

Some `Pos` and `Dim` types are not compatible with `Dim.Auto`. For example, you cannot use `Dim.Auto (DimAutoStyle.Content)` with Subviews that use `Dim.Fill ()` or `Dim.Percent (n)` because the size of the Subview is not known until the layout is computed.

`Pos` types that are relative to the size of the view, such as `Pos.Percent (n)` are not compatible with `Dim.Auto` because the size of the view is not known until the layout is computed. However, `Pos.Center ()` and `Pos.AnchorEnd ()` are compatible with `Dim.Auto` because they are relative to the size of the view's Superview.


## Building Dim.Auto friendly View

It is common to build View classes that have a natrual size based on their content. For example, the `Label` class is a view that is sized based on the `Text` property. Another example is `Slider` which is size based on the number of options it has, it's orientation, etc... 

Developers using these views shouldn't need to know the details of how the view is sized, they should just be able to use the view and have it size itself correctly.

For example, a vertical `Slider` with 3 options may be created like this:

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

Views like `Slider` do this by setting `Width` and `Height` to `Dim.Auto (DimAutoStyle.Content)` in the constructor and and setting the `ContentSize` property to the size that the view should be in the `LayoutStarted` event handler.

Views that use `Text` for their content can just set `Width` and `Height` to `Dim.Auto (DimAutoStyle.Text)`.

