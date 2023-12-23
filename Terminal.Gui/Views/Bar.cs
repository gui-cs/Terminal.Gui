using System;
using System.Linq;

namespace Terminal.Gui;


public class BarItem : View {
	public BarItem ()
	{
		Height = 1;
	}
	public override string Text {
		set {
			base.Text = $"{KeyBindings.Bindings.FirstOrDefault (b => b.Value.Scope != KeyBindingScope.Focused).Key} `{value}`";
		}
		get {
			return $"{KeyBindings.Bindings.FirstOrDefault(b => b.Value.Scope != KeyBindingScope.Focused).Key} `{base.Text}`";
		}
	}
}
/// <summary>
/// The Bar <see cref="View"/> provides a container for other views to be used as a toolbar or status bar.
/// </summary>
/// <remarks>
/// Views added to a Bar will be positioned horizontally from left to right.
/// </remarks>
public class Bar : View {
	/// <inheritdoc/>
	public Bar () => SetInitialProperties ();

	void SetInitialProperties ()
	{
		X = 0;
		Y = Pos.AnchorEnd (1);
		Width = Dim.Fill ();
		Height = 1;
		AutoSize = false;
		ColorScheme = Colors.Menu;
	}

	public override void Add (View view)
	{
		// Align the views horizontally from left to right. Use Border to separate them.

		// until we know this view is not the rightmost, make it fill the bar
		//view.Width = Dim.Fill ();

		view.Margin.Thickness = new Thickness (1, 0, 0, 0);
		view.Margin.ColorScheme = Colors.Menu;

		// Light up right border
		view.BorderStyle = LineStyle.Single;
		view.Border.Thickness = new Thickness (0, 0, 1, 0);
		view.Padding.Thickness = new Thickness (0, 0, 1, 0);
		view.Padding.ColorScheme = Colors.Menu;

		// leftmost view is at X=0
		if (Subviews.Count == 0) {
			view.X = 0;
		} else {
			// Make view to right be autosize
			//Subviews [^1].AutoSize = true;

			// Align the view to the right of the previous view
			view.X = Pos.Right (Subviews [^1]);

		}

		base.Add (view);
	}
}