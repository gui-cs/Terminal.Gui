
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Terminal.Gui;
using static UICatalog.Scenario;
using Attribute = Terminal.Gui.Attribute;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Syntax Highlighting", Description: "Text editor with keyword highlighting")]
	[ScenarioCategory ("Controls")]
	class SyntaxHighlighting : Scenario {

			public override void Setup ()
			{
				Win.Title = this.GetName ();
				Win.Y = 1; // menu
				Win.Height = Dim.Fill (1); // status bar
				Top.LayoutSubviews ();

				var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_Quit", "", () => Quit()),
				})
				});
				Top.Add (menu);

				var textView = new SqlTextView () {
					X = 0,
					Y = 0,
					Width = Dim.Fill (),
					Height = Dim.Fill (1),
				};

				textView.Init();

				textView.Text = "SELECT TOP 100 * \nfrom\n MyDb.dbo.Biochemistry;";
				
				Win.Add (textView);

				var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Quit()),

			});


				Top.Add (statusBar);
			}


			private void Quit ()
			{
				Application.RequestStop ();
			}

		private class SqlTextView : TextView{

			private HashSet<string> keywords = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
			private Attribute blue;
			private Attribute white;
			private Attribute magenta;


			public void Init()
			{
				keywords.Add("select");
				keywords.Add("distinct");
				keywords.Add("top");
				keywords.Add("from");
				keywords.Add ("create");
				keywords.Add ("primary");
				keywords.Add ("key");
				keywords.Add ("insert");
				keywords.Add ("alter");
				keywords.Add ("add");
				keywords.Add ("update");
				keywords.Add ("set");
				keywords.Add ("delete");
				keywords.Add ("truncate");
				keywords.Add ("as");
				keywords.Add ("order");
				keywords.Add ("by");
				keywords.Add ("asc");
				keywords.Add ("desc");
				keywords.Add ("between");
				keywords.Add ("where");
				keywords.Add ("and");
				keywords.Add ("or");
				keywords.Add ("not");
				keywords.Add ("limit");
				keywords.Add ("null");
				keywords.Add ("is");
				keywords.Add ("drop");
				keywords.Add ("database");
				keywords.Add ("column");
				keywords.Add ("table");
				keywords.Add ("having");
				keywords.Add ("in");
				keywords.Add ("join");
				keywords.Add ("on");
				keywords.Add ("union");
				keywords.Add ("exists");

				magenta = Driver.MakeAttribute (Color.Magenta, Color.Black);
				blue = Driver.MakeAttribute (Color.Cyan, Color.Black);
				white = Driver.MakeAttribute (Color.White, Color.Black);
			}

			protected override void ColorNormal ()
			{
				Driver.SetAttribute (white);
			}

			protected override void ColorNormal (List<System.Rune> line, int idx)
			{
				if(IsInStringLiteral(line,idx)) {
					Driver.SetAttribute (magenta);
				}
				else
				if(IsKeyword(line,idx))
				{
					Driver.SetAttribute (blue);
				}
				else{
					Driver.SetAttribute (white);
				}
			}

			private bool IsInStringLiteral (List<System.Rune> line, int idx)
			{
				string strLine = new string (line.Select (r => (char)r).ToArray ());
				
				foreach(Match m in Regex.Matches(strLine, "'[^']*'")) {
					if(idx >= m.Index && idx < m.Index+m.Length) {
						return true;
					}
				}

				return false;
			}

			private bool IsKeyword(List<System.Rune> line, int idx)
			{
				var word = IdxToWord(line,idx);
				
				if(string.IsNullOrWhiteSpace(word)){
					return false;
				}

				return keywords.Contains(word,StringComparer.CurrentCultureIgnoreCase);
			}

			private string IdxToWord(List<System.Rune> line, int idx)
			{
				var words = Regex.Split(
					new string(line.Select(r=>(char)r).ToArray()),
					"\\b");


				int count = 0;
				string current = null;

				foreach(var word in words)
				{
					current = word;
					count+= word.Length;
					if(count > idx){
						break;
					}
				}

				return current?.Trim();
			}
		}
	}
}
