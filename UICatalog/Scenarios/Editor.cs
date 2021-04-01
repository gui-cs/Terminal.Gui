using System;
using System.Text;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Editor", Description: "A Terminal.Gui Text Editor via TextView")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Dialogs")]
	[ScenarioCategory ("Text")]
	[ScenarioCategory ("Dialogs")]
	[ScenarioCategory ("TopLevel")]
	class Editor : Scenario {
		private string _fileName = "demo.txt";
		private TextView _textView;
		private bool _saved = true;
		private ScrollBarView _scrollBar;
		private byte [] _originalText;

		public override void Init (Toplevel top, ColorScheme colorScheme)
		{
			Application.Init ();
			Top = top;
			if (Top == null) {
				Top = Application.Top;
			}

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_New", "", () => New()),
					new MenuItem ("_Open", "", () => Open()),
					new MenuItem ("_Save", "", () => Save()),
					new MenuItem ("_Save As", "", () => SaveAs()),
					null,
					new MenuItem ("_Quit", "", () => Quit()),
				}),
				new MenuBarItem ("_Edit", new MenuItem [] {
					new MenuItem ("_Copy", "", () => Copy(),null,null, Key.CtrlMask | Key.C),
					new MenuItem ("C_ut", "", () => Cut(),null,null, Key.CtrlMask | Key.W),
					new MenuItem ("_Paste", "", () => Paste(),null,null, Key.CtrlMask | Key.Y)
				}),
				new MenuBarItem ("_ScrollBarView", CreateKeepChecked ()),
				new MenuBarItem ("_Cursor", new MenuItem [] {
					new MenuItem ("_Invisible", "", () => SetCursor(CursorVisibility.Invisible)),
					new MenuItem ("_Box", "", () => SetCursor(CursorVisibility.Box)),
					new MenuItem ("_Underline", "", () => SetCursor(CursorVisibility.Underline)),
					new MenuItem ("", "", () => {}, () => { return false; }),
					new MenuItem ("xTerm :", "", () => {}, () => { return false; }),
					new MenuItem ("", "", () => {}, () => { return false; }),
					new MenuItem ("  _Default", "", () => SetCursor(CursorVisibility.Default)),
					new MenuItem ("  _Vertical", "", () => SetCursor(CursorVisibility.Vertical)),
					new MenuItem ("  V_ertical Fix", "", () => SetCursor(CursorVisibility.VerticalFix)),
					new MenuItem ("  B_ox Fix", "", () => SetCursor(CursorVisibility.BoxFix)),
					new MenuItem ("  U_nderline Fix","", () => SetCursor(CursorVisibility.UnderlineFix))
				}),
				new MenuBarItem ("Forma_t", CreateWrapChecked ())
			});
			Top.Add (menu);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.F2, "~F2~ Open", () => Open()),
				new StatusItem(Key.F3, "~F3~ Save", () => Save()),
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Quit()),
			});
			Top.Add (statusBar);

			CreateDemoFile (_fileName);

			Win = new Window (_fileName ?? "Untitled") {
				X = 0,
				Y = 1,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				ColorScheme = colorScheme,
			};
			Top.Add (Win);

			_textView = new TextView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				BottomOffset = 1,
				RightOffset = 1
			};

			LoadFile ();

			Win.Add (_textView);

			_scrollBar = new ScrollBarView (_textView, true);

			_scrollBar.ChangedPosition += () => {
				_textView.TopRow = _scrollBar.Position;
				if (_textView.TopRow != _scrollBar.Position) {
					_scrollBar.Position = _textView.TopRow;
				}
				_textView.SetNeedsDisplay ();
			};

			_scrollBar.OtherScrollBarView.ChangedPosition += () => {
				_textView.LeftColumn = _scrollBar.OtherScrollBarView.Position;
				if (_textView.LeftColumn != _scrollBar.OtherScrollBarView.Position) {
					_scrollBar.OtherScrollBarView.Position = _textView.LeftColumn;
				}
				_textView.SetNeedsDisplay ();
			};

			_textView.DrawContent += (e) => {
				_scrollBar.Size = _textView.Lines;
				_scrollBar.Position = _textView.TopRow;
				if (_scrollBar.OtherScrollBarView != null) {
					_scrollBar.OtherScrollBarView.Size = _textView.Maxlength;
					_scrollBar.OtherScrollBarView.Position = _textView.LeftColumn;
				}
				_scrollBar.LayoutSubviews ();
				_scrollBar.Refresh ();
			};
		}

		public override void Setup ()
		{
		}

		private void New ()
		{
			if (!CanCloseFile ()) {
				return;
			}

			Win.Title = "Untitled.txt";
			_fileName = null;
			_originalText = new System.IO.MemoryStream ().ToArray ();
			_textView.Text = _originalText;
		}

		private void LoadFile ()
		{
			if (_fileName != null) {
				// BUGBUG: #452 TextView.LoadFile keeps file open and provides no way of closing it
				//_textView.LoadFile(_fileName);
				_textView.Text = System.IO.File.ReadAllText (_fileName);
				_originalText = _textView.Text.ToByteArray ();
				Win.Title = _fileName;
				_saved = true;
			}
		}

		private void Paste ()
		{
			if (_textView != null) {
				_textView.Paste ();
			}
		}

		private void Cut ()
		{
			if (_textView != null) {
				_textView.Cut ();
			}
		}

		private void Copy ()
		{
			if (_textView != null) {
				_textView.Copy ();
			}
		}

		private void SetCursor (CursorVisibility visibility)
		{
			_textView.DesiredCursorVisibility = visibility;
		}

		private bool CanCloseFile ()
		{
			if (_textView.Text == _originalText) {
				return true;
			}

			var r = MessageBox.ErrorQuery ("Save File",
				$"Do you want save changes in {Win.Title}?", "Yes", "No", "Cancel");
			if (r == 0) {
				return Save ();
			} else if (r == 1) {
				return true;
			}

			return false;
		}

		private void Open ()
		{
			if (!CanCloseFile ()) {
				return;
			}

			var d = new OpenDialog ("Open", "Open a file") { AllowsMultipleSelection = false };
			Application.Run (d);

			if (!d.Canceled) {
				_fileName = d.FilePaths [0];
				LoadFile ();
			}
		}

		private bool Save ()
		{
			if (_fileName != null) {
				// BUGBUG: #279 TextView does not know how to deal with \r\n, only \r 
				// As a result files saved on Windows and then read back will show invalid chars.
				return SaveFile (Win.Title.ToString (), _fileName);
			} else {
				return SaveAs ();
			}
		}

		private bool SaveAs ()
		{
			var sd = new SaveDialog ("Save file", "Choose the path where to save the file.");
			sd.FilePath = System.IO.Path.Combine (sd.FilePath.ToString (), Win.Title.ToString ());
			Application.Run (sd);

			if (!sd.Canceled) {
				if (System.IO.File.Exists (sd.FilePath.ToString ())) {
					if (MessageBox.Query ("Save File",
						"File already exists. Overwrite any way?", "No", "Ok") == 1) {
						return SaveFile (sd.FileName.ToString (), sd.FilePath.ToString ());
					} else {
						_saved = false;
						return _saved;
					}
				} else {
					return SaveFile (sd.FileName.ToString (), sd.FilePath.ToString ());
				}
			} else {
				_saved = false;
				return _saved;
			}
		}

		private bool SaveFile (string title, string file)
		{
			try {
				Win.Title = title;
				_fileName = file;
				System.IO.File.WriteAllText (_fileName, _textView.Text.ToString ());
				_saved = true;
				MessageBox.Query ("Save File", "File was successfully saved.", "Ok");

			} catch (Exception ex) {
				MessageBox.ErrorQuery ("Error", ex.Message, "Ok");
				return false;
			}

			return true;
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}

		private void CreateDemoFile (string fileName)
		{
			var sb = new StringBuilder ();
			// BUGBUG: #279 TextView does not know how to deal with \r\n, only \r
			sb.Append ("Hello world.\n");
			sb.Append ("This is a test of the Emergency Broadcast System.\n");

			for (int i = 0; i < 30; i++) {
				sb.Append ($"{i} - This is a test with a very long line and many lines to test the ScrollViewBar against the TextView. - {i}\n");
			}
			var sw = System.IO.File.CreateText (fileName);
			sw.Write (sb.ToString ());
			sw.Close ();
		}

		private MenuItem [] CreateKeepChecked ()
		{
			var item = new MenuItem ();
			item.Title = "Keep Content Always In Viewport";
			item.CheckType |= MenuItemCheckStyle.Checked;
			item.Checked = true;
			item.Action += () => _scrollBar.KeepContentAlwaysInViewport = item.Checked = !item.Checked;

			return new MenuItem [] { item };
		}

		private MenuItem [] CreateWrapChecked ()
		{
			var item = new MenuItem ();
			item.Title = "Word Wrap";
			item.CheckType |= MenuItemCheckStyle.Checked;
			item.Checked = false;
			item.Action += () => {
				_textView.WordWrap = item.Checked = !item.Checked;
				if (_textView.WordWrap) {
					_scrollBar.AutoHideScrollBars = false;
					_scrollBar.OtherScrollBarView.ShowScrollIndicator = false;
					_textView.BottomOffset = 0;
				} else {
					_scrollBar.AutoHideScrollBars = true;
					_textView.BottomOffset = 1;
				}
			};

			return new MenuItem [] { item };
		}

		public override void Run ()
		{
			base.Run ();
		}
	}
}
