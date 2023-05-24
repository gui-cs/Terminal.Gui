using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using Xunit;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.DriverTests {
	public class AttributeTests {
		[Fact]
		public void Constuctors_Constuct ()
		{
			var driver = new FakeDriver ();
			Application.Init (driver);
			driver.Init (() => { });

			// Test parameterless constructor
			var attr = new Attribute ();

			Assert.Equal (default (int), attr.Value);
			Assert.Equal (default (Color), attr.Foreground);
			Assert.Equal (default (Color), attr.Background);

			// Test foreground, background
			var fg = new Color ();
			fg = Color.Red;

			var bg = new Color ();
			bg = Color.Blue;

			attr = new Attribute (fg, bg);

			Assert.True (attr.Initialized);
			Assert.True (attr.HasValidColors);
			Assert.Equal (fg, attr.Foreground);
			Assert.Equal (bg, attr.Background);

			attr = new Attribute (fg);
			Assert.True (attr.Initialized);
			Assert.True (attr.HasValidColors);
			Assert.Equal (fg, attr.Foreground);
			Assert.Equal (fg, attr.Background);

			attr = new Attribute (bg);
			Assert.True (attr.Initialized);
			Assert.True (attr.HasValidColors);
			Assert.Equal (bg, attr.Foreground);
			Assert.Equal (bg, attr.Background);

			driver.End ();
			Application.Shutdown ();
		}

		[Fact]
		public void Implicit_Assign ()
		{
			var driver = new FakeDriver ();
			Application.Init (driver);
			driver.Init (() => { });

			var attr = new Attribute ();

			var value = 42;
			var fg = new Color ();
			fg = Color.Red;

			var bg = new Color ();
			bg = Color.Blue;

			// Test conversion to int
			attr = new Attribute (value, fg, bg);
			int value_implicit = attr.Value;
			Assert.Equal (value, value_implicit);

			Assert.Equal (value, attr.Value);

			driver.End ();
			Application.Shutdown ();
		}

		[Fact]
		public void Make_SetsNotInitialized_NoDriver ()
		{
			var fg = new Color ();
			fg = Color.Red;

			var bg = new Color ();
			bg = Color.Blue;

			var a = Attribute.Make (fg, bg);

			Assert.False (a.Initialized);
		}

		[Fact]
		public void Make_Creates ()
		{
			var driver = new FakeDriver ();
			Application.Init (driver);
			driver.Init (() => { });

			var fg = new Color ();
			fg = Color.Red;

			var bg = new Color ();
			bg = Color.Blue;

			var attr = Attribute.Make (fg, bg);
			Assert.True (attr.Initialized);
			Assert.Equal (fg, attr.Foreground);
			Assert.Equal (bg, attr.Background);

			driver.End ();
			Application.Shutdown ();
		}

		[Fact]
		public void Make_Creates_NoDriver ()
		{

			var fg = new Color ();
			fg = Color.Red;

			var bg = new Color ();
			bg = Color.Blue;

			var attr = Attribute.Make (fg, bg);
			Assert.False (attr.Initialized);
			Assert.Equal (fg, attr.Foreground);
			Assert.Equal (bg, attr.Background);
		}

		[Fact]
		public void Get_Asserts_NoDriver ()
		{
			Assert.Throws<InvalidOperationException> (() => Attribute.Get ());
		}

		[Fact]
		public void Get_Gets ()
		{
			var driver = new FakeDriver ();
			Application.Init (driver);
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

		[Fact]
		public void IsValid_Tests ()
		{
			var attr = new Attribute ();
			Assert.True (attr.HasValidColors);

			attr = new Attribute (Color.Red, Color.Green);
			Assert.True (attr.HasValidColors);

			attr = new Attribute (Color.Red, (Color)(-1));
			Assert.False (attr.HasValidColors);

			attr = new Attribute ((Color)(-1), Color.Green);
			Assert.False (attr.HasValidColors);

			attr = new Attribute ((Color)(-1), (Color)(-1));
			Assert.False (attr.HasValidColors);
		}
	}
}
