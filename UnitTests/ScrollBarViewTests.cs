using System;
using Xunit;

namespace Terminal.Gui {
	public class ScrollBarViewTests {
		public class HostView : View {
			public int Top { get; set; }
			public int Lines { get; set; }
			public int Left { get; set; }
			public int Cols { get; set; }
		}

		private HostView _hostView;
		private ScrollBarView _scrollBar;
		private bool _added;

		public ScrollBarViewTests ()
		{
			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var top = Application.Top;

			_hostView = new HostView () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Top = 0,
				Lines = 30,
				Left = 0,
				Cols = 100
			};

			top.Add (_hostView);
		}

		private void AddHandlers ()
		{
			if (!_added) {
				_hostView.DrawContent += _hostView_DrawContent;
				_scrollBar.ChangedPosition += _scrollBar_ChangedPosition;
				_scrollBar.OtherScrollBarView.ChangedPosition += _scrollBar_OtherScrollBarView_ChangedPosition;
			}
			_added = true;
		}

		private void RemoveHandlers ()
		{
			if (_added) {
				_hostView.DrawContent -= _hostView_DrawContent;
				_scrollBar.ChangedPosition -= _scrollBar_ChangedPosition;
				_scrollBar.OtherScrollBarView.ChangedPosition -= _scrollBar_OtherScrollBarView_ChangedPosition;
			}
			_added = false;
		}

		private void _hostView_DrawContent (Rect obj)
		{
			_scrollBar.Size = _hostView.Lines;
			_scrollBar.Position = _hostView.Top;
			_scrollBar.OtherScrollBarView.Size = _hostView.Cols;
			_scrollBar.OtherScrollBarView.Position = _hostView.Left;
			_scrollBar.ColorScheme = _scrollBar.OtherScrollBarView.ColorScheme = _hostView.ColorScheme;
			_scrollBar.Refresh ();
		}

		private void _scrollBar_ChangedPosition ()
		{
			_hostView.Top = _scrollBar.Position;
			if (_hostView.Top != _scrollBar.Position) {
				_scrollBar.Position = _hostView.Top;
			}
			_hostView.SetNeedsDisplay ();
		}

		private void _scrollBar_OtherScrollBarView_ChangedPosition ()
		{
			_hostView.Left = _scrollBar.OtherScrollBarView.Position;
			if (_hostView.Left != _scrollBar.OtherScrollBarView.Position) {
				_scrollBar.OtherScrollBarView.Position = _hostView.Left;
			}
			_hostView.SetNeedsDisplay ();
		}

		[Fact]
		public void Hosting_A_Null_View_To_A_ScrollBarView_Throws_ArgumentNullException ()
		{
			Assert.Throws<ArgumentNullException> ("The host parameter can't be null.",
				() => new ScrollBarView (null, true));
			Assert.Throws<ArgumentNullException> ("The host parameter can't be null.",
				() => new ScrollBarView (null, false));
		}

		[Fact]
		public void Hosting_A_Null_SuperView_View_To_A_ScrollBarView_Throws_ArgumentNullException ()
		{
			Assert.Throws<ArgumentNullException> ("The host SuperView parameter can't be null.",
				() => new ScrollBarView (new View (), true));
			Assert.Throws<ArgumentNullException> ("The host SuperView parameter can't be null.",
				() => new ScrollBarView (new View (), false));
		}

		[Fact]
		public void Hosting_Two_Vertical_ScrollBarView_Throws_ArgumentException ()
		{
			var top = new Toplevel ();
			var host = new View ();
			top.Add (host);
			var v = new ScrollBarView (host, true);
			var h = new ScrollBarView (host, true);

			Assert.Throws<ArgumentException> (null, () => v.OtherScrollBarView = h);
			Assert.Throws<ArgumentException> (null, () => h.OtherScrollBarView = v);
		}

		[Fact]
		public void Scrolling_With_Default_Constructor_Do_Not_Scroll ()
		{
			var sbv = new ScrollBarView {
				Position = 1
			};
			Assert.NotEqual (1, sbv.Position);
			Assert.Equal (0, sbv.Position);
		}

		[Fact]
		public void Hosting_Two_Horizontal_ScrollBarView_Throws_ArgumentException ()
		{
			var top = new Toplevel ();
			var host = new View ();
			top.Add (host);
			var v = new ScrollBarView (host, false);
			var h = new ScrollBarView (host, false);

			Assert.Throws<ArgumentException> (null, () => v.OtherScrollBarView = h);
			Assert.Throws<ArgumentException> (null, () => h.OtherScrollBarView = v);
		}

