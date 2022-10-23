using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using Xunit;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.ConsoleDrivers {
	public class AttributeTests {
		[Fact]
		public void Constuctors_Constuct ()
		{
			var driver = new FakeDriver ();
			Application.Init (driver, new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			driver.Init (() => { });

			// Test parameterless constructor
			var attr = new Attribute ();

			Assert.Equal (default (int), attr.Value);
			Assert.Equal (default (Color), attr.Foreground);
			Assert.Equal (default (Color), attr.Background);

			// Test value, foreground, background
			var value = 42;
			var fg = new Color ();
			fg = Color.Red;

			var bg = new Color ();
			bg = Color.Blue;

			attr = new Attribute (value, fg, bg);

			Assert.Equal (value, attr.Value);
			Assert.Equal (fg, attr.Foreground);
			Assert.Equal (bg, attr.Background);

			// value, foreground, background
			attr = new Attribute (fg, bg);

			Assert.Equal (fg, attr.Foreground);
			Assert.Equal (bg, attr.Background);

			attr = new Attribute (fg);
			Assert.Equal (fg, attr.Foreground);
			Assert.Equal (fg, attr.Background);

			attr = new Attribute (bg);
			Assert.Equal (bg, attr.Foreground);
			Assert.Equal (bg, attr.Background);

			driver.End ();
			Application.Shutdown ();
		}

		[Fact]
		public void Implicit_Assign ()
		{
			var driver = new FakeDriver ();
			Application.Init (driver, new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			driver.Init (() => { });

			var attr = new Attribute ();

			var value = 42;
			var fg = new Color ();
			fg = Color.Red;

			var bg = new Color ();
			bg = Color.Blue;

			// Test conversion to int
			attr = new Attribute (value, fg, bg);
			int value_implicit = (int)attr.Value;
			Assert.Equal (value, value_implicit);

			// Test conversion from int
			attr = value;
			Assert.Equal (value, attr.Value);

			driver.End ();
			Application.Shutdown ();
		}

		[Fact]
		public void Make_Asserts_IfNotInit ()
		{
			var fg = new Color ();
			fg = Color.Red;

			var bg = new Color ();
			bg = Color.Blue;

			Assert.Throws<InvalidOperationException> (() => Attribute.Make (fg, bg));
		}

		[Fact]
		public void Make_Creates ()
		{
			var driver = new FakeDriver ();
			Application.Init (driver, new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			driver.Init (() => { });

			var fg = new Color ();
			fg = Color.Red;

			var bg = new Color ();
			bg = Color.Blue;

			var attr =  Attribute.Make (fg, bg);

			Assert.Equal (fg, attr.Foreground);
			Assert.Equal (bg, attr.Background);

			driver.End ();
			Application.Shutdown ();
		}

		[Fact]
		public void Get_Asserts_IfNotInit ()
		{
			Assert.Throws<InvalidOperationException> (() => Attribute.Get ());
		}

		[Fact]
		public void Get_Gets ()
		{
			var driver = new FakeDriver ();
			Application.Init (driver, new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			driver.Init (() => { });

			var value = 42;
			var fg = new Color ();
			fg = Color.Red;

			var bg = new Color ();
			bg = Color.Blue;

			var attr = new Attribute (value, fg, bg);

			driver.SetAttribute (attr);

			var ret_attr = Attribute.Get ();

			Assert.Equal (value, ret_attr.Value);
			Assert.Equal (fg, ret_attr.Foreground);
			Assert.Equal (bg, ret_attr.Background);

			driver.End ();
			Application.Shutdown ();
		}

		[Fact]
		[AutoInitShutdown]
		public void GetColors_Based_On_Value ()
		{
			var driver = Application.Driver;
			var attrValue = new Attribute (Color.Red, Color.Green).Value;
			driver.GetColors (attrValue, out Color fg, out Color bg);

			Assert.Equal (Color.Red, fg);
			Assert.Equal (Color.Green, bg);
		}
	}
}
