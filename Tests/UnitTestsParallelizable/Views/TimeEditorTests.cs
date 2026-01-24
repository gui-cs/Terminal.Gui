using System.Globalization;
using UnitTests;

namespace ViewsTests;

public class TimeEditorTests : TestDriverBase
{
    [Fact]
    public void Constructor_Defaults ()
    {
        TimeEditor te = new ();
        te.Layout ();
        
        Assert.NotNull (te.Provider);
        Assert.IsType<TimeTextProvider> (te.Provider);
        Assert.Equal (TimeSpan.Zero, te.Value);
        Assert.NotNull (te.Format);
        Assert.Equal (CultureInfo.CurrentCulture.DateTimeFormat, te.Format);
    }

    [Fact]
    public void Value_Property_GetSet ()
    {
        TimeEditor te = new ();
        TimeSpan testTime = new (14, 30, 45);
        
        te.Value = testTime;
        Assert.Equal (testTime, te.Value);
        
        // Test setting to zero
        te.Value = TimeSpan.Zero;
        Assert.Equal (TimeSpan.Zero, te.Value);
        
        // Test setting to max
        te.Value = TimeSpan.FromHours (23) + TimeSpan.FromMinutes (59) + TimeSpan.FromSeconds (59);
        Assert.Equal (new TimeSpan (23, 59, 59), te.Value);
    }

    [Fact]
    public void Format_Property_Changes_Width ()
    {
        TimeEditor te = new ();
        te.Layout ();
        
        int initialWidth = te.Frame.Width;
        Assert.True (initialWidth > 0);
        
        // Change to a different culture with different pattern
        DateTimeFormatInfo customFormat = (DateTimeFormatInfo)CultureInfo.CurrentCulture.DateTimeFormat.Clone ();
        customFormat.LongTimePattern = "HH:mm";
        te.Format = customFormat;
        te.Layout ();
        
        // Width should change to accommodate shorter pattern
        int newWidth = te.Frame.Width;
        Assert.NotEqual (initialWidth, newWidth);
    }

    [Fact]
    public void ValueChanging_Event_Can_Cancel ()
    {
        TimeEditor te = new () { Value = TimeSpan.FromHours (10) };
        bool eventFired = false;
        
        te.ValueChanging += (_, e) =>
        {
            eventFired = true;
            e.Handled = true; // Cancel the change
        };
        
        te.Value = TimeSpan.FromHours (15);
        
        Assert.True (eventFired);
        Assert.Equal (TimeSpan.FromHours (10), te.Value); // Value should not change
    }

    [Fact]
    public void ValueChanged_Event_Fires ()
    {
        TimeEditor te = new () { Value = TimeSpan.FromHours (10) };
        bool eventFired = false;
        TimeSpan? oldValue = null;
        TimeSpan? newValue = null;
        
        te.ValueChanged += (_, e) =>
        {
            eventFired = true;
            oldValue = e.OldValue;
            newValue = e.NewValue;
        };
        
        TimeSpan expectedNewValue = TimeSpan.FromHours (15);
        te.Value = expectedNewValue;
        
        Assert.True (eventFired);
        Assert.Equal (TimeSpan.FromHours (10), oldValue);
        Assert.Equal (expectedNewValue, newValue);
    }

    [Fact]
    public void TimeTextProvider_CursorNavigation_SkipsSeparators ()
    {
        TimeTextProvider provider = new ();
        
        // CursorStart should return 0
        Assert.Equal (0, provider.CursorStart ());
        
        // For a format like "HH:mm:ss" (positions: 0,1,:,3,4,:,6,7)
        // Position 2 is separator, cursor should skip it
        int cursorPos = provider.CursorRight (1);
        Assert.NotEqual (2, cursorPos); // Should skip position 2 (separator)
        
        // CursorLeft from position 3 should skip separator at 2 and go to 1
        cursorPos = provider.CursorLeft (3);
        Assert.Equal (1, cursorPos);
    }

