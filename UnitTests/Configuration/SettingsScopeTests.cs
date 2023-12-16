using Xunit;
using Terminal.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Terminal.Gui.ConfigurationManager;

namespace Terminal.Gui.ConfigurationTests {
	public class SettingsScopeTests {

		[Fact]
		public void GetHardCodedDefaults_ShouldSetProperties ()
		{
			ConfigurationManager.Reset ();

			Assert.Equal (3, ((Dictionary<string, ThemeScope>)ConfigurationManager.Settings ["Themes"].PropertyValue).Count);

			ConfigurationManager.GetHardCodedDefaults ();
			Assert.NotEmpty (ConfigurationManager.Themes);
			Assert.Equal ("Default", ConfigurationManager.Themes.Theme);

			Assert.True (ConfigurationManager.Settings ["Application.QuitKey"].PropertyValue is Key);
			Assert.True (ConfigurationManager.Settings ["Application.AlternateForwardKey"].PropertyValue is Key);
			Assert.True (ConfigurationManager.Settings ["Application.AlternateBackwardKey"].PropertyValue is Key);
			Assert.True (ConfigurationManager.Settings ["Application.IsMouseDisabled"].PropertyValue is bool);

			Assert.True (ConfigurationManager.Settings ["Theme"].PropertyValue is string);
			Assert.Equal ("Default", ConfigurationManager.Settings ["Theme"].PropertyValue as string);

			Assert.True (ConfigurationManager.Settings ["Themes"].PropertyValue is Dictionary<string, ThemeScope>);
			Assert.Single (((Dictionary<string, ThemeScope>)ConfigurationManager.Settings ["Themes"].PropertyValue));

		}

		[Fact, AutoInitShutdown]
		public void Apply_ShouldApplyProperties ()
		{
			// arrange
			Assert.Equal (KeyCode.Q | KeyCode.CtrlMask, ((Key)ConfigurationManager.Settings ["Application.QuitKey"].PropertyValue).KeyCode);
			Assert.Equal (KeyCode.PageDown | KeyCode.CtrlMask, ((Key)ConfigurationManager.Settings ["Application.AlternateForwardKey"].PropertyValue).KeyCode);
			Assert.Equal (KeyCode.PageUp | KeyCode.CtrlMask, ((Key)ConfigurationManager.Settings ["Application.AlternateBackwardKey"].PropertyValue).KeyCode);
			Assert.False ((bool)ConfigurationManager.Settings ["Application.IsMouseDisabled"].PropertyValue);

			// act
			ConfigurationManager.Settings ["Application.QuitKey"].PropertyValue = new Key (KeyCode.Q);
			ConfigurationManager.Settings ["Application.AlternateForwardKey"].PropertyValue = new Key (KeyCode.F);
			ConfigurationManager.Settings ["Application.AlternateBackwardKey"].PropertyValue = new Key (KeyCode.B);
			ConfigurationManager.Settings ["Application.IsMouseDisabled"].PropertyValue = true;

			ConfigurationManager.Settings.Apply ();

			// assert
			Assert.Equal (KeyCode.Q, Application.QuitKey.KeyCode);
			Assert.Equal (KeyCode.F, Application.AlternateForwardKey.KeyCode);
			Assert.Equal (KeyCode.B, Application.AlternateBackwardKey.KeyCode);
			Assert.True (Application.IsMouseDisabled);
		}

		[Fact, AutoInitShutdown]
		public void CopyUpdatedProperitesFrom_ShouldCopyChangedPropertiesOnly ()
		{
			ConfigurationManager.Settings ["Application.QuitKey"].PropertyValue = new Key (KeyCode.End); ;

			var updatedSettings = new SettingsScope ();

			///Don't set Quitkey
			updatedSettings ["Application.AlternateForwardKey"].PropertyValue = new Key (KeyCode.F);
			updatedSettings ["Application.AlternateBackwardKey"].PropertyValue = new Key (KeyCode.B);
			updatedSettings ["Application.IsMouseDisabled"].PropertyValue = true;

			ConfigurationManager.Settings.Update (updatedSettings);
			Assert.Equal (KeyCode.End, ((Key)ConfigurationManager.Settings ["Application.QuitKey"].PropertyValue).KeyCode);
			Assert.Equal (KeyCode.F,  ((Key)updatedSettings ["Application.AlternateForwardKey"].PropertyValue).KeyCode);
			Assert.Equal (KeyCode.B, ((Key)updatedSettings ["Application.AlternateBackwardKey"].PropertyValue).KeyCode);
			Assert.True ((bool)updatedSettings ["Application.IsMouseDisabled"].PropertyValue);
		}
	}
}