using System.Text.RegularExpressions;
using Terminal.Gui.TextValidateProviders;
using Xunit;

namespace Terminal.Gui.ViewsTests;

// TODO: These tests should not rely on AutoInitShutdown / Application

public class TextValidateField_NET_Provider_Tests {

	[Fact]
	[AutoInitShutdown]
	public void Initialized_With_Cursor_On_First_Editable_Character ()
	{
		//                                                            *
		//                                                         0123456789
		var field = new TextValidateField (new NetMaskedTextProvider ("--(0000)--")) {
			TextAlignment = TextAlignment.Centered,
			Width = 20
		};

		field.NewKeyDownEvent (new Key (KeyCode.D1));

		Assert.Equal ("--(1___)--", field.Provider.DisplayText);
		Assert.Equal ("--(1   )--", field.Text);
	}

	[Fact]
	[AutoInitShutdown]
	public void Input_Ilegal_Character ()
	{
		//                                                            *
		//                                                         0123456789
		var field = new TextValidateField (new NetMaskedTextProvider ("--(0000)--")) {
			TextAlignment = TextAlignment.Centered,
			Width = 20
		};

		field.NewKeyDownEvent (new Key (KeyCode.A));

		Assert.Equal ("--(    )--", field.Text);
		Assert.Equal ("--(____)--", field.Provider.DisplayText);
		Assert.False (field.IsValid);
	}

	[Fact]
	[AutoInitShutdown]
	public void Home_Key_First_Editable_Character ()
	{
		//                                                            *
		//                                                         0123456789
		var field = new TextValidateField (new NetMaskedTextProvider ("--(0000)--")) {
			TextAlignment = TextAlignment.Centered,
			Width = 20
		};

		field.NewKeyDownEvent (new Key (KeyCode.CursorRight));
		field.NewKeyDownEvent (new Key (KeyCode.CursorRight));
		field.NewKeyDownEvent (new Key (KeyCode.Home));

		field.NewKeyDownEvent (new Key (KeyCode.D1));

		Assert.Equal ("--(1___)--", field.Provider.DisplayText);
		Assert.Equal ("--(1   )--", field.Text);
		Assert.False (field.IsValid);
	}

	[Fact]
	[AutoInitShutdown]
	public void End_Key_Last_Editable_Character ()
	{
		//                                                               *
		//                                                         0123456789
		var field = new TextValidateField (new NetMaskedTextProvider ("--(0000)--")) {
			TextAlignment = TextAlignment.Centered,
			Width = 20
		};

		field.NewKeyDownEvent (new Key (KeyCode.End));

		field.NewKeyDownEvent (new Key (KeyCode.D1));

		Assert.Equal ("--(___1)--", field.Provider.DisplayText);
		Assert.Equal ("--(   1)--", field.Text);
		Assert.False (field.IsValid);
	}

	[Fact]
	[AutoInitShutdown]
	public void Right_Key_Stops_In_Last_Editable_Character ()
	{
		//                                                               *
		//                                                         0123456789
		var field = new TextValidateField (new NetMaskedTextProvider ("--(0000)--")) {
			TextAlignment = TextAlignment.Centered,
			Width = 20
		};

		for (var i = 0; i < 10; i++) {
			field.NewKeyDownEvent (new Key (KeyCode.CursorRight));
		}
		field.NewKeyDownEvent (new Key (KeyCode.D1));

		Assert.Equal ("--(___1)--", field.Provider.DisplayText);
		Assert.Equal ("--(   1)--", field.Text);
		Assert.False (field.IsValid);
	}

	[Fact]
	[AutoInitShutdown]
	public void Left_Key_Stops_In_First_Editable_Character ()
	{
		//                                                            *
		//                                                         0123456789
		var field = new TextValidateField (new NetMaskedTextProvider ("--(0000)--")) {
			TextAlignment = TextAlignment.Centered,
			Width = 20
		};

		for (var i = 0; i < 10; i++) {
			field.NewKeyDownEvent (new Key (KeyCode.CursorLeft));
		}
		field.NewKeyDownEvent (new Key (KeyCode.D1));

		Assert.Equal ("--(1___)--", field.Provider.DisplayText);
		Assert.Equal ("--(1   )--", field.Text);
		Assert.False (field.IsValid);
	}

