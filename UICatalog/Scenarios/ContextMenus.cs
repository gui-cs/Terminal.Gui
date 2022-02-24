using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "ContextMenus", Description: "Context Menu Sample")]
	[ScenarioCategory ("Controls")]
	public class ContextMenus : Scenario {
		ContextMenu contextMenu = new ContextMenu ();

		public override void Setup ()
		{
			var tf = new TextField ("Context Menu") {
				X = Pos.Center (),
				Y = Pos.Center (),
				Width = 30
			};
			Win.Add (tf);

			Win.KeyPress += (e) => {
				if (e.KeyEvent.Key == contextMenu.Key && !ContextMenu.IsShow) {
					ShowContextMenu (tf.Frame.X, tf.Frame.Bottom);
					e.Handled = true;
				}
			};

			Win.MouseClick += (e) => {
				if (e.MouseEvent.Flags == contextMenu.MouseFlags) {
					ShowContextMenu (e.MouseEvent.X, e.MouseEvent.Y);
					e.Handled = true;
				}
			};
		}

		private void ShowContextMenu (int x, int y)
		{
			contextMenu = new ContextMenu (x, y,
				new MenuBarItem (new MenuItem [] {
					new MenuItem ("_Configuration", "Show configuration", () => MessageBox.Query (50, 5, "Info", "This would open settings dialog", "Ok")),
					new MenuBarItem ("More options", new MenuItem [] {
						new MenuItem ("_Setup", "Change settings", () => MessageBox.Query (50, 5, "Info", "This would open setup dialog", "Ok")),
						new MenuItem ("_Maintenance", "Maintenance mode", () => MessageBox.Query (50, 5, "Info", "This would open maintenance dialog", "Ok")),
					}),
					null,
					new MenuItem ("_Quit", "", () => Application.RequestStop ())
				})
			);
			contextMenu.Show ();
		}
	}
}