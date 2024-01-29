using System;
using System.Collections.Generic;

namespace Terminal.Gui; 

/// <summary>
/// Defines rendering options that affect how the table is displayed.
/// 
/// <a href="../docs/tableview.md">See TableView Deep Dive for more information</a>.
/// </summary>
public class TableStyle {

	/// <summary>
	/// Gets or sets the LineStyle for the borders surrounding header rows of a <see cref="TableView"/>.
	/// Defaults to <see cref="LineStyle.Single"/>.
	/// </summary>
	public LineStyle OuterHeaderBorderStyle { get; set; } = LineStyle.Single;

	/// <summary>
	/// Gets or sets the LineStyle for the vertical lines separating header items in a <see cref="TableView"/>.
	/// Defaults to <see cref="LineStyle.Single"/>.
	/// </summary>
	public LineStyle InnerHeaderBorderStyle { get; set; } = LineStyle.Single;

	/// <summary>
	/// Gets or sets the LineStyle for the borders surrounding the regular (non-header) portion of a <see cref="TableView"/>.
	/// Defaults to <see cref="LineStyle.Single"/>.
	/// </summary>
	public LineStyle OuterBorderStyle { get; set; } = LineStyle.Single;

	/// <summary>
	/// Gets or sets the LineStyle for the lines separating regular (non-header) items in a <see cref="TableView"/>.
	/// Defaults to <see cref="LineStyle.Single"/>.
	/// </summary>
	public LineStyle InnerBorderStyle { get; set; } = LineStyle.Single;

	/// <summary>
	/// Gets or sets the color Attribute of the inner and outer borders of a <see cref="TableView"/>.
	/// Defaults to Attribute(-1, -1) which results in <see cref="Border.ColorScheme.Normal"/>.
	/// </summary>
	public Attribute BorderColor { get; set; } = new Attribute(-1, -1);

	/// <summary>
	/// Gets or sets a flag indicating whether to render headers of a <see cref="TableView"/>.
	/// Defaults to <see langword="true"/>.
	/// </summary>
	/// <remarks><see cref="ShowHorizontalHeaderOverline"/>, <see cref="ShowHorizontalHeaderUnderline"/> etc
	/// may still be used even if <see cref="ShowHeaders"/> is <see langword="false"/>.</remarks>
	public bool ShowHeaders { get; set; } = true;

	/// <summary>
	/// When scrolling down always lock the column headers in place as the first row of the table
	/// </summary>
	public bool AlwaysShowHeaders { get; set; } = false;

	/// <summary>
	/// True to render a solid line above the headers
	/// </summary>
	public bool ShowHorizontalHeaderOverline { get; set; } = true;

	/// <summary>
	/// True to render a solid line under the headers
	/// </summary>
	public bool ShowHorizontalHeaderUnderline { get; set; } = true;

	/// <summary>
	/// True to render a solid line through the headers (only when Overline and/or Underline are <see langword="false"/>)
	/// </summary>
	public bool ShowHorizontalHeaderThroughline { get; set; } = false;

	/// <summary>
	/// True to render a solid line vertical line between cells
	/// </summary>
	public bool ShowVerticalCellLines { get; set; } = true;

	/// <summary>
	/// True to render a solid line vertical line between headers
	/// </summary>
	public bool ShowVerticalHeaderLines { get; set; } = true;

	/// <summary>
	/// True to render a arrows on the right/left of the table when 
	/// there are more column(s) that can be scrolled to.  Requires
	/// <see cref="ShowHorizontalHeaderUnderline"/> to be true.
	/// Defaults to true
	/// </summary>
	public bool ShowHorizontalScrollIndicators { get; set; } = true;


	/// <summary>
	/// Gets or sets a flag indicating whether there should be a horizontal line after all the data
	/// in the table. Defaults to <see langword="false"/>.
	/// </summary>
	public bool ShowHorizontalBottomline { get; set; } = false;

	/// <summary>
	/// True to invert the colors of the entire selected cell in the <see cref="TableView"/>.
	/// Helpful for when <see cref="TableView.FullRowSelect"/> is on, especially when the <see cref="ConsoleDriver"/> doesn't show
	/// the cursor
	/// </summary>
	public bool InvertSelectedCell { get; set; } = false;

	/// <summary>
	/// True to invert the colors of the first symbol of the selected cell in the <see cref="TableView"/>.
	/// This gives the appearance of a cursor for when the <see cref="ConsoleDriver"/> doesn't otherwise show
	/// this
	/// </summary>
	public bool InvertSelectedCellFirstCharacter { get; set; } = false;

