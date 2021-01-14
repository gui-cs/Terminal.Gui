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
		private ScrollBarView _vertical;
		private ScrollBarView _horizontal;
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
				_vertical.ChangedPosition += _vertical_ChangedPosition;
				_horizontal.ChangedPosition += _horizontal_ChangedPosition;
			}
			_added = true;
		}

		private void RemoveHandlers ()
		{
			if (_added) {
				_hostView.DrawContent -= _hostView_DrawContent;
				_vertical.ChangedPosition -= _vertical_ChangedPosition;
				_horizontal.ChangedPosition -= _horizontal_ChangedPosition;
			}
			_added = false;
		}

		private void _hostView_DrawContent (Rect obj)
		{
			_vertical.Size = _hostView.Lines;
			_vertical.Position = _hostView.Top;
			_horizontal.Size = _hostView.Cols;
			_horizontal.Position = _hostView.Left;
			_vertical.ColorScheme = _horizontal.ColorScheme = _hostView.ColorScheme;
			if (_vertical.ShowScrollIndicator) {
				_vertical.Redraw (obj);
			}
			if (_horizontal.ShowScrollIndicator) {
				_horizontal.Redraw (obj);
			}
		}

		private void _vertical_ChangedPosition ()
		{
			_hostView.Top = _vertical.Position;
			if (_hostView.Top != _vertical.Position) {
				_vertical.Position = _hostView.Top;
			}
			_hostView.SetNeedsDisplay ();
		}

		private void _horizontal_ChangedPosition ()
		{
			_hostView.Left = _horizontal.Position;
			if (_hostView.Left != _horizontal.Position) {
				_horizontal.Position = _hostView.Left;
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
		public void Hosting_A_View_To_A_ScrollBarView ()
		{
			RemoveHandlers ();

			_vertical = new ScrollBarView (_hostView, true);
			_horizontal = new ScrollBarView (_hostView, false);
			_vertical.OtherScrollBarView = _horizontal;
			_horizontal.OtherScrollBarView = _vertical;

			Assert.True (_vertical.IsVertical);
			Assert.False (_horizontal.IsVertical);

			Assert.Equal (_vertical.Position, _hostView.Top);
			Assert.NotEqual (_vertical.Size, _hostView.Lines);
			Assert.Equal (_horizontal.Position, _hostView.Left);
			Assert.NotEqual (_horizontal.Size, _hostView.Cols);

			AddHandlers ();
			_hostView.SuperView.LayoutSubviews ();
			_hostView.Redraw (_hostView.Bounds);

			Assert.Equal (_vertical.Position, _hostView.Top);
			Assert.Equal (_vertical.Size, _hostView.Lines);
			Assert.Equal (_horizontal.Position, _hostView.Left);
			Assert.Equal (_horizontal.Size, _hostView.Cols);
		}

		[Fact]
		public void ChangedPosition_Update_The_Hosted_View ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			_vertical.Position = 2;
			Assert.Equal (_vertical.Position, _hostView.Top);

			_horizontal.Position = 5;
			Assert.Equal (_horizontal.Position, _hostView.Left);
		}

		[Fact]
		public void ChangedPosition_Scrolling ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			for (int i = 0; i < _vertical.Size; i++) {
				_vertical.Position += 1;
				Assert.Equal (_vertical.Position, _hostView.Top);
			}
			for (int i = _vertical.Size - 1; i >= 0; i--) {
				_vertical.Position -= 1;
				Assert.Equal (_vertical.Position, _hostView.Top);
			}

			for (int i = 0; i < _horizontal.Size; i++) {
				_horizontal.Position += i;
				Assert.Equal (_horizontal.Position, _hostView.Left);
			}
			for (int i = _horizontal.Size - 1; i >= 0; i--) {
				_horizontal.Position -= 1;
				Assert.Equal (_horizontal.Position, _hostView.Left);
			}
		}

		[Fact]
		public void ChangedPosition_Negative_Value ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			_vertical.Position = -20;
			Assert.Equal (0, _vertical.Position);
			Assert.Equal (_vertical.Position, _hostView.Top);

			_horizontal.Position = -50;
			Assert.Equal (0, _horizontal.Position);
			Assert.Equal (_horizontal.Position, _hostView.Left);
		}

		[Fact]
		public void DrawContent_Update_The_ScrollBarView_Position ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			_hostView.Top = 3;
			_hostView.Redraw (_hostView.Bounds);
			Assert.Equal (_vertical.Position, _hostView.Top);

			_hostView.Left = 6;
			_hostView.Redraw (_hostView.Bounds);
			Assert.Equal (_horizontal.Position, _hostView.Left);
		}

		[Fact]
		public void OtherScrollBarView_Not_Null ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			Assert.Equal (_vertical.OtherScrollBarView, _horizontal);
			Assert.Equal (_horizontal.OtherScrollBarView, _vertical);
		}

		[Fact]
		public void ShowScrollIndicator_Check ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			Assert.True (_vertical.ShowScrollIndicator);
			Assert.True (_horizontal.ShowScrollIndicator);
		}

		[Fact]
		public void KeepContentAlwaysInViewport_True ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			_vertical.Position = 50;
			Assert.Equal (_vertical.Position, _vertical.Size - _vertical.Bounds.Height + 1);
			Assert.Equal (_vertical.Position, _hostView.Top);

			_horizontal.Position = 150;
			Assert.Equal (_horizontal.Position, _horizontal.Size - _horizontal.Bounds.Width + 1);
			Assert.Equal (_horizontal.Position, _hostView.Left);
		}

		[Fact]
		public void KeepContentAlwaysInViewport_False ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			_vertical.KeepContentAlwaysInViewport = false;
			_vertical.Position = 50;
			Assert.Equal (_vertical.Position, _vertical.Size - 1);
			Assert.Equal (_vertical.Position, _hostView.Top);

			_horizontal.Position = 150;
			Assert.Equal (_horizontal.Position, _horizontal.Size - 1);
			Assert.Equal (_horizontal.Position, _hostView.Left);
		}

		[Fact]
		public void AutoHideScrollBars_Check ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			_hostView.Lines = 10;
			_hostView.Redraw (_hostView.Bounds);
			Assert.False (_vertical.ShowScrollIndicator);
			_hostView.Cols = 60;
			_hostView.Redraw (_hostView.Bounds);
			Assert.False (_horizontal.ShowScrollIndicator);

			_hostView.Lines = 40;
			_hostView.Redraw (_hostView.Bounds);
			Assert.True (_vertical.ShowScrollIndicator);
			_hostView.Cols = 120;
			_hostView.Redraw (_hostView.Bounds);
			Assert.True (_horizontal.ShowScrollIndicator);
		}
	}
}