	[Fact]
	[AutoInitShutdown]
	public void When_Valid_Is_Valid_True ()
	{
		//                                                            ****
		//                                                         0123456789
		var field = new TextValidateField (new NetMaskedTextProvider ("--(0000)--")) {
			TextAlignment = TextAlignment.Centered,
			Width = 20
		};

		field.NewKeyDownEvent (new Key (KeyCode.D1));
		Assert.Equal ("--(1   )--", field.Text);
		Assert.False (field.IsValid);

		field.NewKeyDownEvent (new Key (KeyCode.D2));
		Assert.Equal ("--(12  )--", field.Text);
		Assert.False (field.IsValid);

		field.NewKeyDownEvent (new Key (KeyCode.D3));
		Assert.Equal ("--(123 )--", field.Text);
		Assert.False (field.IsValid);

		field.NewKeyDownEvent (new Key (KeyCode.D4));
		Assert.Equal ("--(1234)--", field.Text);
		Assert.True (field.IsValid);
	}

	[Fact]
	[AutoInitShutdown]
	public void Insert_Skips_Non_Editable_Characters ()
	{
		//                                                            ** **
		//                                                         01234567890
		var field = new TextValidateField (new NetMaskedTextProvider ("--(00-00)--")) {
			TextAlignment = TextAlignment.Centered,
			Width = 20
		};

		field.NewKeyDownEvent (new Key (KeyCode.D1));
		Assert.Equal ("--(1_-__)--", field.Provider.DisplayText);
		Assert.False (field.IsValid);

		field.NewKeyDownEvent (new Key (KeyCode.D2));
		Assert.Equal ("--(12-__)--", field.Provider.DisplayText);
		Assert.False (field.IsValid);

		field.NewKeyDownEvent (new Key (KeyCode.D3));
		Assert.Equal ("--(12-3_)--", field.Provider.DisplayText);
		Assert.False (field.IsValid);

		field.NewKeyDownEvent (new Key (KeyCode.D4));
		Assert.Equal ("--(12-34)--", field.Provider.DisplayText);
		Assert.True (field.IsValid);
	}

	[Fact]
	[AutoInitShutdown]
	public void Initial_Value_Exact_Valid ()
	{
		//                                                            ****
		//                                                         0123456789
		var field = new TextValidateField (new NetMaskedTextProvider ("--(0000)--") {
			Text = "1234"
		}) {
			TextAlignment = TextAlignment.Centered,
			Width = 20
		};

		Assert.Equal ("--(1234)--", field.Text);
		Assert.True (field.IsValid);
	}

	[Fact]
	[AutoInitShutdown]
	public void Initial_Value_Bigger_Than_Mask_Discarded ()
	{
		//                                                            ****
		//                                                         0123456789
		var field = new TextValidateField (new NetMaskedTextProvider ("--(0000)--") {
			Text = "12345"
		}) {
			TextAlignment = TextAlignment.Centered,
			Width = 20
		};

		Assert.Equal ("--(____)--", field.Provider.DisplayText);
		Assert.Equal ("--(    )--", field.Text);
		Assert.False (field.IsValid);
	}

	[Fact]
	[AutoInitShutdown]
	public void Initial_Value_Smaller_Than_Mask_Accepted ()
	{
		//                                                            ****
		//                                                         0123456789
		var field = new TextValidateField (new NetMaskedTextProvider ("--(0000)--") {
			Text = "123"
		}) {
			TextAlignment = TextAlignment.Centered,
			Width = 20
		};

		Assert.Equal ("--(123_)--", field.Provider.DisplayText);
		Assert.Equal ("--(123 )--", field.Text);
		Assert.False (field.IsValid);
	}

	[Fact]
	[AutoInitShutdown]
	public void Delete_Key_Doesnt_Move_Cursor ()
	{
		//                                                            ****
		//                                                         0123456789
		var field = new TextValidateField (new NetMaskedTextProvider ("--(0000)--") {
			Text = "1234"
		}) {
			TextAlignment = TextAlignment.Centered,
			Width = 20
		};

		Assert.Equal ("--(1234)--", field.Provider.DisplayText);
		Assert.True (field.IsValid);

		field.NewKeyDownEvent (new Key (KeyCode.Delete));
		field.NewKeyDownEvent (new Key (KeyCode.Delete));
		field.NewKeyDownEvent (new Key (KeyCode.Delete));

		Assert.Equal ("--(_234)--", field.Provider.DisplayText);
		Assert.False (field.IsValid);

		field.NewKeyDownEvent (new Key (KeyCode.CursorRight));
		field.NewKeyDownEvent (new Key (KeyCode.CursorRight));

		field.NewKeyDownEvent (new Key (KeyCode.Delete));
		field.NewKeyDownEvent (new Key (KeyCode.Delete));
		field.NewKeyDownEvent (new Key (KeyCode.Delete));

		Assert.Equal ("--(_2_4)--", field.Provider.DisplayText);
		Assert.False (field.IsValid);
	}

