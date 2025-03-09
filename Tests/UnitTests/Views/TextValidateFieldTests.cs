using System.Text.RegularExpressions;
using Terminal.Gui.TextValidateProviders;

namespace Terminal.Gui.ViewsTests;

public class TextValidateField_NET_Provider_Tests
{
    [Fact]
    public void Backspace_Key_Deletes_Previous_Character ()
    {
        var field = new TextValidateField
        {
            TextAlignment = Alignment.Center,
            Width = 20,

            //                                             ****
            //                                          0123456789
            Provider = new NetMaskedTextProvider ("--(0000)--") { Text = "1234" }
        };

        // Go to the end.
        field.NewKeyDownEvent (Key.End);

        field.NewKeyDownEvent (Key.Backspace);
        Assert.Equal ("--(12_4)--", field.Provider.DisplayText);
        Assert.False (field.IsValid);

        field.NewKeyDownEvent (Key.Backspace);
        Assert.Equal ("--(1__4)--", field.Provider.DisplayText);
        Assert.False (field.IsValid);

        field.NewKeyDownEvent (Key.Backspace);
        Assert.Equal ("--(___4)--", field.Provider.DisplayText);
        Assert.False (field.IsValid);

        // One more
        field.NewKeyDownEvent (Key.Backspace);
        Assert.Equal ("--(___4)--", field.Provider.DisplayText);
        Assert.False (field.IsValid);
    }

