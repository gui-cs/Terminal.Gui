using Xunit;
using Terminal.Gui.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui.ConfigurationTests {
	public class SettingsScopeTests {

		[Fact]
		public void GetHardCodedDefaults_ShouldSetProperties ()
		{
			ConfigurationManager.Reset ();

			Assert.Equal (3, ((Dictionary<string, ConfigurationManager.ThemeScope>)ConfigurationManager.Settings ["Themes"].PropertyValue).Count);


			ConfigurationManager.GetHardCodedDefaults ();
			Assert.NotEmpty (ConfigurationManager.Themes);
			Assert.Equal ("Default", ConfigurationManager.Themes.Theme);

			Assert.True (ConfigurationManager.Settings ["Application.QuitKey"].PropertyValue is Key);
			Assert.True (ConfigurationManager.Settings ["Application.AlternateForwardKey"].PropertyValue is Key);
			Assert.True (ConfigurationManager.Settings ["Application.AlternateBackwardKey"].PropertyValue is Key);
			Assert.True (ConfigurationManager.Settings ["Application.UseSystemConsole"].PropertyValue is bool);
			Assert.True (ConfigurationManager.Settings ["Application.IsMouseDisabled"].PropertyValue is bool);
			Assert.True (ConfigurationManager.Settings ["Application.HeightAsBuffer"].PropertyValue is bool);

			Assert.True (ConfigurationManager.Settings ["Theme"].PropertyValue is string);
			Assert.Equal ("Default", ConfigurationManager.Settings ["Theme"].PropertyValue as string);

			Assert.True (ConfigurationManager.Settings ["Themes"].PropertyValue is Dictionary<string, ConfigurationManager.ThemeScope>);
			Assert.Single (((Dictionary<string, ConfigurationManager.ThemeScope>)ConfigurationManager.Settings ["Themes"].PropertyValue));

		}

		[Fact, AutoInitShutdown]
		public void Apply_ShouldApplyProperties ()
		{
			// arrange
			Assert.Equal (Key.Q | Key.CtrlMask, (Key)ConfigurationManager.Settings ["Application.QuitKey"].PropertyValue);
			Assert.Equal (Key.PageDown | Key.CtrlMask, (Key)ConfigurationManager.Settings ["Application.AlternateForwardKey"].PropertyValue);
			Assert.Equal (Key.PageUp | Key.CtrlMask, (Key)ConfigurationManager.Settings ["Application.AlternateBackwardKey"].PropertyValue);
			Assert.False ((bool)ConfigurationManager.Settings ["Application.UseSystemConsole"].PropertyValue);
			Assert.False ((bool)ConfigurationManager.Settings ["Application.IsMouseDisabled"].PropertyValue);
			Assert.False ((bool)ConfigurationManager.Settings ["Application.HeightAsBuffer"].PropertyValue);

			// act
			ConfigurationManager.Settings ["Application.QuitKey"].PropertyValue = Key.Q;
			ConfigurationManager.Settings ["Application.AlternateForwardKey"].PropertyValue = Key.F;
			ConfigurationManager.Settings ["Application.AlternateBackwardKey"].PropertyValue = Key.B;
			ConfigurationManager.Settings ["Application.UseSystemConsole"].PropertyValue = true;
			ConfigurationManager.Settings ["Application.IsMouseDisabled"].PropertyValue = true;
			ConfigurationManager.Settings ["Application.HeightAsBuffer"].PropertyValue = true;

			ConfigurationManager.Settings.Apply ();
			
			// assert
			Assert.Equal (Key.Q, Application.QuitKey);
			Assert.Equal (Key.F, Application.AlternateForwardKey);
			Assert.Equal (Key.B, Application.AlternateBackwardKey);
			Assert.True (Application.UseSystemConsole);
			Assert.True (Application.IsMouseDisabled);
			Assert.True (Application.HeightAsBuffer);
		}

		//[Fact]
		//public void Apply_FiresApplied ()
		//{
		//	ConfigurationManager.Reset ();

		//	ConfigurationManager.Applied += (object sender, EventArgs e) => {
		//		// assert
		//		Assert.Equal (Key.Q, Application.QuitKey);
		//		Assert.Equal (Key.F, Application.AlternateForwardKey);
		//		Assert.Equal (Key.B, Application.AlternateBackwardKey);
		//		Assert.True (Application.UseSystemConsole);
		//		Assert.True (Application.IsMouseDisabled);
		//		Assert.True (Application.HeightAsBuffer);
		//	};
		//}

		//[Fact, AutoInitShutdown]
		//public void CopyUpdatedProperitesFrom_ShouldCopyChangedPropertiesOnly ()
		//{
		//	var settings = new Settings ();
		//	settings.QuitKey = Key.End;
		//	var updatedSettings = new Settings ();

		//	///Don't set Quitkey
		//	// updatedSettings.QuitKey = Key.Q;
		//	updatedSettings.AlternateForwardKey = Key.F;
		//	updatedSettings.AlternateBackwardKey = Key.B;
		//	updatedSettings.UseSystemConsole = true;
		//	updatedSettings.IsMouseDisabled = true;
		//	updatedSettings.HeightAsBuffer = true;

		//	settings.CopyUpdatedProperitesFrom (updatedSettings);
		//	Assert.Equal (Key.End, settings.QuitKey);
		//	Assert.Equal (Key.F, settings.AlternateForwardKey);
		//	Assert.Equal (Key.B, settings.AlternateBackwardKey);
		//	Assert.True (settings.UseSystemConsole);
		//	Assert.True (settings.IsMouseDisabled);
		//	Assert.True (settings.HeightAsBuffer);
		//}
	}
}