		[Fact]
		public void Hosting_A_View_To_A_ScrollBarView ()
		{
			RemoveHandlers ();

			_scrollBar = new ScrollBarView (_hostView, true);
			_scrollBar.OtherScrollBarView = new ScrollBarView (_hostView, false);
			_scrollBar.OtherScrollBarView = _scrollBar.OtherScrollBarView;
			_scrollBar.OtherScrollBarView.OtherScrollBarView = _scrollBar;

			Assert.True (_scrollBar.IsVertical);
			Assert.False (_scrollBar.OtherScrollBarView.IsVertical);

			Assert.Equal (_scrollBar.Position, _hostView.Top);
			Assert.NotEqual (_scrollBar.Size, _hostView.Lines);
			Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
			Assert.NotEqual (_scrollBar.OtherScrollBarView.Size, _hostView.Cols);

			AddHandlers ();
			_hostView.SuperView.LayoutSubviews ();
			_hostView.Redraw (_hostView.Bounds);

			Assert.Equal (_scrollBar.Position, _hostView.Top);
			Assert.Equal (_scrollBar.Size, _hostView.Lines);
			Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
			Assert.Equal (_scrollBar.OtherScrollBarView.Size, _hostView.Cols);
		}

		[Fact]
		public void ChangedPosition_Update_The_Hosted_View ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			_scrollBar.Position = 2;
			Assert.Equal (_scrollBar.Position, _hostView.Top);

