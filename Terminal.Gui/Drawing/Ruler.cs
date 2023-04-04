using NStack;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.Json.Serialization;
using Terminal.Gui.Configuration;
using Terminal.Gui.Graphs;

namespace Terminal.Gui {
	/// <summary>
	/// Draws a ruler on the screen.
	/// </summary>
	/// <remarks>
	/// <para>
	/// </para>
	/// </remarks>
	public class Ruler {

		/// <summary>
		/// Gets or sets whether the ruler is drawn horizontally or vertically. The default is horizontally.
		/// </summary>
		public Orientation Orientation { get; set; }

		/// <summary>
		/// Gets or sets the lenght of the ruler. The default is 0.
		/// </summary>
		public int Length { get; set; }

		/// <summary>
		/// Gets or sets the foreground and backgrond color to use.
		/// </summary>
		public Attribute Attribute { get; set; }

		/// <summary>
		/// Gets or sets the ruler template. This will be repeated in the ruler. The default is "0123456789".
		/// </summary>
		public string Template { get; set; } = "0123456789";


		/// <summary>
		/// Draws the <see cref="Ruler"/>. 
		/// </summary>
		/// <param name="location">The location to start drawing the ruler, in screen-relative coordinates.</param>
		public void Draw (Point location)
		{
			if (Length < 1) {
				return;
			}

			if (Orientation == Orientation.Horizontal) {
				var hrule = Template.Repeat ((int)Math.Ceiling ((double)Length / (double)Template.Length)) [0..Length];
				// Top
				Application.Driver.Move (location.X, location.Y);
				Application.Driver.AddStr (hrule);

			} else {
				var vrule = Template.Repeat ((int)Math.Ceiling ((double)(Length * 2) / (double)Template.Length)) [0..(Length * 2)];
				for (var r = location.Y; r < location.Y + Length; r++) {
					Application.Driver.Move (location.X, r);
					Application.Driver.AddRune (vrule [r - location.Y]);
				}
			}
		}
	}

	internal static class StringExtensions {
		public static string Repeat (this string instr, int n)
		{
			if (n <= 0) {
				return null;
			}

			if (string.IsNullOrEmpty (instr) || n == 1) {
				return instr;
			}

			return new StringBuilder (instr.Length * n)
				.Insert (0, instr, n)
				.ToString ();
		}
	}
}