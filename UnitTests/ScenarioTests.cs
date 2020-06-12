using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using UICatalog;
using Xunit;

// Alais Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui {
	public class ScenarioTests {
		int CreateInput (string input)
		{
			// Put a control-q in at the end
			Console.MockKeyPresses.Push (new ConsoleKeyInfo ('q', ConsoleKey.Q, shift: false, alt: false, control: true));
			foreach (var c in input.Reverse ()) {
				if (char.IsLetter (c)) {
					Console.MockKeyPresses.Push (new ConsoleKeyInfo (char.ToLower (c), (ConsoleKey)char.ToUpper (c), shift: char.IsUpper (c), alt: false, control: false));
				} else {
					Console.MockKeyPresses.Push (new ConsoleKeyInfo (c, (ConsoleKey)c, shift: false, alt: false, control: false));
				}
			}
			return Console.MockKeyPresses.Count;
		}

		/// <summary>
		/// This runs through all Sceanrios defined in UI Catalog, calling Init, Setup, and Run.
		/// It puts a Ctrl-Q in the input queue so the Scenario immediately exits. 
		/// Should find any egregious regressions.
		/// </summary>
		[Fact]
		public void Run_All_Sceanrios ()
		{
			List<Type> scenarioClasses = Scenario.GetDerivedClasses<Scenario> ();
			Assert.NotEmpty (scenarioClasses);

			foreach (var scenarioClass in scenarioClasses) {
				// Setup some fake kepresses 
				// Passing empty string will cause just a ctrl-q to be fired
				int stackSize = CreateInput ("");
				int iterations = 0;
				Application.Iteration = () => {
					iterations++;
					// Stop if we run out of control...
					if (iterations > 10) {
						Application.RequestStop ();
					}
				};
				Application.Init (new FakeDriver (), new NetMainLoop (() => FakeConsole.ReadKey (true)));

				var scenario = (Scenario)Activator.CreateInstance (scenarioClass);
				scenario.Init (Application.Top, Colors.Base);
				scenario.Setup ();
				scenario.Run ();

				Application.Shutdown ();

				// # of key up events should match # of iterations
				Assert.Equal (1, iterations);
				Assert.Equal (stackSize, iterations);
			}
		}
	}
}
