using static Terminal.Gui.ConfigurationManager;

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
                      KeyCode.PageDown | KeyCode.CtrlMask,
                      ((Key)Settings ["Application.AlternateForwardKey"].PropertyValue).KeyCode
                     );

        Assert.Equal (
                      KeyCode.PageUp | KeyCode.CtrlMask,
                      ((Key)Settings ["Application.AlternateBackwardKey"].PropertyValue).KeyCode
                     );

        // act
        Settings ["Application.QuitKey"].PropertyValue = Key.Q;
        Settings ["Application.AlternateForwardKey"].PropertyValue = Key.F;
        Settings ["Application.AlternateBackwardKey"].PropertyValue = Key.B;

        Settings.Apply ();

        // assert
        Assert.Equal (KeyCode.Q, Application.QuitKey.KeyCode);
        Assert.Equal (KeyCode.F, Application.AlternateForwardKey.KeyCode);
        Assert.Equal (KeyCode.B, Application.AlternateBackwardKey.KeyCode);
    }

    [Fact]
    [AutoInitShutdown]
    public void CopyUpdatedPropertiesFrom_ShouldCopyChangedPropertiesOnly ()
    {
        Settings ["Application.QuitKey"].PropertyValue = Key.End;
        ;

        var updatedSettings = new SettingsScope ();

        ///Don't set Quitkey
        updatedSettings ["Application.AlternateForwardKey"].PropertyValue = Key.F;
        updatedSettings ["Application.AlternateBackwardKey"].PropertyValue = Key.B;

        Settings.Update (updatedSettings);
        Assert.Equal (KeyCode.End, ((Key)Settings ["Application.QuitKey"].PropertyValue).KeyCode);
        Assert.Equal (KeyCode.F, ((Key)updatedSettings ["Application.AlternateForwardKey"].PropertyValue).KeyCode);
        Assert.Equal (KeyCode.B, ((Key)updatedSettings ["Application.AlternateBackwardKey"].PropertyValue).KeyCode);
    }

    [Fact]
    public void GetHardCodedDefaults_ShouldSetProperties ()
    {
        Reset ();

        Assert.Equal (3, ((Dictionary<string, ThemeScope>)Settings ["Themes"].PropertyValue).Count);

        GetHardCodedDefaults ();
        Assert.NotEmpty (Themes);
        Assert.Equal ("Default", Themes.Theme);

        Assert.True (Settings ["Application.QuitKey"].PropertyValue is Key);
        Assert.True (Settings ["Application.AlternateForwardKey"].PropertyValue is Key);
        Assert.True (Settings ["Application.AlternateBackwardKey"].PropertyValue is Key);

        Assert.True (Settings ["Theme"].PropertyValue is string);
        Assert.Equal ("Default", Settings ["Theme"].PropertyValue as string);

        Assert.True (Settings ["Themes"].PropertyValue is Dictionary<string, ThemeScope>);
        Assert.Single ((Dictionary<string, ThemeScope>)Settings ["Themes"].PropertyValue);
    }
}
