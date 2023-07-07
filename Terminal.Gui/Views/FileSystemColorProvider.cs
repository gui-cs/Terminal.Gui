// This code is adapted from https://github.com/devblackops/Terminal-Icons (which also uses the MIT license).
// Nerd fonts can be installed by following the instructions on the Nerd Fonts repository: https://github.com/ryanoasis/nerd-fonts

using System.Collections.Generic;

namespace Terminal.Gui {
	class FileSystemColorProvider
	{
        TrueColor b = new TrueColor();

        /// <summary>
		/// Mapping of file name to color.
		/// </summary>
		public Dictionary<string, TrueColor> FilenameToColor {get;set;} = new ()
		{
            {"docs",StringToColor("#00BFFF")},
        };

        private static TrueColor StringToColor(string str)
        {
            TrueColor.TryParse(str, out var c);
            return c ?? throw new System.Exception("Failed to parse TrueColor from " + str);
        }
	}
}