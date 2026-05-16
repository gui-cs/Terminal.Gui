using Terminal.Gui.App;

Application.AppModel = AppModel.StatusLine;

using IApplication app = Application.Create ();
app.Init ();
app.Run<StatusLineDemo> ();
