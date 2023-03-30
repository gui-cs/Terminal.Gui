﻿using System.Text.Json.Serialization;

#nullable enable

namespace Terminal.Gui {

	public static partial class ConfigurationManager {
		/// <summary>
		/// The <see cref="Scope{T}"/> class for application-defined configuration settings.
		/// </summary>
		/// <remarks>
		/// </remarks>
		/// <example>
		/// <para>
		/// Use the <see cref="SerializableConfigurationProperty"/> attribute to mark properties that should be serialized as part
		/// of application-defined configuration settings.
		/// </para>
		/// <code>
		/// public class MyAppSettings {
		///	[SerializableConfigurationProperty (Scope = typeof (AppScope))]
		///	public static bool? MyProperty { get; set; } = true;
		/// }
		/// </code>
		/// <para>
		/// THe resultant Json will look like this:
		/// </para>
		/// <code>
		///   "AppSettings": {
		///     "MyAppSettings.MyProperty": true,
		///     "UICatalog.ShowStatusBar": true
		///   },
		/// </code>
		/// </example> 
		[JsonConverter (typeof (ScopeJsonConverter<AppScope>))]
		public class AppScope : Scope<AppScope> {
		}
	}
}