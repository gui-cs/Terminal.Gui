// 
// Flex layout by Laurent Sansonetti
// Port to C# by Stephane Delcroix
//
// Last check against changes 6d5d4a33e115ba930c8e3959a8fdf3db0c0666ac

using System;
using System.Collections;
using System.Collections.Generic;

namespace Terminal.Gui {
	/// <summary>
	/// Values for <see cref="P:Xamarin.Flex.Item.AlignContent" />.
	/// </summary>
	public enum AlignContent {
		/// <summary>
		/// Whether an item's should be stretched out.
		/// </summary>
		Stretch = 1,
		/// <summary>
		/// Whether an item should be packed around the center.
		/// </summary>
		Center = 2,
		/// <summary>
		/// Whether an item should be packed at the start.
		/// </summary>
		Start = 3,
		/// <summary>
		/// Whether an item should be packed at the end.
		/// </summary>
		End = 4,
		/// <summary>
		/// Whether items should be distributed evenly, the first item being at the start and the last item being at the end.
		/// </summary>
		SpaceBetween = 5,
		/// <summary>
		/// Whether items should be distributed evenly, the first and last items having a half-size space.
		/// </summary>
		SpaceAround = 6,
		/// <summary>
		/// Whether items should be distributed evenly, all items having equal space around them.
		/// </summary>
		SpaceEvenly = 7,
	}

	/// <summary>
	/// Values for <see cref="P:Xamarin.Flex.Item.AlignItems" />.
	/// </summary>
	public enum AlignItems {
		/// <summary>
		/// Whether an item's should be stretched out.
		/// </summary>
		Stretch = 1,
		/// <summary>
		///  Whether an item should be packed around the center.
		/// </summary>
		Center = 2,
		/// <summary>
		/// Whether an item should be packed at the start.
		/// </summary>
		Start = 3,
		/// <summary>
		/// Whether an item should be packed at the end.
		/// </summary>
		End = 4,
		//Baseline = 8,
	}

	/// <summary>
	/// Values for <see cref="P:Xamarin.Flex.Item.AlignSelf" />.
	/// </summary>
	public enum AlignSelf {
		/// <summary>
		///  Whether an item should be packed according to the alignment value of its parent.
		/// </summary>
		Auto = 0,
		/// <summary>
		/// Whether an item's should be stretched out.
		/// </summary>
		Stretch = 1,
		/// <summary>
		/// Whether an item should be packed around the center.
		/// </summary>
		Center = 2,
		/// <summary>
		/// Whether an item should be packed at the start.
		/// </summary>
		Start = 3,
		/// <summary>
		/// Whether an item should be packed at the end.
		/// </summary>
		End = 4,
		//Baseline = 8,
	}

	/// <summary>
	/// Values for <see cref="P:Xamarin.Flex.Item.Direction" />.
	/// </summary>
	public enum Direction {
		/// <summary>
		/// Whether items should be stacked horizontally.
		/// </summary>
		Row = 0,
		/// <summary>
		/// Like Row but in reverse order.
		/// </summary>
		RowReverse = 1,
		/// <summary>
		/// Whether items should be stacked vertically.
		/// </summary>
		Column = 2,
		/// <summary>
		/// Like Column but in reverse order.
		/// </summary>
		ColumnReverse = 3,
	}

	/// <summary>
	/// Values for <see cref="P:Xamarin.Flex.Item.Justify" />.
	/// </summary>
	public enum Justify {
		/// <summary>
		/// Items are centered along th eline.
		/// </summary>
		Center = 2,
		/// <summary>
		/// Items are packed towards the start of the line.
		/// </summary>
		Start = 3,
		/// <summary>
		/// Items are packed towards the end of the line.
		/// </summary>
		End = 4,
		/// <summary>
		/// Whether items should be distributed evenly, the first item being at the start and the last item being at the end.
		/// </summary>
		SpaceBetween = 5,
		/// <summary>
		/// Whether items should be distributed evenly, the first and last items having a half-size space.
		/// </summary>
		SpaceAround = 6,
		/// <summary>
		/// Whether items should be distributed evenly, all items having equal space around them.
		/// </summary>
		SpaceEvenly = 7,
	}

	/// <summary>
	/// Values for <see cref="P:Xamarin.Flex.Item.Position" />.
	/// </summary>
	public enum Position {
		/// <summary>
		/// Whether the item's frame will be determined by the flex rules of the layout system.
		/// </summary>
		Relative = 0,
		/// <summary>
		/// Whether the item's frame will be determined by fixed position values (<see cref="P:Xamarin.Flex.Item.Left" />, <see cref="P:Xamarin.Flex.Item.Right" />, <see cref="P:Xamarin.Flex.Item.Top" /> and <see cref="P:Xamarin.Flex.Item.Bottom" />).
		/// </summary>
		Absolute = 1,
	}

