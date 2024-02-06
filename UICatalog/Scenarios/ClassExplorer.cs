using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Class Explorer", "Tree view explorer for classes by namespace based on TreeView.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("TreeView")]
public class ClassExplorer : Scenario {
	MenuItem _highlightModelTextOnly;
	MenuItem _miShowPrivate;
	TextView _textView;
	TreeView<object> _treeView;

	public override void Setup ()
	{
		Win.Title = GetName ();
		Win.Y = 1; // menu
		Win.Height = Dim.Fill (1); // status bar

		var menu = new MenuBar {
			Menus = [
				new MenuBarItem ("_File", new MenuItem [] {
					new("_Quit", "", () => Quit ())
				}),
				new MenuBarItem ("_View", new [] {
					_miShowPrivate = new MenuItem ("_Include Private", "", () => ShowPrivate ()) {
						Checked = false,
						CheckType = MenuItemCheckStyle.Checked
					},
					new("_Expand All", "", () => _treeView.ExpandAll ()),
					new("_Collapse All", "", () => _treeView.CollapseAll ())
				}),
				new MenuBarItem ("_Style", new [] {
					_highlightModelTextOnly = new MenuItem ("_Highlight Model Text Only", "",
						() => OnCheckHighlightModelTextOnly ()) {
						CheckType = MenuItemCheckStyle.Checked
					}
				})
			]
		};
		Application.Top.Add (menu);

		_treeView = new TreeView<object> {
			X = 0,
			Y = 1,
			Width = Dim.Percent (50),
			Height = Dim.Fill ()
		};

		var lblSearch = new Label { Text = "Search" };
		var tfSearch = new TextField {
			Width = 20,
			X = Pos.Right (lblSearch)
		};

		Win.Add (lblSearch);
		Win.Add (tfSearch);

		var filter = new TreeViewTextFilter<object> (_treeView);
		_treeView.Filter = filter;
		tfSearch.TextChanged += (s, e) => {
			filter.Text = tfSearch.Text;
			if (_treeView.SelectedObject != null) {
				_treeView.EnsureVisible (_treeView.SelectedObject);
			}
		};

		_treeView.AddObjects (AppDomain.CurrentDomain.GetAssemblies ());
		_treeView.AspectGetter = GetRepresentation;
		_treeView.TreeBuilder = new DelegateTreeBuilder<object> (ChildGetter, CanExpand);
		_treeView.SelectionChanged += TreeView_SelectionChanged;

		Win.Add (_treeView);

		_textView = new TextView {
			X = Pos.Right (_treeView),
			Y = 0,
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};

		Win.Add (_textView);
	}

	void OnCheckHighlightModelTextOnly ()
	{
		_treeView.Style.HighlightModelTextOnly = !_treeView.Style.HighlightModelTextOnly;
		_highlightModelTextOnly.Checked = _treeView.Style.HighlightModelTextOnly;
		_treeView.SetNeedsDisplay ();
	}

	void ShowPrivate ()
	{
		_miShowPrivate.Checked = !_miShowPrivate.Checked;
		_treeView.RebuildTree ();
		_treeView.SetFocus ();
	}

	BindingFlags GetFlags ()
	{
		if (_miShowPrivate.Checked == true) {
			return BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
			       BindingFlags.NonPublic;
		}

		return BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
	}