	[Fact]
	[AutoInitShutdown]
	public void Backspace_Key_Deletes_Previous_Character ()
	{
		//                                                            ****
		//                                                         0123456789
		var field = new TextValidateField (new NetMaskedTextProvider ("--(0000)--") {
			Text = "1234"
		}) {
			TextAlignment = TextAlignment.Centered,
			Width = 20
		};

		// Go to the end.
		field.NewKeyDownEvent (new Key (KeyCode.End));

		field.NewKeyDownEvent (new Key (KeyCode.Backspace));
		Assert.Equal ("--(12_4)--", field.Provider.DisplayText);
		Assert.False (field.IsValid);

		field.NewKeyDownEvent (new Key (KeyCode.Backspace));
		Assert.Equal ("--(1__4)--", field.Provider.DisplayText);
		Assert.False (field.IsValid);

		field.NewKeyDownEvent (new Key (KeyCode.Backspace));
		Assert.Equal ("--(___4)--", field.Provider.DisplayText);
		Assert.False (field.IsValid);

		// One more
		field.NewKeyDownEvent (new Key (KeyCode.Backspace));
		Assert.Equal ("--(___4)--", field.Provider.DisplayText);
		Assert.False (field.IsValid);
	}

	[Fact]
	[AutoInitShutdown]
	public void Set_Text_After_Initialization ()
	{
		//                                                            ****
		//                                                         0123456789
		var field = new TextValidateField (new NetMaskedTextProvider ("--(0000)--")) {
			TextAlignment = TextAlignment.Left,
			Width = 30
		};

		field.Text = "1234";

		Assert.Equal ("--(1234)--", field.Text);
		Assert.True (field.IsValid);
	}

	[Fact]
	[AutoInitShutdown]
	public void Changing_The_Mask_Tries_To_Keep_The_Previous_Text ()
	{
		//                                                            ****
		//                                                         0123456789
		var field = new TextValidateField (new NetMaskedTextProvider ("--(0000)--")) {
			TextAlignment = TextAlignment.Left,
			Width = 30
		};

		field.Text = "1234";
		Assert.Equal ("--(1234)--", field.Text);
		Assert.True (field.IsValid);

		var provider = field.Provider as NetMaskedTextProvider;
		provider.Mask = "--------(00000000)--------";
		Assert.Equal ("--------(1234____)--------", field.Provider.DisplayText);
		Assert.False (field.IsValid);
	}

	[Fact]
	[AutoInitShutdown]
	public void MouseClick_Right_X_Greater_Than_Text_Width_Goes_To_Last_Editable_Position ()
	{
		//                                                            ****
		//                                                         0123456789
		var field = new TextValidateField (new NetMaskedTextProvider ("--(0000)--")) {
			TextAlignment = TextAlignment.Left,
			Width = 30
		};

		field.NewKeyDownEvent (new Key (KeyCode.D1));

		Assert.Equal ("--(1___)--", field.Provider.DisplayText);
		Assert.False (field.IsValid);

		field.MouseEvent (new MouseEvent { X = 25, Flags = MouseFlags.Button1Pressed });

		field.NewKeyDownEvent (new Key (KeyCode.D1));

		Assert.Equal ("--(1__1)--", field.Provider.DisplayText);
		Assert.False (field.IsValid);
	}
}

public class TextValidateField_Regex_Provider_Tests {

	[Fact]
	[AutoInitShutdown]
	public void Input_Without_Validate_On_Input ()
	{
		var field = new TextValidateField (new TextRegexProvider ("^[0-9][0-9][0-9]$") {
			ValidateOnInput = false
		}) {
			Width = 20
		};

		field.NewKeyDownEvent (new Key (KeyCode.D1));
		Assert.Equal ("1", field.Text);
		Assert.False (field.IsValid);

		field.NewKeyDownEvent (new Key (KeyCode.D2));
		Assert.Equal ("12", field.Text);
		Assert.False (field.IsValid);

		field.NewKeyDownEvent (new Key (KeyCode.D3));
		Assert.Equal ("123", field.Text);
		Assert.True (field.IsValid);

		field.NewKeyDownEvent (new Key (KeyCode.D4));
		Assert.Equal ("1234", field.Text);
		Assert.False (field.IsValid);
	}

