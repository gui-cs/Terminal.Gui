﻿using static Terminal.Gui.ConfigurationManager;

namespace Terminal.Gui.ConfigurationTests;

public class SettingsScopeTests
{
    [Fact]
    [AutoInitShutdown]
    public void Apply_ShouldApplyProperties ()
    {
        // arrange
        Assert.Equal (Key.Esc, (Key)Settings ["Application.QuitKey"].PropertyValue);

        Assert.Equal (
                      Key.F6,
                      (Key)Settings ["Application.NextTabGroupKey"].PropertyValue
                     );

        Assert.Equal (
                      Key.F6.WithShift,
                      (Key)Settings["Application.PrevTabGroupKey"].PropertyValue
                     );

        // act
        Settings ["Application.QuitKey"].PropertyValue = Key.Q;
        Settings ["Application.NextTabGroupKey"].PropertyValue = Key.F;
        Settings ["Application.PrevTabGroupKey"].PropertyValue = Key.B;

        Settings.Apply ();

        // assert
        Assert.Equal (Key.Q, Application.QuitKey);
        Assert.Equal (Key.F, Application.NextTabGroupKey);
        Assert.Equal (Key.B, Application.PrevTabGroupKey);
    }

    [Fact]
    [AutoInitShutdown]
    public void CopyUpdatedPropertiesFrom_ShouldCopyChangedPropertiesOnly ()
    {
        Settings ["Application.QuitKey"].PropertyValue = Key.End;

        var updatedSettings = new SettingsScope ();

        ///Don't set Quitkey
        updatedSettings ["Application.NextTabGroupKey"].PropertyValue = Key.F;
        updatedSettings ["Application.PrevTabGroupKey"].PropertyValue = Key.B;

        Settings.Update (updatedSettings);
        Assert.Equal (KeyCode.End, ((Key)Settings ["Application.QuitKey"].PropertyValue).KeyCode);
        Assert.Equal (KeyCode.F, ((Key)updatedSettings ["Application.NextTabGroupKey"].PropertyValue).KeyCode);
        Assert.Equal (KeyCode.B, ((Key)updatedSettings ["Application.PrevTabGroupKey"].PropertyValue).KeyCode);
    }

    [Fact]
    public void GetHardCodedDefaults_ShouldSetProperties ()
    {
        Reset ();

        Assert.Equal (5, ((Dictionary<string, ThemeScope>)Settings ["Themes"].PropertyValue).Count);

        GetHardCodedDefaults ();
        Assert.NotEmpty (Themes);
        Assert.Equal ("Default", Themes.Theme);

        Assert.True (Settings ["Application.QuitKey"].PropertyValue is Key);
        Assert.True (Settings ["Application.NextTabGroupKey"].PropertyValue is Key);
        Assert.True (Settings ["Application.PrevTabGroupKey"].PropertyValue is Key);

        Assert.True (Settings ["Theme"].PropertyValue is string);
        Assert.Equal ("Default", Settings ["Theme"].PropertyValue as string);

        Assert.True (Settings ["Themes"].PropertyValue is Dictionary<string, ThemeScope>);
        Assert.Single ((Dictionary<string, ThemeScope>)Settings ["Themes"].PropertyValue);
    }
}