    [Fact]
    public void TimeTextProvider_InsertAt_ReplacesDigit ()
    {
        TimeTextProvider provider = new ();
        provider.TimeValue = TimeSpan.Zero; // "00:00:00" in 24h format
        
        // Insert '1' at position 0 (first hour digit)
        bool result = provider.InsertAt ('1', 0);
        Assert.True (result);
        
        // Check that the value was updated
        string text = provider.Text;
        Assert.StartsWith ("1", text.TrimStart ());
    }

    [Fact]
    public void TimeTextProvider_Delete_ReplacesWithZero ()
    {
        TimeTextProvider provider = new ();
        
        // Use 24-hour format to avoid culture-specific issues
        DateTimeFormatInfo format24h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
        provider.Format = format24h;
        
        provider.TimeValue = new TimeSpan (14, 30, 45);
        
        // Delete at position 0 should replace with '0'
        bool result = provider.Delete (0);
        Assert.True (result);
        
        // The hour should now start with 0
        string text = provider.Text.Trim ();
        Assert.StartsWith ("0", text);
    }

    [Fact]
    public void TimeTextProvider_Format_Change_Updates_Pattern ()
    {
        TimeTextProvider provider = new ();
        TimeSpan testTime = new (14, 30, 45);
        provider.TimeValue = testTime;
        
        string initialDisplay = provider.DisplayText;
        
        // Change to a custom format
        DateTimeFormatInfo customFormat = (DateTimeFormatInfo)CultureInfo.CurrentCulture.DateTimeFormat.Clone ();
        customFormat.LongTimePattern = "HH:mm";
        provider.Format = customFormat;
        
        string newDisplay = provider.DisplayText;
        
        // Display should change
        Assert.NotEqual (initialDisplay, newDisplay);
        
        // New display should not contain seconds
        Assert.DoesNotContain ("45", newDisplay);
    }

    [Fact]
    public void TimeTextProvider_Validates_Hours_Minutes_Seconds ()
    {
        TimeTextProvider provider = new ();
        
        // Valid time
        provider.TimeValue = new TimeSpan (23, 59, 59);
        Assert.True (provider.IsValid);
        
        // Another valid time
        provider.TimeValue = new TimeSpan (0, 0, 0);
        Assert.True (provider.IsValid);
        
        // Provider auto-corrects invalid values, so IsValid should always be true
        provider.TimeValue = new TimeSpan (12, 30, 15);
        Assert.True (provider.IsValid);
    }