	[Fact]
	[AutoInitShutdown]
	public void Input_With_Validate_On_Input_Set_Text ()
	{
		var field = new TextValidateField (new TextRegexProvider ("^[0-9][0-9][0-9]$")) {
			Width = 20
		};

		// Input dosen't validates the pattern.
		field.NewKeyDownEvent (new Key (KeyCode.D1));
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
	[AutoInitShutdown]
	public void Text_With_All_Charset ()
	{
		var field = new TextValidateField (new TextRegexProvider ("^[0-9][0-9][0-9]$")) {
			Width = 20
		};

		var text = "";
		for (var i = 0; i < 255; i++) {
			text += (char)i;
		}

		field.Text = text;

		Assert.False (field.IsValid);
	}

	[Fact]
	[AutoInitShutdown]
	public void Mask_With_Invalid_Pattern_Exception ()
	{
		// Regex Exception
		// Maybe it's not the right behaviour.

		var mask = "";
		for (var i = 0; i < 255; i++) {
			mask += (char)i;
		}

		try {
			var field = new TextValidateField (new TextRegexProvider (mask)) {
				Width = 20
			};
		} catch (RegexParseException ex) {
			Assert.True (true, ex.Message);
			return;
		}
		Assert.True (false);
	}

	[Fact]
	[AutoInitShutdown]
	public void Home_Key_First_Editable_Character ()
	{
		// Range 0 to 1000
		// Accepts 001 too.
		var field = new TextValidateField (new TextRegexProvider ("^[0-9]?[0-9]?[0-9]|1000$")) {
			Width = 20
		};

		field.NewKeyDownEvent (new Key (KeyCode.D1));
		field.NewKeyDownEvent (new Key (KeyCode.D0));
		field.NewKeyDownEvent (new Key (KeyCode.D0));
		field.NewKeyDownEvent (new Key (KeyCode.D0));

		Assert.Equal ("1000", field.Text);
		Assert.True (field.IsValid);

		// HOME KEY
		field.NewKeyDownEvent (new Key (KeyCode.Home));

		// DELETE
		field.NewKeyDownEvent (new Key (KeyCode.Delete));

		Assert.Equal ("000", field.Text);
		Assert.True (field.IsValid);
	}

	[Fact]
	[AutoInitShutdown]
	public void End_Key_End_Of_Input ()
	{
		// Exactly 5 numbers
		var field = new TextValidateField (new TextRegexProvider ("^[0-9]{5}$") {
			ValidateOnInput = false
		}) {
			Width = 20
		};

		for (var i = 0; i < 4; i++) {
			field.NewKeyDownEvent (new Key (KeyCode.D0));
		}

		Assert.Equal ("0000", field.Text);
		Assert.False (field.IsValid);

		// HOME KEY
		field.NewKeyDownEvent (new Key (KeyCode.Home));

		// END KEY
		field.NewKeyDownEvent (new Key (KeyCode.End));

		// Insert 9
		field.NewKeyDownEvent (new Key (KeyCode.D9));

		Assert.Equal ("00009", field.Text);
		Assert.True (field.IsValid);

		// Insert 9
		field.NewKeyDownEvent (new Key (KeyCode.D9));

		Assert.Equal ("000099", field.Text);
		Assert.False (field.IsValid);
	}

	[Fact]
	[AutoInitShutdown]
	public void Right_Key_Stops_At_End_And_Insert ()
	{
		var field = new TextValidateField (new TextRegexProvider ("^[0-9][0-9][0-9]$") {
			ValidateOnInput = false
		}) {
			TextAlignment = TextAlignment.Centered,
			Width = 20
		};

		field.Text = "123";

		for (var i = 0; i < 10; i++) {
			field.NewKeyDownEvent (new Key (KeyCode.CursorRight));
		}

		Assert.Equal ("123", field.Text);
		Assert.True (field.IsValid);

		// Insert 4
		field.NewKeyDownEvent (new Key (KeyCode.D4));

		Assert.Equal ("1234", field.Text);
		Assert.False (field.IsValid);
	}

	[Fact]
	[AutoInitShutdown]
	public void Left_Key_Stops_At_Start_And_Insert ()
	{
		var field = new TextValidateField (new TextRegexProvider ("^[0-9][0-9][0-9]$") {
			ValidateOnInput = false
		}) {
			TextAlignment = TextAlignment.Centered,
			Width = 20
		};

		field.Text = "123";

		for (var i = 0; i < 10; i++) {
			field.NewKeyDownEvent (new Key (KeyCode.CursorLeft));
		}

		Assert.Equal ("123", field.Text);
		Assert.True (field.IsValid);

		// Insert 4
		field.NewKeyDownEvent (new Key (KeyCode.D4));

		Assert.Equal ("4123", field.Text);
		Assert.False (field.IsValid);
	}
}