	// NOTE: This is equivalent to True by default after change to LineCanvas borders and can't be turned off
	// without disabling ShowVerticalCellLines, however  SeparatorSymbol and HeaderSeparatorSymbol could be
	// used to approximate the previous default behavior with FullRowSelect
	// TODO: Explore ways of changing this without a workaround
	/// <summary>
	/// Gets or sets a flag indicating whether to force <see cref="ColorScheme.Normal"/> use when rendering
	/// vertical cell lines (even when <see cref="TableView.FullRowSelect"/> is on).
	/// </summary>
	//public bool AlwaysUseNormalColorForVerticalCellLines { get; set; } = false;

	/// <summary>
	/// The symbol to add after each header value to visually seperate values (if not using vertical gridlines)
	/// </summary>
	public char HeaderSeparatorSymbol { get; set; } = ' ';

	/// <summary>
	/// The symbol to add after each cell value to visually seperate values (if not using vertical gridlines)
	/// </summary>
	public char SeparatorSymbol { get; set; } = ' ';

	/// <summary>
	/// The text representation that should be rendered for cells with the value <see cref="DBNull.Value"/>
	/// </summary>
	public string NullSymbol { get; set; } = "-";

	/// <summary>
	/// The symbol to pad around values (between separators) in the header line
	/// </summary>
	public char HeaderPaddingSymbol { get; set; } = ' ';

	/// <summary>
	/// The symbol to pad around values (between separators)
	/// </summary>
	public char CellPaddingSymbol { get; set; } = ' ';

	/// <summary>
	/// The symbol to pad outside table (if both <see cref="ExpandLastColumn"/> and <see cref="AddEmptyColumn"/>
	/// are False)
	/// </summary>
	public char BackgroundSymbol { get; set; } = ' ';

	/// <summary>
	/// Collection of columns for which you want special rendering (e.g. custom column lengths, text alignment etc)
	/// </summary>
	public Dictionary<int, ColumnStyle> ColumnStyles { get; set; } = new Dictionary<int, ColumnStyle> ();

	/// <summary>
	/// Delegate for coloring specific rows in a different color.  For cell color <see cref="ColumnStyle.ColorGetter"/>
	/// </summary>
	/// <value></value>
	public RowColorGetterDelegate RowColorGetter { get; set; }

	/// <summary>
	/// Determines rendering when the last column in the table is visible but its
	/// content or <see cref="ColumnStyle.MaxWidth"/> is less than the remaining 
	/// space in the control.  True (the default) will expand the column to fill
	/// the remaining bounds of the control.  If false, <see cref="AddEmptyColumn"/>
	/// determines the behavior of the remaining space.
	/// </summary>
	/// <value></value>
	public bool ExpandLastColumn { get; set; } = true;

	/// <summary>
	/// Determines rendering when the last column in the table is visible but its
	/// content or <see cref="ColumnStyle.MaxWidth"/> is less than the remaining 
	/// space in the control *and* <see cref="ExpandLastColumn"/> is False.  True (the default)
	/// will add a blank column that cannot be selected in the remaining space.
	/// False will fill the remaining space with <see cref="BackgroundSymbol"/>.
	/// </summary>
	/// <value></value>
	public bool AddEmptyColumn { get; set; } = true;

	/// <summary>
	/// <para>
	/// Determines how <see cref="TableView.ColumnOffset"/> is updated when scrolling
	/// right off the end of the currently visible area.
	/// </para>
	/// <para>
	/// If true then when scrolling right the scroll offset is increased the minimum required to show
	/// the new column.  This may be slow if you have an incredibly large number of columns in
	/// your table and/or slow <see cref="ColumnStyle.RepresentationGetter"/> implementations
	/// </para>
	/// <para>
	/// If false then scroll offset is set to the currently selected column (i.e. PageRight).
	/// </para>
	/// </summary>
	public bool SmoothHorizontalScrolling { get; set; } = true;

	/// <summary>
	/// Returns the entry from <see cref="ColumnStyles"/> for the given <paramref name="col"/> or null if no custom styling is defined for it
	/// </summary>
	/// <param name="col"></param>
	/// <returns></returns>
	public ColumnStyle GetColumnStyleIfAny (int col)
	{
		return ColumnStyles.TryGetValue (col, out ColumnStyle result) ? result : null;
	}

	/// <summary>
	/// Returns an existing <see cref="ColumnStyle"/> for the given <paramref name="col"/> or creates a new one with default options
	/// </summary>
	/// <param name="col"></param>
	/// <returns></returns>
	public ColumnStyle GetOrCreateColumnStyle (int col)
	{
		if (!ColumnStyles.ContainsKey (col))
			ColumnStyles.Add (col, new ColumnStyle ());

		return ColumnStyles [col];
	}
}