﻿using System;

namespace Terminal.Gui.Views {

	/// <summary>
	/// A 1x1 <see cref="View"/> based on <see cref="Label"/> which displays a spinning
	/// line character.
	/// </summary>
	/// <remarks>
	/// By default animation only occurs when you call <see cref="View.SetNeedsDisplay()"/>.
	/// Use <see cref="AutoSpin"/> to make the automate calls to <see cref="View.SetNeedsDisplay()"/>.
	/// </remarks>
	public class SpinnerLabel : Label {
		private Rune [] runes = new Rune [] { '|', '/', '\u2500', '\\' };
		private int currentIdx = 0;
		private DateTime lastRender = DateTime.MinValue;
		private object _timeout;

		/// <summary>
		/// Gets or sets the number of milliseconds to wait between characters
		/// in the spin.  Defaults to 250
		/// </summary>
		public int SpinDelayInMilliseconds { get; set; } = 250;

		/// <inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			if (DateTime.Now - lastRender > TimeSpan.FromMilliseconds (SpinDelayInMilliseconds)) {
				currentIdx = (currentIdx + 1) % runes.Length;
				Text = "" + runes [currentIdx];
			}

			base.Redraw (bounds);
		}

		/// <summary>
		/// Automates spinning
		/// </summary>
		public void AutoSpin()
		{
			if(_timeout != null) {
				return;
			}

			_timeout = Application.MainLoop.AddTimeout (
				TimeSpan.FromMilliseconds (SpinDelayInMilliseconds), (m) => {
					Application.MainLoop.Invoke (this.SetNeedsDisplay);
					return true;
				});
		}

		/// <inheritdoc/>
		protected override void Dispose (bool disposing)
		{
			if (_timeout != null) {
				Application.MainLoop.RemoveTimeout (_timeout);
			}

			base.Dispose (disposing);
		}
	}
}