	/// <summary>
	/// Values for <see cref="P:Xamarin.Flex.Item.Wrap" />.
	/// </summary>
	public enum Wrap {
		/// <summary>
		/// Whether items are laid out in a single line.
		/// </summary>
		NoWrap = 0,
		/// <summary>
		/// Whether items are laid out in multiple lines if needed.
		/// </summary>
		Wrap = 1,
		/// <summary>
		/// Like Wrap but in reverse order.
		/// </summary>
		WrapReverse = 2,
	}

	/// <summary>
	/// Value for <see cref="P:Xamarin.Flex.Item.Wrap" />.
	/// </summary>
	public struct Basis {
		readonly bool _isRelative;
		readonly bool _isLength;
		readonly int _length;
		/// <summary>
		/// Auto basis.
		/// </summary>
		public static Basis Auto = new Basis ();
		/// <summary>
		/// Whether the basis length is relative to parent's size.
		/// </summary>
		/// <value><c>true</c> if is relative; otherwise, <c>false</c>.</value>
		public bool IsRelative => _isRelative;
		/// <summary>
		/// Whether the basis is auto.
		/// </summary>
		/// <value><c>true</c> if is auto; otherwise, <c>false</c>.</value>
		public bool IsAuto => !_isLength && !_isRelative;
		/// <summary>
		/// Gets the length.
		/// </summary>
		/// <value>The length.</value>
		public int Length => _length;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Terminal.Gui.Basis"/> struct.
		/// </summary>
		/// <param name="length">Length.</param>
		/// <param name="isRelative">If set to <c>true</c> is relative.</param>
		public Basis (int length, bool isRelative = false)
		{
			_length = length;
			_isLength = !isRelative;
			_isRelative = isRelative;
		}
	}

	public partial class View : IEnumerable<View> {
		bool ShouldOrderChildren { get; set; }

		/// <summary>
		/// This property defines how the layout engine will distribute space between and 
		/// around child items that have been laid out on multiple lines. This property is 
		/// ignored if the root item does not have its<see cref="P:Xamarin.Flex.Item.Wrap" /> 
		/// property set to Wrap or WrapReverse.
		/// </summary>
		/// <remarks>The default value for this property is Stretch.</remarks>
		/// <value>The content of the align.</value>
		public AlignContent AlignContent { get; set; } = AlignContent.Stretch;
		/// <summary>
		/// This property defines how the layout engine will distribute space between 
		/// and around child items along the cross-axis.
		/// </summary>
		/// <value>The align items.</value>
		/// <remarks>The default value for this property is Stretch.</remarks>
		public AlignItems AlignItems { get; set; } = AlignItems.Stretch;
		/// <summary>
		/// his property defines how the layout engine will distribute space between and 
		/// around child items for a specific child along the cross-axis. If this property 
		/// is set to Auto on a child item, the parent's value for 
		/// <see cref="P:Xamarin.Flex.Item.AlignItems" /> will be used instead.
		/// </summary>
		/// <value>The align self.</value>
		public AlignSelf AlignSelf { get; set; } = AlignSelf.Auto;
		/// <summary>
		/// This property defines the initial main-axis dimension of the item. 
		/// If <see cref="P:Xamarin.Flex.Item.Direction" /> is row-based (horizontal), 
		/// it will be used instead of<see cref= "P:Xamarin.Flex.Item.Width" />, and if it's 
		/// column-based (vertical), it will be used instead of 
		/// <see cref="P:Xamarin.Flex.Item.Height" />.
		/// </summary>
		/// <value>The basis.</value>
		public Basis Basis { get; set; } = Basis.Auto;
		/// <summary>
		/// This property defines the bottom edge absolute position of the item. It also 
		/// defines the item's height if <see cref="P:Xamarin.Flex.Item.Top" /> is also set 
		/// and if <see cref = "P:Xamarin.Flex.Item.Height" /> isn't set. 
		/// It is ignored if <see cref="P:Xamarin.Flex.Item.Position" /> isn't set to Absolute.
		/// </summary>
		/// <value>The bottom.</value>
		/// <remarks>The default value is UnsetValue.</remarks>
		public int Bottom { get; set; } = UnsetValue;
		/// <summary>
		/// This property defines the direction and main-axis of child items. If set to 
		/// Column (or ColumnReverse), the main-axis will be the y-axis and items will be 
		/// stacked vertically.If set to Row (or RowReverse), the main-axis will be the x-axis 
		/// and items will be stacked horizontally.
		/// </summary>
		/// <value>The direction.</value>
		/// <remarks>The default value for this property is Column.</remarks>
		public Direction Direction { get; set; } = Direction.Column;
		/// <summary>
		/// This property defines the grow factor of the item; the amount of available 
		/// space it should use on the main-axis. If this property is set to 0, 
		/// the item will not grow.
		/// </summary>
		/// <value>The item grow factor.</value>
		/// <remarks>The default value for this property is 0 (does not take any available space).</remarks>
		public int Grow { get; set; } = 0;
		/// <summary>
		/// This property defines the height size dimension of the item.
		/// </summary>
		/// <value>The height size dimension.</value>
		/// <remarks>The default value is UnsetValue.</remarks>
		public int Height { get; set; } = UnsetValue;
		/// <summary>
		/// This property defines how the layout engine will distribute space 
		/// between and around child items along the main-axis.
		/// </summary>
		/// <value>Any value part of the<see cref="T:Xamarin.Flex.Align" /> 
		/// enumeration, with the exception of Stretch and Auto.</value>
		/// <remarks>
		/// The default value for this property is Start.
		/// </remarks>
		public Justify JustifyContent { get; set; } = Justify.Start;
		/// <summary>
		/// This property defines the left edge absolute position of the item. 
		/// It also defines the item's width if <see cref="P:Xamarin.Flex.Item.Right" /> 
		/// is also set and if <see cref = "P:Xamarin.Flex.Item.Width" /> isn't set. 
		/// It is ignored if <see cref = "P:Xamarin.Flex.Item.Position" /> isn't set 
		/// to Absolute.
		/// </summary>
		/// <value>The value for the property.</value>
		/// <remarks>The default value is UnsetValue.</remarks>
		public int Left { get; set; } = UnsetValue;
		/// <summary>
		/// This property defines the margin space required on the bottom edge of the item.
		/// </summary>
		/// <value>The top edge margin space (negative values are allowed).</value>
		/// <remarks>The default value for this property is 0.</remarks>
		public int MarginBottom { get; set; } = 0;
		/// <summary>
		/// This property defines the margin space required on the left edge of the item.
		/// </summary>
		/// <value>The top edge margin space (negative values are allowed).</value>
		/// <remarks>The default value for this property is 0.</remarks>
		public int MarginLeft { get; set; } = 0;
		/// <summary>
		/// This property defines the margin space required on the right edge of the item.
		/// </summary>
		/// <value>The top edge margin space (negative values are allowed).</value>
		/// <remarks>The default value for this property is 0.</remarks>
		public int MarginRight { get; set; } = 0;

