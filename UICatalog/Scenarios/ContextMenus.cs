using System.Globalization;
using System.Threading;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "ContextMenus", Description: "Context Menu Sample")]
	[ScenarioCategory ("Controls")]
	public class ContextMenus : Scenario {
		ContextMenu contextMenu = new ContextMenu ();
		MenuItem miEn, miPt;
		bool isEn = true;

		public override void Setup ()
		{
			var text = "Context Menu";
			var width = 20;

			var tfTopLeft = new TextField (text) {
				Width = width
			};
			Win.Add (tfTopLeft);

			var tfTopRight = new TextField (text) {
				X = Pos.AnchorEnd (width),
				Width = width
			};
			Win.Add (tfTopRight);

			var tfMiddle = new TextField (text) {
				X = Pos.Center (),
				Y = Pos.Center (),
				Width = width
			};
			Win.Add (tfMiddle);

			var tfBottomLeft = new TextField (text) {
				Y = Pos.AnchorEnd (1),
				Width = width
			};
			Win.Add (tfBottomLeft);

			var tfBottomRight = new TextField (text) {
				X = Pos.AnchorEnd (width),
				Y = Pos.AnchorEnd (1),
				Width = width
			};
			Win.Add (tfBottomRight);

			Point mousePos = default;

			Win.KeyPress += (e) => {
				if (e.KeyEvent.Key == (Key.Space | Key.CtrlMask) && !ContextMenu.IsShow) {
					ShowContextMenu (mousePos.X, mousePos.Y);
					e.Handled = true;
				}
			};

			Win.MouseClick += (e) => {
				if (e.MouseEvent.Flags == contextMenu.MouseFlags) {
					ShowContextMenu (e.MouseEvent.X, e.MouseEvent.Y);
					e.Handled = true;
				}
				mousePos = new Point (e.MouseEvent.X, e.MouseEvent.Y);
			};

			Win.WantMousePositionReports = true;

			Top.Closed += (_) => Thread.CurrentThread.CurrentUICulture = new CultureInfo ("en-US");
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
					new MenuBarItem ("_Languages", new MenuItem [] {
						miEn = new MenuItem ("_English", "Default", () => {
							Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
							isEn = !isEn;
							miEn.Checked = isEn;
							miPt.Checked = !isEn;
						})
						{ CheckType = MenuItemCheckStyle.Checked, Checked = isEn },
						miPt = new MenuItem ("_Portuguese", "pt-PT", () => {
							Thread.CurrentThread.CurrentUICulture = new CultureInfo("pt-PT");
							isEn = !isEn;
							miEn.Checked = isEn;
							miPt.Checked = !isEn;
						})
						{ CheckType = MenuItemCheckStyle.Checked, Checked = !isEn },
					}),

					null,
					new MenuItem ("_Quit", "", () => Application.RequestStop ())
				})
			);
			contextMenu.Show ();
		}
	}
}