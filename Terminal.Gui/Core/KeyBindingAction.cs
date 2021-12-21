// These classes use a keybinding system based on the design implemented in Scintilla.Net which is an MIT licensed open source project https://github.com/jacobslusser/ScintillaNET/blob/master/src/ScintillaNET/Command.cs

using System;

namespace Terminal.Gui {

	/// <summary>
	/// Records for a given <see cref="View"/> instance how to carry out a given <see cref="Command"/>
	/// </summary>
	public class KeyBinding {

		/// <summary>
		///  The key which can be pressed to trigger the given <see cref="Command"/>
		/// </summary>
		public Key Key { get; set; }


		/// <summary>
		///  The type of operation that will be performed
		/// </summary>
		public Command Command { get; }

		/// <summary>
		/// The view specific implementation logic of the given <see cref="Command"/>
		/// </summary>
		public Action Action { get; }

		/// <summary>
		/// Set to false to disable this keybinding.  Defaults to true
		/// </summary>
		public bool Enabled { get; set; } = true;

		/// <summary>
		///  Creates a new instance in which the given key combination triggers the associated action
		/// </summary>
		/// <param name="key"></param>
		/// <param name="command"></param>
		/// <param name="action"></param>
		public KeyBinding (Key key, Command command, Action action)
		{
			Key = key;
			Command = command;
			Action = action;
		}

		/// <summary>
		/// Creates a new instance targetting the same <see cref="Action"/> with the same
		/// bindings.  Use this method if you want to configure additional keys which will
		/// also trigger the same <see cref="Command"/> in a view
		/// </summary>
		/// <returns></returns>
		public KeyBinding Clone ()
		{
			return new KeyBinding (Key, Command, Action);
		}
	}

	/// <summary>
	/// Actions which can be performed by the application or bound to keys in a <see cref="View"/> control.
	/// </summary>
	public enum Command {

		/// <summary>
		/// Moves the caret down one line.
		/// </summary>
		LineDown,

		/// <summary>
		/// Extends the selection down one line.
		/// </summary>
		LineDownExtend,

		/// <summary>
		/// Scrolls down one line.
		/// </summary>
		LineScrollDown,

		// --------------------------------------------------------------------

		/// <summary>
		/// Moves the caret up one line.
		/// </summary>
		LineUp,

		/// <summary>
		/// Extends the selection up one line.
		/// </summary>
		LineUpExtend,

		/// <summary>
		/// Scrolls up one line.
		/// </summary>
		LineScrollUp,

		/// <summary>
		/// Moves the caret left one character.
		/// </summary>
		CharLeft,

		/// <summary>
		/// Extends the selection left one character.
		/// </summary>
		CharLeftExtend,

		/// <summary>
		/// Moves the caret right one character.
		/// </summary>
		CharRight,

		/// <summary>
		/// Extends the selection right one character.
		/// </summary>
		CharRightExtend,

		/// <summary>
		/// Moves the caret to the start of the previous word.
		/// </summary>
		WordLeft,

		/// <summary>
		/// Extends the selection to the start of the previous word.
		/// </summary>
		WordLeftExtend,

		/// <summary>
		/// Moves the caret to the start of the next word.
		/// </summary>
		WordRight,

		/// <summary>
		/// Extends the selection to the start of the next word.
		/// </summary>
		WordRightExtend,


	}
}