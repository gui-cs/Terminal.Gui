using System;
using System.Collections.Generic;
using System.Data;
using Terminal.Gui;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Text;
using NStack;
using System.Text.RegularExpressions;
using Terminal.Gui.Graphs;
using System.Threading;
using System.Threading.Tasks;

namespace UICatalog.Scenarios {

	[ScenarioMetadata (Name: "Auto Scrolling Text", Description: "Demonstrates async update and auto scrolling.")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Text and Formatting")]
	public class AutoScrollingChat : Scenario 
	{
        const string Words = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum";
		const string Users = "Bot232 Frank Mary Harley";
		private TextView textView;

		CancellationTokenSource cts = new CancellationTokenSource();


		public override void Setup ()
		{
			Win.Title = this.GetName();
			textView = new TextView {
				Width = Dim.Fill(),
				Height = Dim.Fill(2),
				ReadOnly = true
			};
			
			var line = new LineView(Orientation.Horizontal){
				Y = Pos.AnchorEnd(2)
			};

			Win.Add(textView);
			Win.Add(line);

			var textfield = new TextField(){
				Y = Pos.AnchorEnd(1),
				Width = Dim.Fill(),
				X = 10
			};

			// when user presses enter in text box
			textfield.KeyPress += (e)=>
			{
				if(e.KeyEvent.Key == Key.Enter){
					// post the chat message
					AddChatEntry("You",textfield.Text.ToString());
					textfield.Text = "";
					GoToEndOfTextView();
					e.Handled = true;
				}
			};

			var lbl = new Label("Message:"){
				Y = Pos.AnchorEnd(1),
				Width = Dim.Fill(),
				X = 0
			};
			Win.Add(lbl);
			Win.Add(textfield);

			// start generating async chat messages
			Task.Run(GenerateChatLogs);

			// when view is closed, stop generating text messages
			Win.Closing += (e)=>cts.Cancel();
		}

		private void GenerateChatLogs()
		{
			while(!cts.IsCancellationRequested)
			{
				AddChatEntry(GetRandomUser(),GetRandomSentence());
				Task.Delay(2000,cts.Token).Wait(cts.Token);
			}
		}

		private void AddChatEntry (string user, string sentence)
		{
			// schedule on the UI thread
			Application.MainLoop.Invoke(
				()=>
				{
					// autoscroll unless the user has manually scrolled up to look at history
					var autoscroll =
					 textView.CurrentRow >= textView.Lines-1;

					// append the chat log
					textView.AppendText($"\n{user}: {sentence}",false);
										
					if(autoscroll)
					{
						GoToEndOfTextView();
					}
				}
			);
		}

		private void GoToEndOfTextView ()
		{			
			textView.MoveEnd();
			textView.MoveStartOfLine();
		}

		private string GetRandomSentence ()
		{
			var r = new Random();
			var words = Words.Split(' ');

			var sentenceLength = r.Next(60);
			var sentence = new StringBuilder();

			for(int i=0 ;i <sentenceLength;i++)
			{
				sentence.Append(words[r.Next(words.Length)] + " ");
			}

			return sentence.ToString().TrimEnd();
		}

		private string GetRandomUser ()
		{
			var r = new Random();
			var u = Users.Split(' ');
			return u[r.Next(u.Length)];
		}
	}
}