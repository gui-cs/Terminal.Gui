// 
// Flex layout by Laurent Sansonetti
// Port to C# by Stephane Delcroix
//

using System;
using System.Collections;
using System.Collections.Generic;

namespace Terminal.Gui {
	public enum AlignContent {
		Stretch = 1,
		Center = 2,
		Start = 3,
		End = 4,
		SpaceBetween = 5,
		SpaceAround = 6,
		SpaceEvenly = 7,
	}

	public enum AlignItems {
		Stretch = 1,
		Center = 2,
		Start = 3,
		End = 4,
		//Baseline = 8,
	}

	public enum AlignSelf {
		Auto = 0,
		Stretch = 1,
		Center = 2,
		Start = 3,
		End = 4,
		//Baseline = 8,
	}

	public enum Direction {
		Row = 0,
		RowReverse = 1,
		Column = 2,
		ColumnReverse = 3,
	}

	public enum Justify {
		Center = 2,
		Start = 3,
		End = 4,
		SpaceBetween = 5,
		SpaceAround = 6,
		SpaceEvenly = 7,
	}

	public enum Position {
		Relative = 0,
		Absolute = 1,
	}

	public enum Wrap {
		NoWrap = 0,
		Wrap = 1,
		WrapReverse = 2,
	}

	public struct Basis {
		readonly bool _isRelative;
		readonly bool _isLength;
		readonly int _length;
		public static Basis Auto = new Basis ();
		public bool IsRelative => _isRelative;
		public bool IsAuto => !_isLength && !_isRelative;
		public int Length => _length;
		public Basis (int length, bool isRelative = false)
		{
			_length = length;
			_isLength = !isRelative;
			_isRelative = isRelative;
		}
	}

	public partial class View : IEnumerable<View> {
		bool ShouldOrderChildren { get; set; }

		public AlignContent AlignContent { get; set; } = AlignContent.Stretch;
		public AlignItems AlignItems { get; set; } = AlignItems.Stretch;
		public AlignSelf AlignSelf { get; set; } = AlignSelf.Auto;
		public Basis Basis { get; set; } = Basis.Auto;
		public int Bottom { get; set; } = UnsetValue;
		public Direction Direction { get; set; } = Direction.Column;
		public int Grow { get; set; } = 0;
		public int Height { get; set; } = UnsetValue;
		public Justify JustifyContent { get; set; } = Justify.Start;
		public int Left { get; set; } = UnsetValue;
		public int MarginBottom { get; set; } = 0;
		public int MarginLeft { get; set; } = 0;
		public int MarginRight { get; set; } = 0;

		/// <summary>
		/// This value is used to reprensent that a dimension has not been set
		/// </summary>
		public const int UnsetValue = Int32.MaxValue;

		static bool IsUnset (int value)
		{
			return value == UnsetValue;
		}

		public int MarginTop { get; set; } = 0;

		int order;
		public int Order {
			get => order;
			set {
				if ((order = value) != 0 && SuperView != null)
					SuperView.ShouldOrderChildren = true;
			}
		}

		public int PaddingBottom { get; set; } = 0;
		public int PaddingLeft { get; set; } = 0;
		public int PaddingRight { get; set; } = 0;
		public int PaddingTop { get; set; } = 0;

		public Position Position { get; set; } = Position.Relative;
		public int Right { get; set; } = UnsetValue;
		public int Shrink { get; set; } = 1;
		public int Top { get; set; } = UnsetValue;
		public int Width { get; set; } = UnsetValue;
		public Wrap Wrap { get; set; } = Wrap.NoWrap;

		public View ()
		{
		}

		public View (int width, int height)
		{
			Width = width;
			Height = height;
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

		public void Layout ()
		{
			if (SuperView != null)
				throw new InvalidOperationException ("Layout() must be called on a root item (that hasn't been added to another item)");
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