    [Fact]
    public void Changing_The_Mask_Tries_To_Keep_The_Previous_Text ()
    {
        var field = new TextValidateField
        {
            TextAlignment = Alignment.Start,
            Width = 30,

            //                                             ****
            //                                          0123456789
            Provider = new NetMaskedTextProvider ("--(0000)--")
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
    public void Default_Width_Is_Always_Equal_To_The_Provider_DisplayText_Length ()
    {
        // 9-Digit or space, optional. 0-Digit, required. L-Letter, required.
        // > Shift up. Converts all characters that follow to uppercase.
        // | Disable a previous shift up or shift down.
        // A-Alphanumeric, required. a-Alphanumeric, optional.
        var field = new TextValidateField { Provider = new NetMaskedTextProvider ("999 000 LLL >LLL |AAA aaa") };
        field.Layout ();
        Assert.Equal (field.Viewport.Width, field.Provider.DisplayText.Length);
        Assert.NotEqual (field.Provider.DisplayText.Length, field.Provider.Text.Length);
        Assert.Equal (new string (' ', field.Text.Length), field.Provider.Text);
    }

    [Fact]
    public void Delete_Key_Doesnt_Move_Cursor ()
    {
        var field = new TextValidateField
        {
            TextAlignment = Alignment.Center,
            Width = 20,

            //                                             ****
            //                                          0123456789
            Provider = new NetMaskedTextProvider ("--(0000)--") { Text = "1234" }
        };

        Assert.Equal ("--(1234)--", field.Provider.DisplayText);
        Assert.True (field.IsValid);

        field.NewKeyDownEvent (Key.Delete);
        field.NewKeyDownEvent (Key.Delete);
        field.NewKeyDownEvent (Key.Delete);

        Assert.Equal ("--(_234)--", field.Provider.DisplayText);
        Assert.False (field.IsValid);

        field.NewKeyDownEvent (Key.CursorRight);
        field.NewKeyDownEvent (Key.CursorRight);

        field.NewKeyDownEvent (Key.Delete);
        field.NewKeyDownEvent (Key.Delete);
        field.NewKeyDownEvent (Key.Delete);

        Assert.Equal ("--(_2_4)--", field.Provider.DisplayText);
        Assert.False (field.IsValid);
    }

    [Fact]
    public void End_Key_Last_Editable_Character ()
    {
        var field = new TextValidateField
        {
            TextAlignment = Alignment.Center,
            Width = 20,

            //                                             *
            //                                          0123456789
            Provider = new NetMaskedTextProvider ("--(0000)--")
        };

        field.NewKeyDownEvent (Key.End);

        field.NewKeyDownEvent (Key.D1);

        Assert.Equal ("--(___1)--", field.Provider.DisplayText);
        Assert.Equal ("--(   1)--", field.Text);
        Assert.False (field.IsValid);
    }

    [Fact]
    public void Home_Key_First_Editable_Character ()
    {
        var field = new TextValidateField
        {
            TextAlignment = Alignment.Center,
            Width = 20,

            //                                             *
            //                                          0123456789
            Provider = new NetMaskedTextProvider ("--(0000)--")
        };

        field.NewKeyDownEvent (Key.CursorRight);
        field.NewKeyDownEvent (Key.CursorRight);
        field.NewKeyDownEvent (Key.Home);

        field.NewKeyDownEvent (Key.D1);

        Assert.Equal ("--(1___)--", field.Provider.DisplayText);
        Assert.Equal ("--(1   )--", field.Text);
        Assert.False (field.IsValid);
    }

    [Fact]
    public void Initial_Value_Bigger_Than_Mask_Discarded ()
    {
        var field = new TextValidateField
        {
            TextAlignment = Alignment.Center,
            Width = 20,

            //                                             ****
            //                                          0123456789
            Provider = new NetMaskedTextProvider ("--(0000)--") { Text = "12345" }
        };

        Assert.Equal ("--(____)--", field.Provider.DisplayText);
        Assert.Equal ("--(    )--", field.Text);
        Assert.False (field.IsValid);
    }

    [Fact]
    public void Initial_Value_Exact_Valid ()
    {
        var field = new TextValidateField
        {
            TextAlignment = Alignment.Center,
            Width = 20,

            //                                             ****
            //                                          0123456789
            Provider = new NetMaskedTextProvider ("--(0000)--") { Text = "1234" }
        };

        Assert.Equal ("--(1234)--", field.Text);
        Assert.True (field.IsValid);
    }

    [Fact]
    public void Initial_Value_Smaller_Than_Mask_Accepted ()
    {
        var field = new TextValidateField
        {
            TextAlignment = Alignment.Center,
            Width = 20,

            //                                             ****
            //                                          0123456789
            Provider = new NetMaskedTextProvider ("--(0000)--") { Text = "123" }
        };

        Assert.Equal ("--(123_)--", field.Provider.DisplayText);
        Assert.Equal ("--(123 )--", field.Text);
        Assert.False (field.IsValid);
    }

    [Fact]
    public void Initialized_With_Cursor_On_First_Editable_Character ()
    {
        var field = new TextValidateField
        {
            TextAlignment = Alignment.Center,
            Width = 20,

            //                                             *
            //                                          0123456789
            Provider = new NetMaskedTextProvider ("--(0000)--")
        };

        field.NewKeyDownEvent (Key.D1);

        Assert.Equal ("--(1___)--", field.Provider.DisplayText);
        Assert.Equal ("--(1   )--", field.Text);
    }

    [Fact]
    public void Input_Ilegal_Character ()
    {
        var field = new TextValidateField
        {
            TextAlignment = Alignment.Center,
            Width = 20,

            //                                             *
            //                                          0123456789
            Provider = new NetMaskedTextProvider ("--(0000)--")
        };

        field.NewKeyDownEvent (Key.A);

        Assert.Equal ("--(    )--", field.Text);
        Assert.Equal ("--(____)--", field.Provider.DisplayText);
        Assert.False (field.IsValid);
    }

    [Fact]
    public void Insert_Skips_Non_Editable_Characters ()
    {
        var field = new TextValidateField
        {
            TextAlignment = Alignment.Center,
            Width = 20,

            //                                             ** **
            //                                          01234567890
            Provider = new NetMaskedTextProvider ("--(00-00)--")
        };

        field.NewKeyDownEvent (Key.D1);
        Assert.Equal ("--(1_-__)--", field.Provider.DisplayText);
        Assert.False (field.IsValid);

        field.NewKeyDownEvent (Key.D2);
        Assert.Equal ("--(12-__)--", field.Provider.DisplayText);
        Assert.False (field.IsValid);

        field.NewKeyDownEvent (Key.D3);
        Assert.Equal ("--(12-3_)--", field.Provider.DisplayText);
        Assert.False (field.IsValid);

        field.NewKeyDownEvent (Key.D4);
        Assert.Equal ("--(12-34)--", field.Provider.DisplayText);
        Assert.True (field.IsValid);
    }

    [Fact]
    public void Left_Key_Stops_In_First_Editable_Character ()
    {
        var field = new TextValidateField
        {
            TextAlignment = Alignment.Center,
            Width = 20,

            //                                             *
            //                                          0123456789
            Provider = new NetMaskedTextProvider ("--(0000)--")
        };

        for (var i = 0; i < 10; i++)
        {
            field.NewKeyDownEvent (Key.CursorLeft);
        }

        field.NewKeyDownEvent (Key.D1);

        Assert.Equal ("--(1___)--", field.Provider.DisplayText);
        Assert.Equal ("--(1   )--", field.Text);
        Assert.False (field.IsValid);
    }

    [Fact]
    public void MouseClick_Right_X_Greater_Than_Text_Width_Goes_To_Last_Editable_Position ()
    {
        var field = new TextValidateField
        {
            TextAlignment = Alignment.Start,
            Width = 30,

            //                                             ****
            //                                          0123456789
            Provider = new NetMaskedTextProvider ("--(0000)--")
        };

        field.NewKeyDownEvent (Key.D1);

        Assert.Equal ("--(1___)--", field.Provider.DisplayText);
        Assert.False (field.IsValid);
        Assert.Equal ("--(1   )--", field.Provider.Text);

        field.NewMouseEvent (new MouseEventArgs { Position = new (25, 0), Flags = MouseFlags.Button1Pressed });

        field.NewKeyDownEvent (Key.D1);

        Assert.Equal ("--(1__1)--", field.Provider.DisplayText);
        Assert.False (field.IsValid);
        Assert.Equal ("--(1  1)--", field.Provider.Text);
    }

    [Fact]
    public void OnTextChanged_TextChanged_Event ()
    {
        var wasTextChanged = false;

        var field = new TextValidateField
        {
            TextAlignment = Alignment.Start, Width = 30, Provider = new NetMaskedTextProvider ("--(0000)--")
        };

        field.Provider.TextChanged += (sender, e) => wasTextChanged = true;

        field.NewKeyDownEvent (Key.D1);

        Assert.Equal ("--(1___)--", field.Provider.DisplayText);
        Assert.False (field.IsValid);
        Assert.Equal ("--(1   )--", field.Provider.Text);
        Assert.True (wasTextChanged);
    }

    [Fact]
    public void Right_Key_Stops_In_Last_Editable_Character ()
    {
        var field = new TextValidateField
        {
            TextAlignment = Alignment.Center,
            Width = 20,

            //                                             *
            //                                          0123456789
            Provider = new NetMaskedTextProvider ("--(0000)--")
        };

        for (var i = 0; i < 10; i++)
        {
            field.NewKeyDownEvent (Key.CursorRight);
        }

        field.NewKeyDownEvent (Key.D1);

        Assert.Equal ("--(___1)--", field.Provider.DisplayText);
        Assert.Equal ("--(   1)--", field.Text);
        Assert.False (field.IsValid);
    }

    [Fact]
    public void Set_Text_After_Initialization ()
    {
        var field = new TextValidateField
        {
            TextAlignment = Alignment.Start,
            Width = 30,

            //                                             ****
            //                                          0123456789
            Provider = new NetMaskedTextProvider ("--(0000)--")
        };

        field.Text = "1234";

        Assert.Equal ("--(1234)--", field.Text);
        Assert.True (field.IsValid);
    }

    [Fact]
    public void When_Valid_Is_Valid_True ()
    {
        var field = new TextValidateField
        {
            TextAlignment = Alignment.Center,
            Width = 20,

            //                                             ****
            //                                          0123456789
            Provider = new NetMaskedTextProvider ("--(0000)--")
        };

        field.NewKeyDownEvent (Key.D1);
        Assert.Equal ("--(1   )--", field.Text);
        Assert.False (field.IsValid);

        field.NewKeyDownEvent (Key.D2);
        Assert.Equal ("--(12  )--", field.Text);
        Assert.False (field.IsValid);

        field.NewKeyDownEvent (Key.D3);
        Assert.Equal ("--(123 )--", field.Text);
        Assert.False (field.IsValid);

        field.NewKeyDownEvent (Key.D4);
        Assert.Equal ("--(1234)--", field.Text);
        Assert.True (field.IsValid);
    }
}

public class TextValidateField_Regex_Provider_Tests
{
    [Fact]
    public void End_Key_End_Of_Input ()
    {
        // Exactly 5 numbers
        var field = new TextValidateField
        {
            Width = 20, Provider = new TextRegexProvider ("^[0-9]{5}$") { ValidateOnInput = false }
        };

        for (var i = 0; i < 4; i++)
        {
            field.NewKeyDownEvent (Key.D0);
        }

        Assert.Equal ("0000", field.Text);
        Assert.False (field.IsValid);

        // HOME KEY
        field.NewKeyDownEvent (Key.Home);

        // END KEY
        field.NewKeyDownEvent (Key.End);

        // Insert 9
        field.NewKeyDownEvent (Key.D9);

        Assert.Equal ("00009", field.Text);
        Assert.True (field.IsValid);

        // Insert 9
        field.NewKeyDownEvent (Key.D9);

        Assert.Equal ("000099", field.Text);
        Assert.False (field.IsValid);
    }

    [Fact]
    public void Home_Key_First_Editable_Character ()
    {
        // Range 0 to 1000
        // Accepts 001 too.
        var field = new TextValidateField { Width = 20, Provider = new TextRegexProvider ("^[0-9]?[0-9]?[0-9]|1000$") };

        field.NewKeyDownEvent (Key.D1);
        field.NewKeyDownEvent (Key.D0);
        field.NewKeyDownEvent (Key.D0);
        field.NewKeyDownEvent (Key.D0);

        Assert.Equal ("1000", field.Text);
        Assert.True (field.IsValid);

        // HOME KEY
        field.NewKeyDownEvent (Key.Home);

        // DELETE
        field.NewKeyDownEvent (Key.Delete);

        Assert.Equal ("000", field.Text);
        Assert.True (field.IsValid);
    }

    [Fact]
    public void Input_With_Validate_On_Input_Set_Text ()
    {
        var field = new TextValidateField { Width = 20, Provider = new TextRegexProvider ("^[0-9][0-9][0-9]$") };

        // Input dosen't validates the pattern.
        field.NewKeyDownEvent (Key.D1);
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
    public void Input_Without_Validate_On_Input ()
    {
        var field = new TextValidateField
        {
            Width = 20, Provider = new TextRegexProvider ("^[0-9][0-9][0-9]$") { ValidateOnInput = false }
        };

        field.NewKeyDownEvent (Key.D1);
        Assert.Equal ("1", field.Text);
        Assert.False (field.IsValid);

        field.NewKeyDownEvent (Key.D2);
        Assert.Equal ("12", field.Text);
        Assert.False (field.IsValid);

        field.NewKeyDownEvent (Key.D3);
        Assert.Equal ("123", field.Text);
        Assert.True (field.IsValid);

        field.NewKeyDownEvent (Key.D4);
        Assert.Equal ("1234", field.Text);
        Assert.False (field.IsValid);
    }

    [Fact]
    public void Left_Key_Stops_At_Start_And_Insert ()
    {
        var field = new TextValidateField
        {
            TextAlignment = Alignment.Center,
            Width = 20,
            Provider = new TextRegexProvider ("^[0-9][0-9][0-9]$") { ValidateOnInput = false }
        };

        field.Text = "123";

        for (var i = 0; i < 10; i++)
        {
            field.NewKeyDownEvent (Key.CursorLeft);
        }

        Assert.Equal ("123", field.Text);
        Assert.True (field.IsValid);

        // Insert 4
        field.NewKeyDownEvent (Key.D4);

        Assert.Equal ("4123", field.Text);
        Assert.False (field.IsValid);
    }

    [Fact]
    public void Mask_With_Invalid_Pattern_Exception ()
    {
        // Regex Exception
        // Maybe it's not the right behaviour.

        var mask = "";

        for (var i = 0; i < 255; i++)
        {
            mask += (char)i;
        }

        try
        {
            var field = new TextValidateField { Width = 20, Provider = new TextRegexProvider (mask) };
        }
        catch (RegexParseException ex)
        {
            Assert.True (true, ex.Message);

            return;
        }

        Assert.True (false);
    }

    [Fact]
    public void OnTextChanged_TextChanged_Event ()
    {
        var wasTextChanged = false;

        var field = new TextValidateField
        {
            TextAlignment = Alignment.Center,
            Width = 20,
            Provider = new TextRegexProvider ("^[0-9][0-9][0-9]$") { ValidateOnInput = false }
        };

        field.Provider.TextChanged += (sender, e) => wasTextChanged = true;

        field.NewKeyDownEvent (Key.D1);

        Assert.Equal ("1", field.Provider.DisplayText);
        Assert.False (field.IsValid);
        Assert.Equal ("1", field.Provider.Text);
        Assert.True (wasTextChanged);
    }

    [Fact]
    public void Right_Key_Stops_At_End_And_Insert ()
    {
        var field = new TextValidateField
        {
            TextAlignment = Alignment.Center,
            Width = 20,
            Provider = new TextRegexProvider ("^[0-9][0-9][0-9]$") { ValidateOnInput = false }
        };

        field.Text = "123";

        for (var i = 0; i < 10; i++)
        {
            field.NewKeyDownEvent (Key.CursorRight);
        }

        Assert.Equal ("123", field.Text);
        Assert.True (field.IsValid);

        // Insert 4
        field.NewKeyDownEvent (Key.D4);

        Assert.Equal ("1234", field.Text);
        Assert.False (field.IsValid);
    }

    [Fact]
    public void Text_With_All_Charset ()
    {
        var field = new TextValidateField { Width = 20, Provider = new TextRegexProvider ("^[0-9][0-9][0-9]$") };

        var text = "";

        for (var i = 0; i < 255; i++)
        {
            text += (char)i;
        }

        field.Text = text;

        Assert.False (field.IsValid);
    }
}
