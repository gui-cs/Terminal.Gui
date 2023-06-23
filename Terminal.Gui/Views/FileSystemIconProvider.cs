using System;
using System.IO;
using System.IO.Abstractions;
using System.Text;

namespace Terminal.Gui {
	public class FileSystemIconProvider
	{
		public bool UseNerdIcons {get;set;} = NerdFonts.Enable;
		public bool UseUnicodeCharacters {get;set;}

		private NerdFonts _nerd = new NerdFonts();
		
		public Func<IDirectoryInfo,bool> IsOpenGetter {get;set;} = (d)=>false;

		public Rune GetIcon (IFileSystemInfo fsi)
		{
			if(UseNerdIcons)
			{
				return new Rune(
					_nerd.GetNerdIcon(
						fsi,
						fsi is IDirectoryInfo dir ? IsOpenGetter(dir) : false
					));
			}

			if (fsi is IDirectoryInfo) {
				return UseUnicodeCharacters ? ConfigurationManager.Glyphs.Folder : new Rune(Path.DirectorySeparatorChar);
			}

			return UseUnicodeCharacters ?  ConfigurationManager.Glyphs.File : new Rune(' ');
		}
	}
}