	void TreeView_SelectionChanged (object sender, SelectionChangedEventArgs<object> e)
	{
		var val = e.NewValue;
		var all = _treeView.GetAllSelectedObjects ().ToArray ();

		if (val == null || val is ShowForType) {
			return;
		}

		try {
			if (all.Length > 1) {
				_textView.Text = all.Length + " Objects";
			} else {
				var sb = new StringBuilder ();

				// tell the user about the currently selected tree node
				sb.AppendLine (e.NewValue.GetType ().Name);

				if (val is Assembly ass) {
					sb.AppendLine ($"Location:{ass.Location}");
					sb.AppendLine ($"FullName:{ass.FullName}");
				}

				if (val is PropertyInfo p) {
					sb.AppendLine ($"Name:{p.Name}");
					sb.AppendLine ($"Type:{p.PropertyType}");
					sb.AppendLine ($"CanWrite:{p.CanWrite}");
					sb.AppendLine ($"CanRead:{p.CanRead}");
				}

				if (val is FieldInfo f) {
					sb.AppendLine ($"Name:{f.Name}");
					sb.AppendLine ($"Type:{f.FieldType}");
				}

				if (val is EventInfo ev) {
					sb.AppendLine ($"Name:{ev.Name}");
					sb.AppendLine ("Parameters:");
					foreach (var parameter in ev.EventHandlerType.GetMethod ("Invoke")
							 .GetParameters ()) {
						sb.AppendLine ($"  {parameter.ParameterType} {parameter.Name}");
					}
				}

				if (val is MethodInfo method) {
					sb.AppendLine ($"Name:{method.Name}");
					sb.AppendLine ($"IsPublic:{method.IsPublic}");
					sb.AppendLine ($"IsStatic:{method.IsStatic}");
					sb.AppendLine ($"Parameters:{(method.GetParameters ().Any () ? "" : "None")}");
					foreach (var parameter in method.GetParameters ()) {
						sb.AppendLine ($"  {parameter.ParameterType} {parameter.Name}");
					}
				}

				if (val is ConstructorInfo ctor) {
					sb.AppendLine ($"Name:{ctor.Name}");
					sb.AppendLine ($"Parameters:{(ctor.GetParameters ().Any () ? "" : "None")}");
					foreach (var parameter in ctor.GetParameters ()) {
						sb.AppendLine ($"  {parameter.ParameterType} {parameter.Name}");
					}
				}

				_textView.Text = sb.ToString ().Replace ("\r\n", "\n");
			}
		} catch (Exception ex) {
			_textView.Text = ex.Message;
		}

		_textView.SetNeedsDisplay ();
	}

	bool CanExpand (object arg) => arg is Assembly || arg is Type || arg is ShowForType;

	IEnumerable<object> ChildGetter (object arg)
	{
		try {
			if (arg is Assembly a) {
				return a.GetTypes ();
			}

			if (arg is Type t) {
				// Note that here we cannot simply return the enum values as the same object cannot appear under multiple branches
				return Enum.GetValues (typeof (Showable))
					.Cast<Showable> ()
					// Although we new the Type every time the delegate is called state is preserved because the class has appropriate equality members
					.Select (v => new ShowForType (v, t));
			}

			if (arg is ShowForType show) {
				switch (show.ToShow) {
				case Showable.Properties:
					return show.Type.GetProperties (GetFlags ());
				case Showable.Constructors:
					return show.Type.GetConstructors (GetFlags ());
				case Showable.Events:
					return show.Type.GetEvents (GetFlags ());
				case Showable.Fields:
					return show.Type.GetFields (GetFlags ());
				case Showable.Methods:
					return show.Type.GetMethods (GetFlags ());
				}
			}
		} catch (Exception) {
			return Enumerable.Empty<object> ();
		}

		return Enumerable.Empty<object> ();
	}

	string GetRepresentation (object model)
	{
		try {
			if (model is Assembly ass) {
				return ass.GetName ().Name;
			}

			if (model is PropertyInfo p) {
				return p.Name;
			}

			if (model is FieldInfo f) {
				return f.Name;
			}

			if (model is EventInfo ei) {
				return ei.Name;
			}
		} catch (Exception ex) {
			return ex.Message;
		}

		return model.ToString ();
	}

	void Quit () => Application.RequestStop ();

	enum Showable {
		Properties,
		Fields,
		Events,
		Constructors,
		Methods
	}

	class ShowForType {
		public ShowForType (Showable toShow, Type type)
		{
			ToShow = toShow;
			Type = type;
		}

		public Type Type { get; }
		public Showable ToShow { get; }

		// Make sure to implement Equals methods on your objects if you intend to return new instances every time in ChildGetter
		public override bool Equals (object obj) =>
			obj is ShowForType type &&
			EqualityComparer<Type>.Default.Equals (Type, type.Type) &&
			ToShow == type.ToShow;

		public override int GetHashCode () => HashCode.Combine (Type, ToShow);

		public override string ToString () => ToShow.ToString ();
	}
}