		/// <summary>
		/// This value is used to reprensent that a dimension has not been set
		/// </summary>
		public const int UnsetValue = Int32.MaxValue;

		static bool IsUnset (int value)
		{
			return value == UnsetValue;
		}

		/// <summary>
		/// This property defines the margin space required on the top edge of the item.
		/// </summary>
		/// <value>The top edge margin space (negative values are allowed).</value>
		/// <remarks>The default value for this property is 0.</remarks>
		public int MarginTop { get; set; } = 0;

		int order;
		/// <summary>
		/// This property specifies whether this item should be laid out before or 
		/// after other items in the container.Items are laid out based on the 
		/// ascending value of this property.Items that have the same value 
		/// for this property will be laid out in the order they were inserted.
		/// </summary>
		/// <value>The item order (can be a negative, 0, or positive value).</value>
		/// <remarks>The default value for this property is 0.</remarks>
		public int Order {
			get => order;
			set {
				if ((order = value) != 0 && SuperView != null)
					SuperView.ShouldOrderChildren = true;
			}
		}

		/// <summary>
		/// This property defines the height of the item's bottom edge padding 
		/// space that should be used when laying out child items.
		/// </summary>
		/// <value>The bottom edge padding space.Negative values are not allowed.</value>
		public int PaddingBottom { get; set; } = 0;
		/// <summary>
		/// This property defines the height of the item's left edge padding 
		/// space that should be used when laying out child items.
		/// </summary>
		/// <value>The bottom edge padding space.Negative values are not allowed.</value>
		public int PaddingLeft { get; set; } = 0;
		/// <summary>
		/// This property defines the height of the item's right edge padding 
		/// space that should be used when laying out child items.
		/// </summary>
		/// <value>The bottom edge padding space.Negative values are not allowed.</value>
		public int PaddingRight { get; set; } = 0;
		/// <summary>
		/// This property defines the height of the item's top edge padding 
		/// space that should be used when laying out child items.
		/// </summary>
		/// <value>The bottom edge padding space.Negative values are not allowed.</value>
		public int PaddingTop { get; set; } = 0;
		/// <summary>
		/// This property defines whether the item should be positioned by the flexbox rules 
		/// of the layout engine(Relative) or have an absolute fixed position (Absolute). 
		/// If this property is set to Absolute, the<see cref="P:Xamarin.Flex.Item.Left" />, 
		/// <see cref = "P:Xamarin.Flex.Item.Right" />, <see cref = "P:Xamarin.Flex.Item.Top" /> 
		/// and < see cref= "P:Xam\arin.Flex.Item.Bottom"/> properties will then be 
		/// used to determine the item's fixed position in its container.
		/// </summary>
		/// <value>Any value part of the<see cref="T:Xamarin.Flex.Position" /> enumeration.</value>
		/// <remarks>The default value for this property is Relative</remarks>
		public Position Position { get; set; } = Position.Relative;
		/// <summary>
		/// This property defines the right edge absolute position of the item. It also defines 
		/// the item's width if <see cref="P:Xamarin.Flex.Item.Left" /> is also set and if 
		/// <see cref = "P:Xamarin.Flex.Item.Width" /> isn't set.It is ignored if 
		/// <see cref = "P:Xamarin.Flex.Item.Position" /> isn't set to Absolute.
		/// </summary>
		/// <remarks>The default value is UnsetValue.</remarks>
		public int Right { get; set; } = UnsetValue;
		/// <summary>
		/// This property defines the shrink factor of the item. In case the child 
		/// items overflow the main-axis of the container, this factor will be used 
		/// to determine how individual items should shrink so that all items can 
		/// fill inside the container. If this property is set to 0, the item 
		/// will not shrink.</summary>
		/// <value>The item shrink factor.</value>
		/// <remarks>The default value for this property is 1 (all items will shrink equally).</remarks>
		public int Shrink { get; set; } = 1;
		/// <summary>
		/// This property defines the top edge absolute position of the item. 
		/// It also defines the item's height if <see cref="P:Xamarin.Flex.Item.Bottom" /> 
		/// is also set and if <see cref = "P:Xamarin.Flex.Item.Height" /> is not set. 
		/// It is ignored if <see cref="P:Xamarin.Flex.Item.Position" /> is not set to Absolute.
		/// </summary>
		/// <value>The value for the property.</value>
		/// <remarks>The default value is UnsetValue.</remarks>
		public int Top { get; set; } = UnsetValue;
		/// <summary>
		/// This property defines the width size dimension of the item.
		/// </summary>
		/// <value>The width size dimension.</value>
		/// <remarks>The default value is UnsetValue.</remarks>
		public int Width { get; set; } = UnsetValue;
		/// <summary>
		/// This property defines whether child items should be laid out in a single 
		/// line(NoWrap) or multiple lines(Wrap or WrapReverse). If this property is set
		/// to Wrap or WrapReverse, <see cref = "P:Xamarin.Flex.Item.AlignContent" /> 
		/// can then be used to specify how the lines should be distributed.
		/// </summary>
		/// <value>Any value part of the<see cref="T:Xamarin.Flex.Wrap" /> enumeration.</value>
		/// <remarks>The default value for this property is NoWrap.</remarks>
		public Wrap Wrap { get; set; } = Wrap.NoWrap;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Terminal.Gui.View"/> class,
		/// this configures the view to be fully controlled by the Flex engine.
		/// </summary>
		public View ()
		{
			CanFocus = false;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Terminal.Gui.View"/> class, 
		/// setting only the width and height, which allow the view to be autosized, unlike
		/// the version that takes a Rect as a parameter which fixes the size of the view.
		/// </summary>
		/// <param name="width">Width.</param>
		/// <param name="height">Height.</param>
		public View (int width, int height)
		{
			Width = width;
			Height = height;
			CanFocus = false;
		}

#if false
		public void InsertAt (uint index, View child)
		{
			ValidateChild (child);
			(Children ?? (Children = new List<Item> ())).Insert ((int)index, child);
			child.SuperView = this;
			ShouldOrderChildren |= child.Order != 0;
		}

		public Item RemoveAt (uint index)
		{
			var child = Children [(int)index];
			child.SuperView = null;
			Children.RemoveAt ((int)index);
			return child;
		}
#endif
		public uint Count =>
			(uint)(subviews?.Count ?? 0);

		public View ItemAt (uint index) =>
			subviews? [(int)index];

		public View this [uint index] {
			get => ItemAt (index);
		}

		/// <summary>
		/// Returns the root view, this walks the hierarchy of parents until it reaches the view with no containing superview.
		/// </summary>
		/// <value>The root view.</value>
		public View Root {
			get {
				var root = this;
				while (root.SuperView != null)
					root = root.SuperView;
				return root;
			}
		}

		/// <summary>
		/// Determines the frames of each child(included nested ones) based on 
		/// the flexbox rules that were applied on this item and the children 
		/// themselves. After this method is called, the <see cref="P:Terminal.Gui.Frame" />
		/// properties can be accessed on child items.
		/// </summary>
		/// <remarks>This method must be called on a root (without parent) item where 
		/// the Width  and Height properties have also been set.</remarks>
		/// <exception cref = "InvalidOperationException" > Thrown if the item has a parent 
		/// (must be root) or if the item does not have a proper value set for 
		/// <see cref = "P:Xamarin.Flex.Item.Width" /> and < see cref = "P:Xamarin.Flex.Item.Height" />.</ exception >
		public void Layout ()
		{
#if false
			if (SuperView != null)
				throw new InvalidOperationException ("Layout() must be called on a root item (that hasn't been added to another item)");
#endif
			if (IsUnset (Width) || IsUnset (Height))
				throw new InvalidOperationException ("Layout() must be called on an item that has proper values for the Width and Height properties");
			if (SelfSizing != null)
				throw new InvalidOperationException ("Layout() cannot be called on an item that has the SelfSizing property set");
			layout_item (this, Width, Height);
		}

		public int Padding {
			set => PaddingTop = PaddingLeft = PaddingRight = PaddingBottom = value;
		}

		public int Margin {
			set => MarginTop = MarginLeft = MarginRight = MarginBottom = value;
		}

		public delegate void SelfSizingDelegate (View item, ref int width, ref int height);

		public SelfSizingDelegate SelfSizing { get; set; }

		IEnumerator IEnumerable.GetEnumerator () =>
		                       Subviews.GetEnumerator ();

		IEnumerator<View> IEnumerable<View>.GetEnumerator () =>
		                                   Subviews.GetEnumerator ();

		void ValidateChild (View child)
		{
			if (this == child)
				throw new ArgumentException ("cannot add item into self");
			if (child.SuperView != null)
				throw new ArgumentException ("child already has a superview");
		}

		static int GetDim (Rect rect, Dim dim)
		{
			if (dim == Dim.Height)
				return rect.Height;
			else
				return rect.Width;
		}

		static Rect SetDim (Rect target, Dim dim, int value)
		{
			if (dim == Dim.Height)
				target.Height = value;
			else
				target.Width = value;
			return target;
		}

		static int GetAxis (Rect rect, Axis axis)
		{
			if (axis == Axis.X)
				return rect.X;
			else
				return rect.Y;
		}

		static Rect SetAxis (Rect target, Axis axis, int value)
		{
			if (axis == Axis.X)
				target.X = value;
			else
				target.Y = value;
			return target;
		}

		static void layout_item (View item, int width, int height)
		{
			if (item.Subviews == null || item.Subviews.Count == 0)
				return;

			var layout = new flex_layout ();
			layout.init (item, width, height);
			layout.reset ();

			uint last_layout_child = 0;
			uint relative_children_count = 0;
			for (uint i = 0; i < item.Count; i++) {
				View child = layout.child_at (item, i);
				// Items with an absolute position have their frames determined
				// directly and are skipped during layout.
				if (child.Position == Position.Absolute) {
					int child_width = absolute_size (child.Width, child.Left, child.Right, width);
					int child_height = absolute_size (child.Height, child.Top, child.Bottom, height);
					int child_x = absolute_pos (child.Left, child.Right, child_width, width);
					int child_y = absolute_pos (child.Top, child.Bottom, child_height, height);

					child.Frame = new Rect (child_x, child_y, child_width, child_height);

					// Now that the item has a frame, we can layout its children.
					layout_item (child, child.Width, child.Height);
					continue;
				}

				// Initialize frame.
				child.Frame = new Rect (0, 0, child.Width, child.Height);

				// Main axis size defaults to 0.
				if (IsUnset (GetDim (child.Frame, layout.frame_size_i))) {
					child.Frame = SetDim (child.Frame, layout.frame_size_i, 0);
				}

				// Cross axis size defaults to the parent's size (or line size in wrap
				// mode, which is calculated later on).
				if (IsUnset (GetDim (child.Frame, layout.frame_size2_i))) {
					if (layout.wrap) {
						layout.need_lines = true;
					} else {
						child.Frame = SetDim (child.Frame, layout.frame_size2_i, (layout.vertical ? width : height)
								      - (layout.vertical ? child.MarginLeft : child.MarginTop)
								      - (layout.vertical ? child.MarginRight : child.MarginBottom));

					}
				}

				// Call the self_sizing callback if provided. Only non-Unset values
				// are taken into account. If the item's cross-axis align property
				// is set to stretch, ignore the value returned by the callback.
				if (child.SelfSizing != null) {
					var f = child.Frame;
					var w = f.Width;
					int h = f.Height;

					child.SelfSizing (child, ref w, ref h);

					// Handle X
					if (!(layout.frame_size2_i == Dim.Width && child_align (child, item) == AlignItems.Stretch)) {
						if (!IsUnset (w)) {
							f.Width = w;
							child.Frame = f;
						}
					}
						
					// Handle Y
					if (!(layout.frame_size2_i == Dim.Height && child_align (child, item) == AlignItems.Stretch)) {
						if (!IsUnset (h)) {
							f.Height = h;
							child.Frame = f;
						}
					}
				}

				// Honor the `basis' property which overrides the main-axis size.
				if (!child.Basis.IsAuto) {
					if (child.Basis.Length < 0) throw new Exception ("basis should >=0");
					if (child.Basis.IsRelative && child.Basis.Length > 1) throw new Exception ("relative basis should be <=1");
					int basis = child.Basis.Length;
					if (child.Basis.IsRelative)
						basis *= (layout.vertical ? height : width);
					child.Frame = SetDim (child.Frame, layout.frame_size_i, basis);
				}

				int child_size = GetDim (child.Frame, layout.frame_size_i);
				if (layout.wrap) {
					if (layout.flex_dim < child_size) {
						// Not enough space for this child on this line, layout the
						// remaining items and move it to a new line.
						layout_items (item, last_layout_child, i, relative_children_count, ref layout);

						layout.reset ();
						last_layout_child = i;
						relative_children_count = 0;
					}

					int child_size2 = GetDim (child.Frame, layout.frame_size2_i);
					if (!IsUnset (child_size2) && child_size2 > layout.line_dim) {
						layout.line_dim = child_size2;
					}
				}

				if (child.Grow < 0
					|| child.Shrink < 0)
					throw new Exception ("shrink and grow should be >= 0");

				layout.flex_grows += child.Grow;
				layout.flex_shrinks += child.Shrink;

				layout.flex_dim -= child_size
					+ (layout.vertical ? child.MarginTop : child.MarginLeft)
					+ (layout.vertical ? child.MarginBottom : child.MarginRight);

				relative_children_count++;

				if (child_size > 0 && child.Grow > 0) {
					layout.extra_flex_dim += child_size;
				}
			}

			// Layout remaining items in wrap mode, or everything otherwise.
			layout_items (item, last_layout_child, item.Count, relative_children_count, ref layout);

			// In wrap mode we may need to tweak the position of each line according to
			// the align_content property as well as the cross-axis size of items that
			// haven't been set yet.
			if (layout.need_lines && (layout.lines?.Length ?? 0) > 0) {
				int pos = 0;
				int spacing = 0;
				int flex_dim = layout.align_dim - layout.lines_sizes;
				if (flex_dim > 0)
					layout_align (item.AlignContent, flex_dim, (uint) (layout.lines?.Length ?? 0), ref pos, ref spacing);

				int old_pos = 0;
				if (layout.reverse2) {
					pos = layout.align_dim - pos;
					old_pos = layout.align_dim;
				}

				for (uint i = 0; i < (layout.lines?.Length ?? 0); i++) {

					flex_layout.flex_layout_line line = layout.lines [i];

					if (layout.reverse2) {
						pos -= line.size;
						pos -= spacing;
						old_pos -= line.size;
					}

					// Re-position the children of this line, honoring any child
					// alignment previously set within the line.
					for (uint j = line.child_begin; j < line.child_end; j++) {
						View child = layout.child_at (item, j);
						if (child.Position == Position.Absolute) {
							// Should not be re-positioned.
							continue;
						}
						if (IsUnset (GetDim (child.Frame, layout.frame_size2_i))) {
							// If the child's cross axis size hasn't been set it, it
							// defaults to the line size.
							child.Frame = SetDim (child.Frame, layout.frame_size2_i, line.size
									      + (item.AlignContent == AlignContent.Stretch ? spacing : 0));
						}
						child.Frame = SetAxis (child.Frame, layout.frame_pos2_i, pos + GetAxis (child.Frame, layout.frame_pos2_i) - old_pos);
					}

					if (!layout.reverse2) {
						pos += line.size;
						pos += spacing;
						old_pos += line.size;
					}
				}
			}

			layout.cleanup ();
		}

		static void layout_align (Justify align, int flex_dim, uint children_count, ref int pos_p, ref int spacing_p)
		{
			if (flex_dim < 0)
				throw new ArgumentException ();
			if (children_count > Int32.MaxValue-1)
				throw new ArgumentException ("Too many children");
			
			pos_p = 0;
			spacing_p = 0;

			switch (align) {
			case Justify.Start:
				return;
			case Justify.End:
				pos_p = flex_dim;
				return;
			case Justify.Center:
				pos_p = flex_dim / 2;
				return;
			case Justify.SpaceBetween:
				if (children_count > 0)
					spacing_p = flex_dim / (int)(children_count - 1);
				return;
			case Justify.SpaceAround:
				if (children_count > 0) {
					spacing_p = flex_dim / (int)children_count;
					pos_p = spacing_p / 2;
				}
				return;
			case Justify.SpaceEvenly:
				if (children_count > 0) {
					spacing_p = flex_dim / (int) (children_count + 1);
					pos_p = spacing_p;
				}
				return;
			default:
				throw new ArgumentException ();
			}
		}

		static void layout_align (AlignContent align, int flex_dim, uint children_count, ref int pos_p, ref int spacing_p)
		{
			if (flex_dim < 0)
				throw new ArgumentException ();
			if (children_count > Int32.MaxValue - 1)
				throw new ArgumentException ("Too many children");
			
			pos_p = 0;
			spacing_p = 0;

			switch (align) {
			case AlignContent.Start:
				return;
			case AlignContent.End:
				pos_p = flex_dim;
				return;
			case AlignContent.Center:
				pos_p = flex_dim / 2;
				return;
			case AlignContent.SpaceBetween:
				if (children_count > 0)
					spacing_p = flex_dim / (int)(children_count - 1);
				return;
			case AlignContent.SpaceAround:
				if (children_count > 0) {
					spacing_p = flex_dim / (int) children_count;
					pos_p = spacing_p / 2;
				}
				return;
			case AlignContent.SpaceEvenly:
				if (children_count > 0) {
					spacing_p = flex_dim / (int)(children_count + 1);
					pos_p = spacing_p;
				}
				return;
			case AlignContent.Stretch:
				spacing_p = flex_dim / (int) children_count;
				return;
			default:
				throw new ArgumentException ();
			}
		}

		static void layout_items (View item, uint child_begin, uint child_end, uint children_count, ref flex_layout layout)
		{
			if (children_count > (child_end - child_begin))
				throw new ArgumentException ();
			if (children_count <= 0)
				return;
			if (layout.flex_dim > 0 && layout.extra_flex_dim > 0) {
				// If the container has a positive flexible space, let's add to it
				// the sizes of all flexible children.
				layout.flex_dim += layout.extra_flex_dim;
			}

			// Determine the main axis initial position and optional spacing.
			int pos = 0;
			int spacing = 0;
			if (layout.flex_grows == 0 && layout.flex_dim > 0) {
				layout_align (item.JustifyContent, layout.flex_dim, children_count, ref pos, ref spacing);

				if (layout.reverse) {
					pos = layout.size_dim - pos;
				}
			}

			if (layout.reverse) {
				pos -= layout.vertical ? item.PaddingBottom : item.PaddingRight;
			} else {
				pos += layout.vertical ? item.PaddingTop : item.PaddingLeft;
			}
			if (layout.wrap && layout.reverse2) {
				layout.pos2 -= layout.line_dim;
			}

			for (uint i = child_begin; i < child_end; i++) {
				View child = layout.child_at (item, i);
				if (child.Position == Position.Absolute) {
					// Already positioned.
					continue;
				}

				// Grow or shrink the main axis item size if needed.
				int flex_size = 0;
				if (layout.flex_dim > 0) {
					if (child.Grow != 0) {
						child.Frame = SetDim (child.Frame, layout.frame_size_i, 0); // Ignore previous size when growing.
						flex_size = (layout.flex_dim / layout.flex_grows) * child.Grow;
					}
				} else if (layout.flex_dim < 0) {
					if (child.Shrink != 0) {
						flex_size = (layout.flex_dim / layout.flex_shrinks) * child.Shrink;
					}
				}
				child.Frame = SetDim (child.Frame, layout.frame_size_i, GetDim (child.Frame, layout.frame_size_i) + flex_size);

				// Set the cross axis position (and stretch the cross axis size if
				// needed).
				int align_size = GetDim (child.Frame, layout.frame_size2_i);
				int align_pos = layout.pos2 + 0;
				switch (child_align (child, item)) {
				case AlignItems.End:
					align_pos += layout.line_dim - align_size - (layout.vertical ? child.MarginRight : child.MarginBottom);
					break;

				case AlignItems.Center:
					align_pos += (layout.line_dim / 2) - (align_size / 2)
						+ ((layout.vertical ? child.MarginLeft : child.MarginTop)
						   - (layout.vertical ? child.MarginRight : child.MarginBottom));
					break;

				case AlignItems.Stretch:
					if (align_size == 0) {
						child.Frame = SetDim (child.Frame, layout.frame_size2_i, layout.line_dim
								      - ((layout.vertical ? child.MarginLeft : child.MarginTop)
									 + (layout.vertical ? child.MarginRight : child.MarginBottom)));
					}
					align_pos += (layout.vertical ? child.MarginLeft : child.MarginTop);
					break;
				case AlignItems.Start:
					align_pos += (layout.vertical ? child.MarginLeft : child.MarginTop);
					break;

				default:
					throw new Exception ();
				}
				child.Frame = SetAxis (child.Frame, layout.frame_pos2_i, align_pos);

				// Set the main axis position.
				if (layout.reverse) {
					pos -= (layout.vertical ? child.MarginBottom : child.MarginRight);
					pos -= GetDim (child.Frame, layout.frame_size_i);
					child.Frame = SetAxis(child.Frame, layout.frame_pos_i, pos);
					pos -= spacing;
					pos -= (layout.vertical ? child.MarginTop : child.MarginLeft);
				} else {
					pos += (layout.vertical ? child.MarginTop : child.MarginLeft);
					child.Frame = SetAxis (child.Frame, layout.frame_pos_i, pos);
					pos += GetDim (child.Frame, layout.frame_size_i);
					pos += spacing;
					pos += (layout.vertical ? child.MarginBottom : child.MarginRight);
				}

				// Now that the item has a frame, we can layout its children.
				layout_item (child, child.Frame.Width, child.Frame.Height);
			}

			if (layout.wrap && !layout.reverse2) {
				layout.pos2 += layout.line_dim;
			}

			if (layout.need_lines) {
				Array.Resize (ref layout.lines, (layout.lines?.Length ?? 0) + 1);

				ref flex_layout.flex_layout_line line = ref layout.lines [layout.lines.Length - 1];

				line.child_begin = child_begin;
				line.child_end = child_end;
				line.size = layout.line_dim;

				layout.lines_sizes += line.size;
			}
		}

		static int absolute_size (int val, int pos1, int pos2, int dim) =>
			!IsUnset(val) ? val : (!IsUnset(pos1) && !IsUnset(pos2) ? dim - pos2 - pos1 : 0);

		static int absolute_pos (int pos1, int pos2, int size, int dim) =>
			!IsUnset (pos1) ? pos1 : (!IsUnset (pos2) ? dim - size - pos2 : 0);

		static AlignItems child_align (View child, View parent) =>
			child.AlignSelf == AlignSelf.Auto ? parent.AlignItems : (AlignItems)child.AlignSelf;

		enum Dim {
			Width, Height
		};
		enum Axis {
			X, Y
		};

		struct flex_layout {
			// Set during init.
			public bool wrap;
			public bool reverse;                            // whether main axis is reversed
			public bool reverse2;                           // whether cross axis is reversed (wrap only)
			public bool vertical;
			public int size_dim;                          // main axis parent size
			public int align_dim;                         // cross axis parent size
			public Axis frame_pos_i;                        // main axis position
			public Axis frame_pos2_i;                       // cross axis position
			public Dim frame_size_i;                       // main axis size
			public Dim frame_size2_i;                      // cross axis size
			uint [] ordered_indices;

			// Set for each line layout.
			public int line_dim;                          // the cross axis size
			public int flex_dim;                          // the flexible part of the main axis size
			public int extra_flex_dim;            // sizes of flexible items
			public int flex_grows;
			public int flex_shrinks;
			public int pos2;                                      // cross axis position

			// Calculated layout lines - only tracked when needed:
			//   - if the root's align_content property isn't set to FLEX_ALIGN_START
			//   - or if any child item doesn't have a cross-axis size set
			public bool need_lines;
			public struct flex_layout_line {
				public uint child_begin;
				public uint child_end;
				public int size;
			};

			public flex_layout_line [] lines;
			public int lines_sizes;

			//LAYOUT_RESET
			public void reset ()
			{
				line_dim = wrap ? 0 : align_dim;
				flex_dim = size_dim;
				extra_flex_dim = 0;
				flex_grows = 0;
				flex_shrinks = 0;
			}

			//layout_init
			public void init (View item, int width, int height)
			{
				if (item.PaddingLeft < 0
				    || item.PaddingRight < 0
				    || item.PaddingTop < 0
				    || item.PaddingBottom < 0)
					throw new ArgumentException ();

				width -= item.PaddingLeft + item.PaddingRight;
				height -= item.PaddingTop + item.PaddingBottom;
				if (width < 0
				    || height < 0)
					throw new ArgumentException ();

				reverse = item.Direction == Direction.RowReverse || item.Direction == Direction.ColumnReverse;
				vertical = true;
				switch (item.Direction) {
				case Direction.Row:
				case Direction.RowReverse:
					vertical = false;
					size_dim = width;
					align_dim = height;
					frame_pos_i = Axis.X;
					frame_pos2_i = Axis.Y;
					frame_size_i = Dim.Width;
					frame_size2_i = Dim.Height;
					break;
				case Direction.Column:
				case Direction.ColumnReverse:
					size_dim = height;
					align_dim = width;
					frame_pos_i = Axis.Y;
					frame_pos2_i = Axis.X;
					frame_size_i = Dim.Height;
					frame_size2_i = Dim.Width;
					break;
				}

				ordered_indices = null;
				if (item.ShouldOrderChildren && item.Count > 0) {
					var indices = new uint [item.Count];
					// Creating a list of item indices sorted using the children's `order'
					// attribute values. We are using a simple insertion sort as we need
					// stability (insertion order must be preserved) and cross-platform
					// support. We should eventually switch to merge sort (or something
					// else) if the number of items becomes significant enough.
					for (uint i = 0; i < item.Count; i++) {
						indices [i] = i;
						for (uint j = i; j > 0; j--) {
							uint prev = indices [j - 1];
							uint curr = indices [j];
							if (item.Subviews [(int)prev].Order <= item.Subviews [(int)curr].Order) {
								break;
							}
							indices [j - 1] = curr;
							indices [j] = prev;
						}
					}
					ordered_indices = indices;
				}

				flex_dim = 0;
				flex_grows = 0;
				flex_shrinks = 0;

				reverse2 = false;
				wrap = item.Wrap != Wrap.NoWrap;
				if (wrap) {
					if (item.Wrap == Wrap.WrapReverse) {
						reverse2 = true;
						pos2 = align_dim;
					}
				} else {
					pos2 = vertical ? item.PaddingLeft : item.PaddingTop;
				}

				need_lines = wrap && item.AlignContent != AlignContent.Start;
				lines = null;
				lines_sizes = 0;
			}

			public View child_at (View item, uint i) =>
			item.Subviews [(int)(ordered_indices? [i] ?? i)];

			public void cleanup ()
			{
				ordered_indices = null;
				lines = null;
			}
		}
	}
}
