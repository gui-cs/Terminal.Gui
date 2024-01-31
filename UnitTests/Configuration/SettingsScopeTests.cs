#region

using static Terminal.Gui.ConfigurationManager;

#endregion

namespace Terminal.Gui.ConfigurationTests;

public class SettingsScopeTests {
    [Fact]
    public void GetHardCodedDefaults_ShouldSetProperties () {
        Reset ();

        Assert.Equal (3, ((Dictionary<string, ThemeScope>)Settings["Themes"].PropertyValue).Count);

        GetHardCodedDefaults ();
        Assert.NotEmpty (Themes);
        Assert.Equal ("Default", Themes.Theme);

        Assert.True (Settings["Application.QuitKey"].PropertyValue is Key);
        Assert.True (Settings["Application.AlternateForwardKey"].PropertyValue is Key);
        Assert.True (Settings["Application.AlternateBackwardKey"].PropertyValue is Key);
        Assert.True (Settings["Application.IsMouseDisabled"].PropertyValue is bool);

        Assert.True (Settings["Theme"].PropertyValue is string);
        Assert.Equal ("Default", Settings["Theme"].PropertyValue as string);

        Assert.True (Settings["Themes"].PropertyValue is Dictionary<string, ThemeScope>);
        Assert.Single ((Dictionary<string, ThemeScope>)Settings["Themes"].PropertyValue);
    }

    [Fact, AutoInitShutdown]
    public void Apply_ShouldApplyProperties () {
        // arrange
        Assert.Equal (KeyCode.Q | KeyCode.CtrlMask, ((Key)Settings["Application.QuitKey"].PropertyValue).KeyCode);
        Assert.Equal (
                      KeyCode.PageDown | KeyCode.CtrlMask,
                      ((Key)Settings["Application.AlternateForwardKey"].PropertyValue).KeyCode);
        Assert.Equal (
                      KeyCode.PageUp | KeyCode.CtrlMask,
                      ((Key)Settings["Application.AlternateBackwardKey"].PropertyValue).KeyCode);
        Assert.False ((bool)Settings["Application.IsMouseDisabled"].PropertyValue);

        // act
        Settings["Application.QuitKey"].PropertyValue = new Key (KeyCode.Q);
        Settings["Application.AlternateForwardKey"].PropertyValue = new Key (KeyCode.F);
        Settings["Application.AlternateBackwardKey"].PropertyValue = new Key (KeyCode.B);
        Settings["Application.IsMouseDisabled"].PropertyValue = true;

        Settings.Apply ();

        // assert
        Assert.Equal (KeyCode.Q, Application.QuitKey.KeyCode);
        Assert.Equal (KeyCode.F, Application.AlternateForwardKey.KeyCode);
        Assert.Equal (KeyCode.B, Application.AlternateBackwardKey.KeyCode);
        Assert.True (Application.IsMouseDisabled);
    }

    [Fact, AutoInitShutdown]
    public void CopyUpdatedPropertiesFrom_ShouldCopyChangedPropertiesOnly () {
        Settings["Application.QuitKey"].PropertyValue = new Key (KeyCode.End);
        ;

        var updatedSettings = new SettingsScope ();

        ///Don't set Quitkey
        updatedSettings["Application.AlternateForwardKey"].PropertyValue = new Key (KeyCode.F);
        updatedSettings["Application.AlternateBackwardKey"].PropertyValue = new Key (KeyCode.B);
        updatedSettings["Application.IsMouseDisabled"].PropertyValue = true;

        Settings.Update (updatedSettings);
        Assert.Equal (KeyCode.End, ((Key)Settings["Application.QuitKey"].PropertyValue).KeyCode);
        Assert.Equal (KeyCode.F, ((Key)updatedSettings["Application.AlternateForwardKey"].PropertyValue).KeyCode);
        Assert.Equal (KeyCode.B, ((Key)updatedSettings["Application.AlternateBackwardKey"].PropertyValue).KeyCode);
        Assert.True ((bool)updatedSettings["Application.IsMouseDisabled"].PropertyValue);
    }
}
