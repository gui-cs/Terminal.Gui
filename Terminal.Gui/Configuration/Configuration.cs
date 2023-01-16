using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Terminal.Gui.Configuration {
	/// <summary>
	/// Classes that read/write configuration file sections (<see cref="Settings"/> and <see cref="Themes"/> are derived from this class. 
	/// </summary>
	[JsonDerivedType (typeof (Settings))]
	[JsonDerivedType (typeof (Themes))]
	public abstract class Config<T> {
		/// <summary>
		/// Retrieves the hard coded default settings from the implementation (e.g. from <see cref="ListView"/>); called to 
		/// initlize a <see cref="Config{T}"/> object instance.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This method is only really useful when using <see cref="ConfigurationManager.SaveHardCodedDefaults(string)"/>
		/// to generate the JSON doc that is embedded into Terminal.Gui (during development). 
		/// </para>
		/// <para>
		/// If this method is used before Terminal.Gui has been initizlied (either <see cref="Application"/> 
		/// or <see cref="ConsoleDriver.Init(Action)"/>) care must be taken to ensure settings that can't be 
		/// set/read don't throw exceptions. 
		/// </para>
		/// </remarks>
		public abstract void GetHardCodedDefaults ();

		/// <summary>
		/// Applys the settings held by this <see cref="Config{T}"/> object to the running <see cref="Application"/>. 
		/// </summary>
		/// <remarks>
		/// This method must only set a target setting if the configuration held here was actually set (because it was
		/// read from JSON).
		/// </remarks>
		public abstract void Apply ();

		// TODO: Consider refactoring this to use reflection to set the properties.
		// see: https://github.com/tig/winprint/blob/master/proto/winforms/WinPrint.Core/Models/ModelBase.cs
		// see: https://github.com/dotnet/runtime/issues/78556

		/// <summary>
		/// Copies new or updated settings from the specified <see cref="Config{T}"/> object into this one. Called when JSON has been loaded. 
		/// </summary>
		/// <remarks>
		/// <para>
		/// Implementations must set the internal state in such a way that <see cref="Apply"/> can know whether a particular configuration 
		/// setting was actually set.
		/// </para>
		/// <para>
		/// Implementations must only add or copy properties that were set/changed in <paramref name="changedConfig"/>.
		/// </para>
		/// </remarks>		
		/// <param name="changedConfig">The <see cref="Config{T}"/> object that has new/changed properties.</param>
		public abstract void CopyUpdatedProperitesFrom (T changedConfig);
	}

	/// <summary>
	/// Defines the Application settings for a Terminal.Gui application.
	/// </summary>
	/// <example><code>
	///  "Settings": {
	///    "QuitKey": {
	///      "Key": "Q",
	///      "Modifiers": [
	///        "Ctrl"
	///      ]
	///    },
	///    "AlternateForwardKey": {
	///      "Key": "PageDown",
	///      "Modifiers": [
	///         "Ctrl"
	///      ]
	///    },
	///    "AlternateBackwardKey": {
	///      "Key": "PageUp",
	///      "Modifiers": [
	///      "Ctrl"
	///      ]
	///    },
	///    "UseSystemConsole": false,
	///    "IsMouseDisabled": false,
	///    "HeightAsBuffer": false
	///  }
	/// </code></example>
	public class Settings : Config<Settings> {
		/// <summary>
		/// The <see cref="Application.QuitKey"/> setting.
		/// </summary>
		public Key? QuitKey { get; set; }

		/// <summary>
		/// The <see cref="Application.AlternateForwardKey"/> setting.
		/// </summary>
		public Key? AlternateForwardKey { get; set; }

		/// <summary>
		/// The <see cref="Application.AlternateBackwardKey"/> setting.
		/// </summary>
		public Key? AlternateBackwardKey { get; set; }

		/// <summary>
		/// The <see cref="Application.UseSystemConsole"/> setting.
		/// </summary>
		public bool? UseSystemConsole { get; set; }

		/// <summary>
		/// The <see cref="Application.IsMouseDisabled"/> setting.
		/// </summary>
		public bool? IsMouseDisabled { get; set; }

		/// <summary>
		/// The <see cref="Application.HeightAsBuffer"/> setting.
		/// </summary>
		public bool? HeightAsBuffer { get; set; }

		/// <inheritdoc/>
		public override void GetHardCodedDefaults ()
		{
			if (Application.Driver != null) {
				HeightAsBuffer = Application.HeightAsBuffer;
			}
			AlternateForwardKey = Application.AlternateForwardKey;
			AlternateBackwardKey = Application.AlternateBackwardKey;
			QuitKey = Application.QuitKey;
			IsMouseDisabled = Application.IsMouseDisabled;
			UseSystemConsole = Application.UseSystemConsole;
		}

		/// <inheritdoc/>
		public override void Apply ()
		{

			if (Application.Driver != null && HeightAsBuffer.HasValue) Application.HeightAsBuffer = HeightAsBuffer.Value;
			if (AlternateForwardKey.HasValue) Application.AlternateForwardKey = AlternateForwardKey.Value;
			if (AlternateBackwardKey.HasValue) Application.AlternateBackwardKey = AlternateBackwardKey.Value;
			if (QuitKey.HasValue) Application.QuitKey = QuitKey.Value;
			if (IsMouseDisabled.HasValue) Application.IsMouseDisabled = IsMouseDisabled.Value;
			if (UseSystemConsole.HasValue) Application.UseSystemConsole = UseSystemConsole.Value;
		}

		/// <inheritdoc/>
		public override void CopyUpdatedProperitesFrom (Settings updatedSettings)
		{
			if (updatedSettings.HeightAsBuffer.HasValue) HeightAsBuffer = updatedSettings.HeightAsBuffer.Value;
			if (updatedSettings.AlternateForwardKey.HasValue) AlternateForwardKey = updatedSettings.AlternateForwardKey.Value;
			if (updatedSettings.AlternateBackwardKey.HasValue) AlternateBackwardKey = updatedSettings.AlternateBackwardKey.Value;
			if (updatedSettings.QuitKey.HasValue) QuitKey = updatedSettings.QuitKey.Value;
			if (updatedSettings.IsMouseDisabled.HasValue) IsMouseDisabled = updatedSettings.IsMouseDisabled.Value;
			if (updatedSettings.UseSystemConsole.HasValue) UseSystemConsole = updatedSettings.UseSystemConsole.Value;
		}
	}

	/// <summary>
	/// The root object of Terminal.Gui configuration settings / JSON schema.
	/// </summary>
	/// <example><code>
	///  {
	///    "$schema" : "https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json",
	///    "Settings": {
	///    },
	///    "Themes": {
	///    },
	///  },
	/// </code></example>
	public class ConfigRoot {
		/// <summary>
		/// Points to our JSON schema.
		/// </summary>
		[JsonInclude, JsonPropertyName ("$schema")]
		public string schema = "https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json";

		/// <summary>
		/// The Settings.
		/// </summary>
		[JsonInclude]
		public Settings Settings = new Settings ();

		/// <summary>
		/// The ColorSchemes.
		/// </summary>
		[JsonInclude]
		public Themes Themes = new Themes ();

		/// <summary>
		/// Applies the settings in each <see cref="Config{T}"/> object to the running <see cref="Application"/>.
		/// </summary>
		public void ApplyAll ()
		{
			Settings.Apply ();
			Themes.Apply ();
		}

		/// <summary>
		/// Updates the internal state of <see cref="ConfigRoot"/> from a newly read
		/// instance.
		/// </summary>
		/// <param name="newConfig"></param>
		/// <exception cref="NotImplementedException"></exception>
		internal void CopyUpdatedProperitesFrom (ConfigRoot newConfig)
		{
			Settings.CopyUpdatedProperitesFrom (newConfig.Settings);
			Themes.CopyUpdatedProperitesFrom (newConfig.Themes);
		}

		/// <summary>
		/// Retrives all hard coded default settings; used to generate the default config.json file
		/// during development. 
		/// </summary>
		internal void GetAllHardCodedDefaults ()
		{
			Settings = new Settings ();
			Settings.GetHardCodedDefaults ();
			Themes = new Themes ();
			Themes.GetHardCodedDefaults ();
		}
	}
}