			_scrollBar.OtherScrollBarView.Position = 5;
			Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
		}

		[Fact]
		public void ChangedPosition_Scrolling ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			for (int i = 0; i < _scrollBar.Size; i++) {
				_scrollBar.Position += 1;
				Assert.Equal (_scrollBar.Position, _hostView.Top);
			}
			for (int i = _scrollBar.Size - 1; i >= 0; i--) {
				_scrollBar.Position -= 1;
				Assert.Equal (_scrollBar.Position, _hostView.Top);
			}

			for (int i = 0; i < _scrollBar.OtherScrollBarView.Size; i++) {
				_scrollBar.OtherScrollBarView.Position += i;
				Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
			}
			for (int i = _scrollBar.OtherScrollBarView.Size - 1; i >= 0; i--) {
				_scrollBar.OtherScrollBarView.Position -= 1;
				Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
			}
		}

		[Fact]
		public void ChangedPosition_Negative_Value ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			_scrollBar.Position = -20;
			Assert.Equal (0, _scrollBar.Position);
			Assert.Equal (_scrollBar.Position, _hostView.Top);

			_scrollBar.OtherScrollBarView.Position = -50;
			Assert.Equal (0, _scrollBar.OtherScrollBarView.Position);
			Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
		}

		[Fact]
		public void DrawContent_Update_The_ScrollBarView_Position ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			_hostView.Top = 3;
			_hostView.Redraw (_hostView.Bounds);
			Assert.Equal (_scrollBar.Position, _hostView.Top);

			_hostView.Left = 6;
			_hostView.Redraw (_hostView.Bounds);
			Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
		}

		[Fact]
		public void OtherScrollBarView_Not_Null ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			Assert.Equal (_scrollBar.OtherScrollBarView, _scrollBar.OtherScrollBarView);
			Assert.Equal (_scrollBar.OtherScrollBarView.OtherScrollBarView, _scrollBar);
		}

		[Fact]
		public void ShowScrollIndicator_Check ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			Assert.True (_scrollBar.ShowScrollIndicator);
			Assert.True (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
		}

		[Fact]
		public void KeepContentAlwaysInViewport_True ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			Assert.Equal (80, _hostView.Bounds.Width);
			Assert.Equal (25, _hostView.Bounds.Height);
			Assert.Equal (79, _scrollBar.OtherScrollBarView.Bounds.Width);
			Assert.Equal (24, _scrollBar.Bounds.Height);
			Assert.Equal (30, _scrollBar.Size);
			Assert.Equal (100, _scrollBar.OtherScrollBarView.Size);
			Assert.True (_scrollBar.ShowScrollIndicator);
			Assert.True (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
			Assert.True (_scrollBar.Visible);
			Assert.True (_scrollBar.OtherScrollBarView.Visible);

			_scrollBar.Position = 50;
			Assert.Equal (_scrollBar.Position, _scrollBar.Size - _scrollBar.Bounds.Height);
			Assert.Equal (_scrollBar.Position, _hostView.Top);
			Assert.Equal (6, _scrollBar.Position);
			Assert.Equal (6, _hostView.Top);

			_scrollBar.OtherScrollBarView.Position = 150;
			Assert.Equal (_scrollBar.OtherScrollBarView.Position, _scrollBar.OtherScrollBarView.Size - _scrollBar.OtherScrollBarView.Bounds.Width);
			Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
			Assert.Equal (21, _scrollBar.OtherScrollBarView.Position);
			Assert.Equal (21, _hostView.Left);
		}

		[Fact]
		public void KeepContentAlwaysInViewport_False ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			_scrollBar.KeepContentAlwaysInViewport = false;
			_scrollBar.Position = 50;
			Assert.Equal (_scrollBar.Position, _scrollBar.Size - 1);
			Assert.Equal (_scrollBar.Position, _hostView.Top);

			_scrollBar.OtherScrollBarView.Position = 150;
			Assert.Equal (_scrollBar.OtherScrollBarView.Position, _scrollBar.OtherScrollBarView.Size - 1);
			Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
		}

		[Fact]
		public void AutoHideScrollBars_Check ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			_hostView.Redraw (_hostView.Bounds);
			Assert.True (_scrollBar.ShowScrollIndicator);
			Assert.True (_scrollBar.Visible);
			Assert.Equal ("Dim.Combine(DimView(side=Height, target=HostView()({X=0,Y=0,Width=80,Height=25}))-Dim.Absolute(1))",
				_scrollBar.Height.ToString ());
			Assert.Equal (24, _scrollBar.Bounds.Height);
			Assert.True (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
			Assert.True (_scrollBar.OtherScrollBarView.Visible);
			Assert.Equal ("Dim.Combine(DimView(side=Width, target=HostView()({X=0,Y=0,Width=80,Height=25}))-Dim.Absolute(1))",
				_scrollBar.OtherScrollBarView.Width.ToString ());
			Assert.Equal (79, _scrollBar.OtherScrollBarView.Bounds.Width);

			_hostView.Lines = 10;
			_hostView.Redraw (_hostView.Bounds);
			Assert.False (_scrollBar.ShowScrollIndicator);
			Assert.False (_scrollBar.Visible);
			Assert.Equal ("Dim.Combine(DimView(side=Height, target=HostView()({X=0,Y=0,Width=80,Height=25}))-Dim.Absolute(1))",
				_scrollBar.Height.ToString ());
			Assert.Equal (24, _scrollBar.Bounds.Height);

			_hostView.Cols = 60;
			_hostView.Redraw (_hostView.Bounds);
			Assert.False (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
			Assert.False (_scrollBar.OtherScrollBarView.Visible);
			Assert.Equal ("Dim.Combine(DimView(side=Width, target=HostView()({X=0,Y=0,Width=80,Height=25}))-Dim.Absolute(0))",
				_scrollBar.OtherScrollBarView.Width.ToString ());
			Assert.Equal (80, _scrollBar.OtherScrollBarView.Bounds.Width);

			_hostView.Lines = 40;
			_hostView.Redraw (_hostView.Bounds);
			Assert.True (_scrollBar.ShowScrollIndicator);
			Assert.True (_scrollBar.Visible);
			Assert.Equal ("Dim.Combine(DimView(side=Height, target=HostView()({X=0,Y=0,Width=80,Height=25}))-Dim.Absolute(0))",
				_scrollBar.Height.ToString ());
			Assert.Equal (25, _scrollBar.Bounds.Height);

			_hostView.Cols = 120;
			_hostView.Redraw (_hostView.Bounds);
			Assert.True (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
			Assert.True (_scrollBar.OtherScrollBarView.Visible);
			Assert.Equal ("Dim.Combine(DimView(side=Width, target=HostView()({X=0,Y=0,Width=80,Height=25}))-Dim.Absolute(1))",
				_scrollBar.OtherScrollBarView.Width.ToString ());
			Assert.Equal (79, _scrollBar.OtherScrollBarView.Bounds.Width);
			Assert.True (_scrollBar.ShowScrollIndicator);
			Assert.True (_scrollBar.Visible);
			Assert.Equal ("Dim.Combine(DimView(side=Height, target=HostView()({X=0,Y=0,Width=80,Height=25}))-Dim.Absolute(1))",
				_scrollBar.Height.ToString ());
			Assert.Equal (24, _scrollBar.Bounds.Height);
		}
	}
}
