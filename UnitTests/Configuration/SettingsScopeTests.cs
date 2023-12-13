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

			Assert.True (ConfigurationManager.Settings ["Application.QuitKey"].PropertyValue is ConsoleDriverKey);
			Assert.True (ConfigurationManager.Settings ["Application.AlternateForwardKey"].PropertyValue is ConsoleDriverKey);
			Assert.True (ConfigurationManager.Settings ["Application.AlternateBackwardKey"].PropertyValue is ConsoleDriverKey);
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
			Assert.Equal (ConsoleDriverKey.Q | ConsoleDriverKey.CtrlMask, (ConsoleDriverKey)ConfigurationManager.Settings ["Application.QuitKey"].PropertyValue);
			Assert.Equal (ConsoleDriverKey.PageDown | ConsoleDriverKey.CtrlMask, (ConsoleDriverKey)ConfigurationManager.Settings ["Application.AlternateForwardKey"].PropertyValue);
			Assert.Equal (ConsoleDriverKey.PageUp | ConsoleDriverKey.CtrlMask, (ConsoleDriverKey)ConfigurationManager.Settings ["Application.AlternateBackwardKey"].PropertyValue);
			Assert.False ((bool)ConfigurationManager.Settings ["Application.IsMouseDisabled"].PropertyValue);

			// act
			ConfigurationManager.Settings ["Application.QuitKey"].PropertyValue = ConsoleDriverKey.Q;
			ConfigurationManager.Settings ["Application.AlternateForwardKey"].PropertyValue = ConsoleDriverKey.F;
			ConfigurationManager.Settings ["Application.AlternateBackwardKey"].PropertyValue = ConsoleDriverKey.B;
			ConfigurationManager.Settings ["Application.IsMouseDisabled"].PropertyValue = true;

			ConfigurationManager.Settings.Apply ();

			// assert
			Assert.Equal (ConsoleDriverKey.Q, Application.QuitKey);
			Assert.Equal (ConsoleDriverKey.F, Application.AlternateForwardKey);
			Assert.Equal (ConsoleDriverKey.B, Application.AlternateBackwardKey);
			Assert.True (Application.IsMouseDisabled);
		}

		[Fact, AutoInitShutdown]
		public void CopyUpdatedProperitesFrom_ShouldCopyChangedPropertiesOnly ()
		{
			ConfigurationManager.Settings ["Application.QuitKey"].PropertyValue = ConsoleDriverKey.End;

			var updatedSettings = new SettingsScope ();

			///Don't set Quitkey
			updatedSettings["Application.AlternateForwardKey"].PropertyValue = ConsoleDriverKey.F;
			updatedSettings["Application.AlternateBackwardKey"].PropertyValue = ConsoleDriverKey.B;
			updatedSettings["Application.IsMouseDisabled"].PropertyValue = true;

			ConfigurationManager.Settings.Update (updatedSettings);
			Assert.Equal (ConsoleDriverKey.End, ConfigurationManager.Settings ["Application.QuitKey"].PropertyValue);
			Assert.Equal (ConsoleDriverKey.F, updatedSettings ["Application.AlternateForwardKey"].PropertyValue);
			Assert.Equal (ConsoleDriverKey.B, updatedSettings ["Application.AlternateBackwardKey"].PropertyValue);
			Assert.True ((bool)updatedSettings ["Application.IsMouseDisabled"].PropertyValue);
		}
	}
}