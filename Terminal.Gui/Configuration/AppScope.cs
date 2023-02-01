﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using static Terminal.Gui.Configuration.ConfigurationManager;

#nullable enable

namespace Terminal.Gui.Configuration {

	public static partial class ConfigurationManager {
		/// <summary>
		/// The <see cref="Scope"/> class for application-defined configuration settings.
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
		public class AppScope : Scope {

			/// <summary>
			/// Constructs a new instance.
			/// </summary>
			/// <exception cref="InvalidOperationException">Thrown if a property tries to omit the class name it its name.</exception>
			public AppScope () : base ()
			{
				//if (Properties.Any (p => (p.Value.PropertyInfo!.GetCustomAttribute (typeof (SerializableConfigurationProperty)) as SerializableConfigurationProperty)!.OmitClassName)) {
				//	throw new InvalidOperationException ("AppScope property names must not omit the classname.");
				//}
			}
		}
	}
}