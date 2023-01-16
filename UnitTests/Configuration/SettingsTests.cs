using Xunit;
using Terminal.Gui.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui.ConfigurationTests {
	public class SettingsTests {
		[Fact, AutoInitShutdown]
		public void GetHardCodedDefaults_ShouldSetProperties ()
		{
			var settings = new Settings ();
			settings.GetHardCodedDefaults ();
			Assert.True (settings.QuitKey.HasValue);
			Assert.True (settings.AlternateForwardKey.HasValue);
			Assert.True (settings.AlternateBackwardKey.HasValue);
			Assert.True (settings.UseSystemConsole.HasValue);
			Assert.True (settings.IsMouseDisabled.HasValue);
			Assert.True (settings.HeightAsBuffer.HasValue);
		}

		[Fact, AutoInitShutdown]
		public void Apply_ShouldApplyProperties ()
		{
			var settings = new Settings ();
			settings.QuitKey = Key.Q;
			settings.AlternateForwardKey = Key.F;
			settings.AlternateBackwardKey = Key.B;
			settings.UseSystemConsole = true;
			settings.IsMouseDisabled = true;
			settings.HeightAsBuffer = true;

			settings.Apply ();
			Assert.Equal (Key.Q, Application.QuitKey);
			Assert.Equal (Key.F, Application.AlternateForwardKey);
			Assert.Equal (Key.B, Application.AlternateBackwardKey);
			Assert.True (Application.UseSystemConsole);
			Assert.True (Application.IsMouseDisabled);
			Assert.True (Application.HeightAsBuffer);
		}

		[Fact, AutoInitShutdown]
		public void CopyUpdatedProperitesFrom_ShouldCopyChangedPropertiesOnly ()
		{
			var settings = new Settings ();
			settings.QuitKey = Key.End;
			var updatedSettings = new Settings ();
			
			///Don't set Quitkey
			// updatedSettings.QuitKey = Key.Q;
			updatedSettings.AlternateForwardKey = Key.F;
			updatedSettings.AlternateBackwardKey = Key.B;
			updatedSettings.UseSystemConsole = true;
			updatedSettings.IsMouseDisabled = true;
			updatedSettings.HeightAsBuffer = true;

			settings.CopyUpdatedProperitesFrom (updatedSettings);
			Assert.Equal (Key.End, settings.QuitKey);
			Assert.Equal (Key.F, settings.AlternateForwardKey);
			Assert.Equal (Key.B, settings.AlternateBackwardKey);
			Assert.True (settings.UseSystemConsole);
			Assert.True (settings.IsMouseDisabled);
			Assert.True (settings.HeightAsBuffer);
		}
	}
}