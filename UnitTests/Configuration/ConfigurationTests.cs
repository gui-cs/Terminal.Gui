using Xunit;
using Terminal.Gui.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui.ConfigurationTests {
	public class ConfigurationTests {
		[Fact, AutoInitShutdown]
		public void TestApplyAll_AppliesSettingsAndThemes ()
		{
			// Arrange
			var config = new ConfigRoot {
				Settings = new Settings { QuitKey = Key.Q },
				Themes = new Themes { SelectedTheme = "Custom" }
			};

			// Act
			config.ApplyAll ();

			// Assert
			Assert.Equal (Key.Q, Application.QuitKey);
			Assert.Equal ("Custom", config.Themes.SelectedTheme);
		}


		[Fact, AutoInitShutdown]
		public void TestCopyUpdatedPropertiesFrom_CopiesOnlyValidProperties ()
		{
			// Arrange
			var config = new ConfigRoot {
				Settings = new Settings { QuitKey = Key.Q },
				Themes = new Themes { SelectedTheme = "Custom" }
			};
			var newConfig = new ConfigRoot {
				Settings = new Settings { QuitKey = Key.A },
				Themes = new Themes { SelectedTheme = "Default" }
			};

			// Act
			config.CopyUpdatedProperitesFrom (newConfig);

			// Assert
			Assert.Equal (Key.A, config.Settings.QuitKey);
			Assert.Equal ("Default", config.Themes.SelectedTheme);
		}

		[Fact, AutoInitShutdown]
		public void TestGetAllHardCodedDefaults_RetrievesDefaults ()
		{
			// Arrange
			var config = new ConfigRoot ();

			// Act
			config.GetAllHardCodedDefaults ();

			// Assert
			Assert.Equal ("https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json", config.schema);
			Assert.Equal ("Default", config.Themes.SelectedTheme);
			Assert.NotNull (config.Settings);
			Assert.NotNull (config.Themes);
		}
	}

}

