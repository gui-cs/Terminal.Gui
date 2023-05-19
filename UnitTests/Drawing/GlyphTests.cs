using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Xunit;

namespace Terminal.Gui.TextTests;

public class GlyphTests {
	[Fact]
	public void Default_Glyphs_Normalize ()
	{
		var defs = new GlyphDefinitions () {
			
		};
		// enumerate all properties
		foreach (var prop in typeof (GlyphDefinitions).GetProperties ()) {
			if (prop.PropertyType == typeof (Rune)) {
				var glyph = (Rune)prop.GetValue (null);
//				Assert.Equal (glyph, glyph (defs));
			}
		}

	}
}
