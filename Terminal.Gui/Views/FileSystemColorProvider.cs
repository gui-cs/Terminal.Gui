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
            {"documents",StringToColor( "00BFFF")},
            {"desktop",StringToColor( "00FBFF")},
            {"benchmark",StringToColor( "F08519")},
            {"demo",StringToColor( "5F3EC3")},
            {"samples",StringToColor( "5F3EC3")},
            {"contacts",StringToColor( "00FBFF")},
            {"apps",StringToColor( "FF143C")},
            {"applications",StringToColor( "FF143C")},
            {"artifacts",StringToColor( "D49653")},
            {"shortcuts",StringToColor( "FF143C")},
            {"links",StringToColor( "FF143C")},
            {"fonts",StringToColor( "DC143C")},
            {"images",StringToColor( "9ACD32")},
            {"photos",StringToColor( "9ACD32")},
            {"pictures",StringToColor( "9ACD32")},
            {"videos",StringToColor( "FFA500")},
            {"movies",StringToColor( "FFA500")},
            {"media",StringToColor( "D3D3D3")},
            {"music",StringToColor( "DB7093")},
            {"songs",StringToColor( "DB7093")},
            {"onedrive",StringToColor( "D3D3D3")},
            {"downloads",StringToColor( "D3D3D3")},
            {"src",StringToColor( "00FF7F")},
            {"development",StringToColor( "00FF7F")},
            {"projects",StringToColor( "00FF7F")},
            {"bin",StringToColor( "00FFF7")},
            {"tests",StringToColor( "87CEEB")},
            {"windows",StringToColor( "00A8E8")},
            {"users",StringToColor( "F4F4F4")},
            {"favorites",StringToColor( "F7D72C")},
            {"output",StringToColor( "00FF7F")},            
        };

        private static TrueColor StringToColor(string str)
        {
            TrueColor.TryParse(str, out var c);
            return c ?? throw new System.Exception("Failed to parse TrueColor from " + str);
        }
	}
}