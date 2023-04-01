using Terminal.Gui.FileServices;

namespace Terminal.Gui {
	internal class AutocompleteFilepathContext : AutocompleteContext {
		private FileDialogState state;

		public AutocompleteFilepathContext (FileDialogState state)
            : base(new System.Collections.Generic.List<System.Rune>(),0)
		{
			this.state = state;
		}

        // TODO: Generate recommendations
	}
}