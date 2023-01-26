using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;


#nullable enable

namespace Terminal.Gui.Configuration {
	public static partial class ConfigurationManager {
		/// <summary>
		/// Defines a configuration settings scope. Classes that inherit from this abstract class can be used to define
		/// scopes for configuration settings. Each scope is a JSON object that contains a set of configuration settings.
		/// </summary>
		public abstract class Scope : IDictionary<string, ConfigProperty> {
			/// <summary>
			/// Crates a new instance.
			/// </summary>
			public Scope ()
			{
				ConfigurationManager._allConfigProperties ??= getConfigProperties ();
				var props = ConfigurationManager._allConfigProperties.Where (cp =>
					(cp.Value.PropertyInfo?.GetCustomAttribute (typeof (SerializableConfigurationProperty))
					as SerializableConfigurationProperty)?.Scope == this.GetType ());
				Properties = props.ToDictionary (dict => dict.Key,
					dict => new ConfigProperty () { PropertyInfo = dict.Value.PropertyInfo, PropertyValue = null }, StringComparer.InvariantCultureIgnoreCase);
			}

			/// <summary>
			/// Gets the dictionary of <see cref="ConfigProperty"/> objects for this scope.
			/// </summary>
			/// <remarks>
			/// This dictionary is populated in the constructor of the <see cref="Scope"/> class with the properties
			/// attributed with the <see cref="SerializableConfigurationProperty"/> attribute 
			/// and whose <see cref="SerializableConfigurationProperty.Scope"/> 
			/// is the same as the type of this scope.
			/// </remarks>
			[JsonIgnore]
			public Dictionary<string, ConfigProperty> Properties { get; set; }

			// We are derived from IDictionary so that the ColorSchemes JSON element 
			// has a list of ColorScheme objects. 
			#region IDictionary			
			/// <inheritdoc/>
			public ICollection<string> Keys => ((IDictionary<string, ConfigProperty>)Properties).Keys;
			/// <inheritdoc/>
			public ICollection<ConfigProperty> Values => ((IDictionary<string, ConfigProperty>)Properties).Values;
			/// <inheritdoc/>
			public int Count => ((ICollection<KeyValuePair<string, ConfigProperty>>)Properties).Count;
			/// <inheritdoc/>
			public bool IsReadOnly => ((ICollection<KeyValuePair<string, ConfigProperty>>)Properties).IsReadOnly;

			/// <inheritdoc/>
			[JsonIgnore]
			public ConfigProperty this [string index] {
				get {
					return Properties [index];
				}
				set {
					Properties [index] = value;
				}
			}
			
			/// <inheritdoc/>
			public void Add (string key, ConfigProperty value)
			{
				((IDictionary<string, ConfigProperty>)Properties).Add (key, value);
			}
			/// <inheritdoc/>
			public bool ContainsKey (string key)
			{
				return ((IDictionary<string, ConfigProperty>)Properties).ContainsKey (key);
			}
			/// <inheritdoc/>
			public bool Remove (string key)
			{
				return ((IDictionary<string, ConfigProperty>)Properties).Remove (key);
			}
			/// <inheritdoc/>
			public bool TryGetValue (string key, out ConfigProperty value)
			{
				return ((IDictionary<string, ConfigProperty>)Properties).TryGetValue (key, out value!);
			}
			/// <inheritdoc/>
			public void Add (KeyValuePair<string, ConfigProperty> item)
			{
				((ICollection<KeyValuePair<string, ConfigProperty>>)Properties).Add (item);
			}
			/// <inheritdoc/>
			public void Clear ()
			{
				((ICollection<KeyValuePair<string, ConfigProperty>>)Properties).Clear ();
			}
			/// <inheritdoc/>
			public bool Contains (KeyValuePair<string, ConfigProperty> item)
			{
				return ((ICollection<KeyValuePair<string, ConfigProperty>>)Properties).Contains (item);
			}
			/// <inheritdoc/>
			public void CopyTo (KeyValuePair<string, ConfigProperty> [] array, int arrayIndex)
			{
				((ICollection<KeyValuePair<string, ConfigProperty>>)Properties).CopyTo (array, arrayIndex);
			}
			/// <inheritdoc/>
			public bool Remove (KeyValuePair<string, ConfigProperty> item)
			{
				return ((ICollection<KeyValuePair<string, ConfigProperty>>)Properties).Remove (item);
			}
			/// <inheritdoc/>
			public IEnumerator<KeyValuePair<string, ConfigProperty>> GetEnumerator ()
			{
				return ((IEnumerable<KeyValuePair<string, ConfigProperty>>)Properties).GetEnumerator ();
			}

			IEnumerator IEnumerable.GetEnumerator ()
			{
				return ((IEnumerable)Properties).GetEnumerator ();
			}
			#endregion
		}
	}
}
