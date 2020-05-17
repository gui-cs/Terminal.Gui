# gui.cs UI Catalog

UI Catalog is a comprehensive sample library for gui.cs. It attempts to satisfy the following goals:

1. Be an easy to use showcase for gui.cs concepts and features.
2. Provide sample code that illustrates how to properly implement said concepts & features.
3. Make it easy for contributors to add additional samples in a structured way.

![screenshot](screenshot.png)

## Motivation

The original `demo.cs` sample app for gui.cs is neither good to showcase, nor does it explain different concepts. In addition, because it is built on a single source file, it has proven to cause friction when multiple contributors are simultaneously working on different aspects of gui.cs. See [Issue #368](https://github.com/migueldeicaza/gui.cs/issues/368) for more background.

## How To Use

`Program.cs` is the main app and provides a UI for selecting and running **Scenarios**. Each **Scenario* is implemented as a class derived from `Scenario` and `Program.cs` uses reflection to dynamically build the UI.

**Scenarios** are tagged with categories using the `[ScenarioCategory]` attribute. The left pane of the main screen lists the categories. Clicking on a category shows all the scenarios in that category.

**Scenarios** can be run either from the **UICatalog.exe** app UI or by being specified on the command line:

```
UICatalog.exe <Scenario Name>
```

e.g.

```
UICatalog.exe Buttons
```

## Contributing

To add a new **Scenario** simply:

1. Create a new `.cs` file in the `Scenarios` directory that derives from `Scenario`.
2. Add a `[ScenarioMetaData()]` attribute to the class specifying the scenario's name and description.
3. Add one or more `[ScenarioCategory]` attributes to the class specifying which categories the sceanrio belongs to. If you don't specify a category the sceanrio will show up in "All".
3. implement the `Run` method which will be called when a user selects the scenario to run.

```csharp
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "MyScenario", Description: "<description>")]
	[ScenarioCategory ("Controls")]
	class MyScenario : Scenario {
		public override void Run () {
			var top = new Toplevel ();
			var win = new Window ($"ESC to Close - Scenario: {GetName()}") {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			top.Add (win);
			
			// Put your scenario code here

			Application.Run (top);
		}
	}
}

```