    [Fact]
    public void TimeEditor_KeyInput_UpdatesValue ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        
        try
        {
            TimeEditor te = new () { App = app };
            te.Layout ();
            te.Value = TimeSpan.Zero;
            
            // Simulate typing '1'
            te.NewKeyDownEvent (Key.D1);
            
            // The value should have been updated
            string text = te.Text.Trim ();
            Assert.Contains ("1", text);
        }
        finally
        {
            app.Dispose ();
        }
    }

    [Fact]
    public void TimeEditor_Navigation_Keys ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        
        try
        {
            TimeEditor te = new () { App = app };
            te.Layout ();
            
            // Home key should move to start
            te.NewKeyDownEvent (Key.Home);
            
            // End key should move to end
            te.NewKeyDownEvent (Key.End);
            
            // Arrow keys should navigate
            te.NewKeyDownEvent (Key.CursorLeft);
            te.NewKeyDownEvent (Key.CursorRight);
            
            // No exceptions should be thrown
            Assert.NotNull (te);
        }
        finally
        {
            app.Dispose ();
        }
    }

    [Fact]
    public void TimeEditor_IValue_GetValue ()
    {
        TimeEditor te = new ();
        TimeSpan testTime = new (14, 30, 45);
        te.Value = testTime;
        
        object? value = ((Terminal.Gui.ViewBase.IValue)te).GetValue ();
        
        Assert.NotNull (value);
        Assert.IsType<TimeSpan> (value);
        Assert.Equal (testTime, (TimeSpan)value);
    }

    [Fact]
    public void TimeTextProvider_12Hour_Format_With_AM_PM ()
    {
        TimeTextProvider provider = new ();
        
        // Set to a 12-hour format
        DateTimeFormatInfo format12h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
        format12h.LongTimePattern = "h:mm:ss tt";
        provider.Format = format12h;
        
        // Set time to 2:30 PM (14:30)
        provider.TimeValue = new TimeSpan (14, 30, 0);
        
        string display = provider.DisplayText.Trim ();
        
        // Should contain PM
        Assert.Contains ("PM", display, StringComparison.OrdinalIgnoreCase);
        
        // Set time to 2:30 AM (2:30)
        provider.TimeValue = new TimeSpan (2, 30, 0);
        
        display = provider.DisplayText.Trim ();
        
        // Should contain AM
        Assert.Contains ("AM", display, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TimeTextProvider_24Hour_Format ()
    {
        TimeTextProvider provider = new ();
        
        // Set to a 24-hour format
        DateTimeFormatInfo format24h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
        format24h.LongTimePattern = "HH:mm:ss";
        provider.Format = format24h;
        
        // Set time to 14:30
        provider.TimeValue = new TimeSpan (14, 30, 0);
        
        string display = provider.DisplayText.Trim ();
        
        // Should contain 14
        Assert.Contains ("14", display);
        
        // Should not contain AM/PM
        Assert.DoesNotContain ("AM", display, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain ("PM", display, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TimeEditor_Delete_And_Backspace ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        
        try
        {
            TimeEditor te = new () { App = app, Value = new TimeSpan (12, 34, 56) };
            te.Layout ();
            
            // Move to start and delete
            te.NewKeyDownEvent (Key.Home);
            te.NewKeyDownEvent (Key.Delete);
            
            // Value should have changed (first digit replaced with 0)
            string text = te.Text.Trim ();
            Assert.NotEqual ("12:34:56", text);
            
            // Backspace should also work
            te.NewKeyDownEvent (Key.Backspace);
            
            // Text should have changed again
            Assert.NotNull (te.Text);
        }
        finally
        {
            app.Dispose ();
        }
    }

    [Fact]
    public void TimeTextProvider_CursorEnd_Returns_Last_Position ()
    {
        TimeTextProvider provider = new ();
        
        int endPos = provider.CursorEnd ();
        
        // End position should be >= 0
        Assert.True (endPos >= 0);
        
        // For 24-hour format like "HH:mm:ss", end should be at last digit (position 7)
        // For 12-hour format with AM/PM, end should be at AM/PM position
        int displayLength = provider.DisplayText.Trim ().Length;
        Assert.True (endPos < displayLength);
    }

    [Fact]
    public void TimeEditor_Text_Property_Updates_Value ()
    {
        TimeEditor te = new ();
        
        // Set text directly
        te.Text = "14:30:45";
        
        // Value should be updated
        Assert.Equal (14, te.Value.Hours);
        Assert.Equal (30, te.Value.Minutes);
        Assert.Equal (45, te.Value.Seconds);
    }

    [Fact]
    public void TimeTextProvider_InsertAt_NonDigit_Returns_False ()
    {
        TimeTextProvider provider = new ();
        
        // Try to insert a non-digit character at a digit position
        bool result = provider.InsertAt ('x', 0);
        
        // Should fail
        Assert.False (result);
    }

    [Fact]
    public void TimeEditor_Multiple_Format_Changes ()
    {
        TimeEditor te = new () { Value = new TimeSpan (14, 30, 45) };
        te.Layout ();
        
        // Change format multiple times
        for (int i = 0; i < 3; i++)
        {
            DateTimeFormatInfo format = (DateTimeFormatInfo)CultureInfo.CurrentCulture.DateTimeFormat.Clone ();
            format.LongTimePattern = i % 2 == 0 ? "HH:mm" : "HH:mm:ss";
            te.Format = format;
            te.Layout ();
            
            // Value should remain the same
            Assert.Equal (14, te.Value.Hours);
            Assert.Equal (30, te.Value.Minutes);
        }
    }
}
