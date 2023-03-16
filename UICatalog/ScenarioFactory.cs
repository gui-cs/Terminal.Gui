using System;
using System.Collections.Generic;
using UICatalog.Scenarios;

namespace UICatalog {
	internal class ScenarioFactory {

		Dictionary<Type, Func<Scenario>> scenarios = new Dictionary<Type, Func<Scenario>>();

		public ScenarioFactory ()
		{
			scenarios.Add (typeof (TableEditor), ()=>new TableEditor ());
		}

		internal Scenario Create (Type type)
		{
			return scenarios [type].Invoke ();
		}

		internal IEnumerable<Type> GetScenarioTypes ()
		{
			return scenarios.Keys;
		}
	}
}