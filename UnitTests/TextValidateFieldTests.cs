using System.Text.RegularExpressions;
using Terminal.Gui.TextValidateProviders;

using Xunit;

namespace Terminal.Gui {
	public class TextValidateField_NET_Provider_Tests {
		public TextValidateField_NET_Provider_Tests ()
		{
			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));
		}

		[Fact]
		public void Initialized_With_Cursor_On_First_Editable_Character ()
		{
			//                                                            *
			//                                                         0123456789
			var field = new TextValidateField<NetMaskedTextProvider> ("--(0000)--") {
				TextAlignment = TextAlignment.Centered,
				Width = 20
			};

			field.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers { }));

			Assert.Equal ("--(1___)--", field.Text);
		}

		[Fact]
		public void Input_Ilegal_Character ()
		{
			//                                                            *
			//                                                         0123456789
			var field = new TextValidateField<NetMaskedTextProvider> ("--(0000)--") {
				TextAlignment = TextAlignment.Centered,
				Width = 20
			};

			field.ProcessKey (new KeyEvent (Key.A, new KeyModifiers { }));

			Assert.Equal ("--(____)--", field.Text);
			Assert.False (field.IsValid);
		}

		[Fact]
		public void Home_Key_First_Editable_Character ()
		{
			//                                                            *
			//                                                         0123456789
			var field = new TextValidateField<NetMaskedTextProvider> ("--(0000)--") {
				TextAlignment = TextAlignment.Centered,
				Width = 20
			};

			field.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers { }));
			field.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers { }));
			field.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers { }));

			field.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers { }));

			Assert.Equal ("--(1___)--", field.Text);
			Assert.False (field.IsValid);
		}

		[Fact]
		public void End_Key_Last_Editable_Character ()
		{
			//                                                               *
			//                                                         0123456789
			var field = new TextValidateField<NetMaskedTextProvider> ("--(0000)--") {
				TextAlignment = TextAlignment.Centered,
				Width = 20
			};

			field.ProcessKey (new KeyEvent (Key.End, new KeyModifiers { }));

			field.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers { }));
			Assert.Equal ("--(___1)--", field.Text);
			Assert.False (field.IsValid);
		}

		[Fact]
		public void Right_Key_Stops_In_Last_Editable_Character ()
		{
			//                                                               *
			//                                                         0123456789
			var field = new TextValidateField<NetMaskedTextProvider> ("--(0000)--") {
				TextAlignment = TextAlignment.Centered,
				Width = 20
			};

			for (int i = 0; i < 10; i++) {
				field.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers { }));
			}
			field.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers { }));

			Assert.Equal ("--(___1)--", field.Text);
			Assert.False (field.IsValid);
		}

		[Fact]
		public void Left_Key_Stops_In_First_Editable_Character ()
		{
			//                                                            *
			//                                                         0123456789
			var field = new TextValidateField<NetMaskedTextProvider> ("--(0000)--") {
				TextAlignment = TextAlignment.Centered,
				Width = 20
			};

			for (int i = 0; i < 10; i++) {
				field.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers { }));
			}
			field.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers { }));

			Assert.Equal ("--(1___)--", field.Text);
			Assert.False (field.IsValid);
		}

		[Fact]
		public void When_Valid_Is_Valid_True ()
		{
			//                                                            ****
			//                                                         0123456789
			var field = new TextValidateField<NetMaskedTextProvider> ("--(0000)--") {
				TextAlignment = TextAlignment.Centered,
				Width = 20
			};

			field.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers { }));
			Assert.Equal ("--(1___)--", field.Text);
			Assert.False (field.IsValid);

			field.ProcessKey (new KeyEvent (Key.D2, new KeyModifiers { }));
			Assert.Equal ("--(12__)--", field.Text);
			Assert.False (field.IsValid);

			field.ProcessKey (new KeyEvent (Key.D3, new KeyModifiers { }));
			Assert.Equal ("--(123_)--", field.Text);
			Assert.False (field.IsValid);

			field.ProcessKey (new KeyEvent (Key.D4, new KeyModifiers { }));
			Assert.Equal ("--(1234)--", field.Text);
			Assert.True (field.IsValid);
		}

		[Fact]
		public void Insert_Skips_Non_Editable_Characters ()
		{
			//                                                            ** **
			//                                                         01234567890
			var field = new TextValidateField<NetMaskedTextProvider> ("--(00-00)--") {
				TextAlignment = TextAlignment.Centered,
				Width = 20
			};

			field.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers { }));
			Assert.Equal ("--(1_-__)--", field.Text);
			Assert.False (field.IsValid);

			field.ProcessKey (new KeyEvent (Key.D2, new KeyModifiers { }));
			Assert.Equal ("--(12-__)--", field.Text);
			Assert.False (field.IsValid);

			field.ProcessKey (new KeyEvent (Key.D3, new KeyModifiers { }));
			Assert.Equal ("--(12-3_)--", field.Text);
			Assert.False (field.IsValid);

			field.ProcessKey (new KeyEvent (Key.D4, new KeyModifiers { }));
			Assert.Equal ("--(12-34)--", field.Text);
			Assert.True (field.IsValid);
		}


		[Fact]
		public void Initial_Value_Exact_Valid ()
		{
			//                                                            ****
			//                                                         0123456789
			var field = new TextValidateField<NetMaskedTextProvider> ("--(0000)--", "1234") {
				TextAlignment = TextAlignment.Centered,
				Width = 20
			};

			Assert.Equal ("--(1234)--", field.Text);
			Assert.True (field.IsValid);
		}

		[Fact]
		public void Initial_Value_Bigger_Than_Mask_Discarded ()
		{
			//                                                            ****
			//                                                         0123456789
			var field = new TextValidateField<NetMaskedTextProvider> ("--(0000)--", "12345") {
				TextAlignment = TextAlignment.Centered,
				Width = 20
			};

			Assert.Equal ("--(____)--", field.Text);
			Assert.False (field.IsValid);
		}

		[Fact]
		public void Initial_Value_Smaller_Than_Mask_Accepted ()
		{
			//                                                            ****
			//                                                         0123456789
			var field = new TextValidateField<NetMaskedTextProvider> ("--(0000)--", "123") {
				TextAlignment = TextAlignment.Centered,
				Width = 20
			};

			Assert.Equal ("--(123_)--", field.Text);
			Assert.False (field.IsValid);
		}

		[Fact]
		public void Delete_Key_Dosent_Move_Cursor ()
		{
			//                                                            ****
			//                                                         0123456789
			var field = new TextValidateField<NetMaskedTextProvider> ("--(0000)--", "1234") {
				TextAlignment = TextAlignment.Centered,
				Width = 20
			};

			Assert.Equal ("--(1234)--", field.Text);
			Assert.True (field.IsValid);

			field.ProcessKey (new KeyEvent (Key.Delete, new KeyModifiers { }));
			field.ProcessKey (new KeyEvent (Key.Delete, new KeyModifiers { }));
			field.ProcessKey (new KeyEvent (Key.Delete, new KeyModifiers { }));

			Assert.Equal ("--(_234)--", field.Text);
			Assert.False (field.IsValid);

			field.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers { }));
			field.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers { }));

			field.ProcessKey (new KeyEvent (Key.Delete, new KeyModifiers { }));
			field.ProcessKey (new KeyEvent (Key.Delete, new KeyModifiers { }));
			field.ProcessKey (new KeyEvent (Key.Delete, new KeyModifiers { }));

			Assert.Equal ("--(_2_4)--", field.Text);
			Assert.False (field.IsValid);
		}

		[Fact]
		public void Backspace_Key_Deletes_Previous_Character ()
		{
			//                                                            ****
			//                                                         0123456789
			var field = new TextValidateField<NetMaskedTextProvider> ("--(0000)--", "1234") {
				TextAlignment = TextAlignment.Centered,
				Width = 20
			};

			// Go to the end.
			field.ProcessKey (new KeyEvent (Key.End, new KeyModifiers { }));

			field.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers { }));
			Assert.Equal ("--(12_4)--", field.Text);
			Assert.False (field.IsValid);

			field.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers { }));
			Assert.Equal ("--(1__4)--", field.Text);
			Assert.False (field.IsValid);

			field.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers { }));
			Assert.Equal ("--(___4)--", field.Text);
			Assert.False (field.IsValid);

			// One more
			field.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers { }));
			Assert.Equal ("--(___4)--", field.Text);
			Assert.False (field.IsValid);
		}


		[Fact]
		public void Set_Text_After_Initialization ()
		{
			//                                                            ****
			//                                                         0123456789
			var field = new TextValidateField<NetMaskedTextProvider> ("--(0000)--") {
				TextAlignment = TextAlignment.Left,
				Width = 30
			};

			field.Text = "1234";

			Assert.Equal ("--(1234)--", field.Text);
			Assert.True (field.IsValid);
		}

		[Fact]
		public void Changing_The_Mask_Tries_To_Keep_The_Previous_Text ()
		{
			//                                                            ****
			//                                                         0123456789
			var field = new TextValidateField<NetMaskedTextProvider> ("--(0000)--") {
				TextAlignment = TextAlignment.Left,
				Width = 30
			};

			field.Text = "1234";
			Assert.Equal ("--(1234)--", field.Text);
			Assert.True (field.IsValid);

			field.Mask = "--------(00000000)--------";
			Assert.Equal ("--------(1234____)--------", field.Text);
			Assert.False (field.IsValid);
		}

		[Fact]
		public void MouseClick_Right_X_Greater_Than_Text_Width_Goes_To_Last_Editable_Position ()
		{
			//                                                            ****
			//                                                         0123456789
			var field = new TextValidateField<NetMaskedTextProvider> ("--(0000)--") {
				TextAlignment = TextAlignment.Left,
				Width = 30
			};

			field.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers { }));

			Assert.Equal ("--(1___)--", field.Text);
			Assert.False (field.IsValid);

			field.MouseEvent (new MouseEvent () { X = 25, Flags = MouseFlags.Button1Clicked });

			field.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers { }));

			Assert.Equal ("--(1__1)--", field.Text);
			Assert.False (field.IsValid);
		}
	}

	public class TextValidateField_Regex_Provider_Tests {
		public TextValidateField_Regex_Provider_Tests ()
		{
			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));
		}

		[Fact]
		public void Input_Without_Validate_On_Input ()
		{
			var field = new TextValidateField<TextRegexProvider> ("^[0-9][0-9][0-9]$") {
				Width = 20
			};

			// Let you input
			field.Provider.ValidateOnInput = false;

			field.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers { }));
			Assert.Equal ("1", field.Text);
			Assert.False (field.IsValid);

			field.ProcessKey (new KeyEvent (Key.D2, new KeyModifiers { }));
			Assert.Equal ("12", field.Text);
			Assert.False (field.IsValid);

			field.ProcessKey (new KeyEvent (Key.D3, new KeyModifiers { }));
			Assert.Equal ("123", field.Text);
			Assert.True (field.IsValid);

			field.ProcessKey (new KeyEvent (Key.D4, new KeyModifiers { }));
			Assert.Equal ("1234", field.Text);
			Assert.False (field.IsValid);
		}

		[Fact]
		public void Input_With_Validate_On_Input_Set_Text ()
		{
			var field = new TextValidateField<TextRegexProvider> ("^[0-9][0-9][0-9]$") {
				Width = 20
			};

			// Input dosen't validates the pattern.
			field.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers { }));
			Assert.Equal ("", field.Text);
			Assert.False (field.IsValid);

			// Dosen't match
			field.Text = "12356";
			Assert.Equal ("", field.Text);
			Assert.False (field.IsValid);

			// Yes.
			field.Text = "123";
			Assert.Equal ("123", field.Text);
			Assert.True (field.IsValid);
		}

		[Fact]
		public void Empty_Mask_Validates_Everything ()
		{
			// Maybe it's not the right behaviour.

			var field = new TextValidateField<TextRegexProvider> () {
				Width = 20
			};

			field.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers { }));
			Assert.Equal ("1", field.Text);
			Assert.True (field.IsValid);
		}

		[Fact]
		public void Text_With_All_Charset ()
		{
			var field = new TextValidateField<TextRegexProvider> ("^[0-9][0-9][0-9]$") {
				Width = 20
			};

			var text = "";
			for (int i = 0; i < 255; i++) {
				text += (char)i;
			}

			field.Text = text;

			Assert.False (field.IsValid);
		}

		[Fact]
		public void Mask_With_Invalid_Pattern_Exception ()
		{
			// Regex Exception
			// Maybe it's not the right behaviour.

			var mask = "";
			for (int i = 0; i < 255; i++) {
				mask += (char)i;
			}

			try {
				var field = new TextValidateField<TextRegexProvider> (mask) {
					Width = 20
				};
			} catch (RegexParseException ex) {
				Assert.True (true);
				return;
			}
			Assert.True (false);
		}

		[Fact]
		public void Home_Key_First_Editable_Character ()
		{
			// Range 0 to 1000
			// Accepts 001 too.
			var field = new TextValidateField<TextRegexProvider> ("^[0-9]?[0-9]?[0-9]|1000$") {
				Width = 20
			};

			field.ProcessKey (new KeyEvent (Key.D1, new KeyModifiers { }));
			field.ProcessKey (new KeyEvent (Key.D0, new KeyModifiers { }));
			field.ProcessKey (new KeyEvent (Key.D0, new KeyModifiers { }));
			field.ProcessKey (new KeyEvent (Key.D0, new KeyModifiers { }));

			Assert.Equal ("1000", field.Text);
			Assert.True (field.IsValid);

			// HOME KEY
			field.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers { }));

			// DELETE
			field.ProcessKey (new KeyEvent (Key.Delete, new KeyModifiers { }));

			Assert.Equal ("000", field.Text);
			Assert.True (field.IsValid);
		}

		[Fact]
		public void End_Key_End_Of_Input ()
		{
			// Exactly 5 numbers
			var field = new TextValidateField<TextRegexProvider> ("^[0-9]{5}$") {
				Width = 20
			};

			field.Provider.ValidateOnInput = false;

			for (int i = 0; i < 4; i++) {
				field.ProcessKey (new KeyEvent (Key.D0, new KeyModifiers { }));
			}

			Assert.Equal ("0000", field.Text);
			Assert.False (field.IsValid);

			// HOME KEY
			field.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers { }));

			// END KEY
			field.ProcessKey (new KeyEvent (Key.End, new KeyModifiers { }));

			// Insert 9
			field.ProcessKey (new KeyEvent (Key.D9, new KeyModifiers { }));

			Assert.Equal ("00009", field.Text);
			Assert.True (field.IsValid);

			// Insert 9
			field.ProcessKey (new KeyEvent (Key.D9, new KeyModifiers { }));

			Assert.Equal ("000099", field.Text);
			Assert.False (field.IsValid);
		}

		[Fact]
		public void Right_Key_Stops_At_End_And_Insert ()
		{
			var field = new TextValidateField<TextRegexProvider> ("^[0-9][0-9][0-9]$") {
				TextAlignment = TextAlignment.Centered,
				Width = 20
			};
			field.Provider.ValidateOnInput = false;

			field.Text = "123";

			for (int i = 0; i < 10; i++) {
				field.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers { }));
			}

			Assert.Equal ("123", field.Text);
			Assert.True (field.IsValid);

			// Insert 4
			field.ProcessKey (new KeyEvent (Key.D4, new KeyModifiers { }));

			Assert.Equal ("1234", field.Text);
			Assert.False (field.IsValid);
		}

		[Fact]
		public void Left_Key_Stops_At_Start_And_Insert ()
		{
			var field = new TextValidateField<TextRegexProvider> ("^[0-9][0-9][0-9]$") {
				TextAlignment = TextAlignment.Centered,
				Width = 20
			};
			field.Provider.ValidateOnInput = false;

			field.Text = "123";

			for (int i = 0; i < 10; i++) {
				field.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers { }));
			}

			Assert.Equal ("123", field.Text);
			Assert.True (field.IsValid);

			// Insert 4
			field.ProcessKey (new KeyEvent (Key.D4, new KeyModifiers { }));

			Assert.Equal ("4123", field.Text);
			Assert.False (field.IsValid);
		}
	}
}