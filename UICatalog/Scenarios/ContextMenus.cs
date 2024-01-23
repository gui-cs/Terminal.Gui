using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata (Name: "ContextMenus", Description: "Context Menu Sample.")]
[ScenarioCategory ("Menus")]
public class ContextMenus : Scenario {
	private ContextMenu _contextMenu = new ContextMenu ();
	private readonly List<CultureInfo> _cultureInfos = Application.SupportedCultures;
	private MenuItem _miForceMinimumPosToZero;
	private bool _forceMinimumPosToZero = true;
	private TextField _tfTopLeft, _tfTopRight, _tfMiddle, _tfBottomLeft, _tfBottomRight;
	private MenuItem _miUseSubMenusSingleFrame;
	private bool _useSubMenusSingleFrame;

	public override void Setup ()
	{
		var text = "Context Menu";
		var width = 20;
		KeyCode winContextMenuKey = KeyCode.Space | KeyCode.CtrlMask;

		var label = new Label { X = Pos.Center(), Y = 1, Text = $"Press '{winContextMenuKey}' to open the Window context menu." };
		Win.Add (label);
		label = new Label {
			X = Pos.Center (),
			Y = Pos.Bottom (label),
			Text = $"Press '{ContextMenu.DefaultKey}' to open the TextField context menu."
		};
		Win.Add (label);

		_tfTopLeft = new TextField (text) {
			Width = width
		};
		Win.Add (_tfTopLeft);

		_tfTopRight = new TextField (text) {
			X = Pos.AnchorEnd (width),
			Width = width
		};
		Win.Add (_tfTopRight);

		_tfMiddle = new TextField (text) {
			X = Pos.Center (),
			Y = Pos.Center (),
			Width = width
		};
		Win.Add (_tfMiddle);

		_tfBottomLeft = new TextField (text) {
			Y = Pos.AnchorEnd (1),
			Width = width
		};
		Win.Add (_tfBottomLeft);

		_tfBottomRight = new TextField (text) {
			X = Pos.AnchorEnd (width),
			Y = Pos.AnchorEnd (1),
			Width = width
		};
		Win.Add (_tfBottomRight);

		Point mousePos = default;

		Win.KeyDown += (s, e) => {
			if (e.KeyCode == winContextMenuKey) {
				ShowContextMenu (mousePos.X, mousePos.Y);
				e.Handled = true;
			}
		};

		Win.MouseClick += (s, e) => {
			if (e.MouseEvent.Flags == _contextMenu.MouseFlags) {
				ShowContextMenu (e.MouseEvent.X, e.MouseEvent.Y);
				e.Handled = true;
			}
		};

		Application.MouseEvent += ApplicationMouseEvent;

		void ApplicationMouseEvent (object sender, MouseEventEventArgs a)
		{
			mousePos = new Point (a.MouseEvent.X, a.MouseEvent.Y);
		}

		Win.WantMousePositionReports = true;

		Application.Top.Closed += (s, e) => {
			Thread.CurrentThread.CurrentUICulture = new CultureInfo ("en-US");
			Application.MouseEvent -= ApplicationMouseEvent;
		};
	}

	private void ShowContextMenu (int x, int y)
	{
		_contextMenu = new ContextMenu (x, y,
			new MenuBarItem (new MenuItem [] {
					new MenuItem ("_Configuration", "Show configuration", () => MessageBox.Query (50, 5, "Info", "This would open settings dialog", "Ok")),
					new MenuBarItem ("More options", new MenuItem [] {
						new MenuItem ("_Setup", "Change settings", () => MessageBox.Query (50, 5, "Info", "This would open setup dialog", "Ok"), shortcut: KeyCode.T | KeyCode.CtrlMask),
						new MenuItem ("_Maintenance", "Maintenance mode", () => MessageBox.Query (50, 5, "Info", "This would open maintenance dialog", "Ok")),
					}),
					new MenuBarItem ("_Languages", GetSupportedCultures ()),
					_miForceMinimumPosToZero = new MenuItem ("ForceMinimumPosToZero", "", () => {
						_miForceMinimumPosToZero.Checked = _forceMinimumPosToZero = !_forceMinimumPosToZero;
						_tfTopLeft.ContextMenu.ForceMinimumPosToZero = _forceMinimumPosToZero;
						_tfTopRight.ContextMenu.ForceMinimumPosToZero = _forceMinimumPosToZero;
						_tfMiddle.ContextMenu.ForceMinimumPosToZero = _forceMinimumPosToZero;
						_tfBottomLeft.ContextMenu.ForceMinimumPosToZero = _forceMinimumPosToZero;
						_tfBottomRight.ContextMenu.ForceMinimumPosToZero = _forceMinimumPosToZero;
					}) { CheckType = MenuItemCheckStyle.Checked, Checked = _forceMinimumPosToZero },
					_miUseSubMenusSingleFrame = new MenuItem ("Use_SubMenusSingleFrame", "",
						() => _contextMenu.UseSubMenusSingleFrame = (bool)(_miUseSubMenusSingleFrame.Checked = _useSubMenusSingleFrame = !_useSubMenusSingleFrame)) {
							CheckType = MenuItemCheckStyle.Checked, Checked = _useSubMenusSingleFrame
						},
					null,
					new MenuItem ("_Quit", "", () => Application.RequestStop ())
			})
		) { ForceMinimumPosToZero = _forceMinimumPosToZero, UseSubMenusSingleFrame = _useSubMenusSingleFrame };

		_tfTopLeft.ContextMenu.ForceMinimumPosToZero = _forceMinimumPosToZero;
		_tfTopRight.ContextMenu.ForceMinimumPosToZero = _forceMinimumPosToZero;
		_tfMiddle.ContextMenu.ForceMinimumPosToZero = _forceMinimumPosToZero;
		_tfBottomLeft.ContextMenu.ForceMinimumPosToZero = _forceMinimumPosToZero;
		_tfBottomRight.ContextMenu.ForceMinimumPosToZero = _forceMinimumPosToZero;

		_contextMenu.Show ();
	}

	private MenuItem [] GetSupportedCultures ()
	{
		List<MenuItem> supportedCultures = new List<MenuItem> ();
		var index = -1;

		foreach (var c in _cultureInfos) {
			var culture = new MenuItem {
				CheckType = MenuItemCheckStyle.Checked
			};
			if (index == -1) {
				culture.Title = "_English";
				culture.Help = "en-US";
				culture.Checked = Thread.CurrentThread.CurrentUICulture.Name == "en-US";
				CreateAction (supportedCultures, culture);
				supportedCultures.Add (culture);
				index++;
				culture = new MenuItem {
					CheckType = MenuItemCheckStyle.Checked
				};
			}
			culture.Title = $"_{c.Parent.EnglishName}";
			culture.Help = c.Name;
			culture.Checked = Thread.CurrentThread.CurrentUICulture.Name == c.Name;
			CreateAction (supportedCultures, culture);
			supportedCultures.Add (culture);
		}
		return supportedCultures.ToArray ();

		void CreateAction (List<MenuItem> supportedCultures, MenuItem culture)
		{
			culture.Action += () => {
				Thread.CurrentThread.CurrentUICulture = new CultureInfo (culture.Help);
				culture.Checked = true;
				foreach (var item in supportedCultures) {
					item.Checked = item.Help == Thread.CurrentThread.CurrentUICulture.Name;
				}
			};
		}
	}
}
