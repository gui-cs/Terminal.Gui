using System.Collections.Generic;
using System.IO.Abstractions;


namespace Terminal.Gui {
	internal class NerdFonts {

		public string GetNerdIcon(IFileSystemInfo file)
		{
			if(_filenameToIcon.ContainsKey(file.Name))
			{
				return new string(glyphs[_filenameToIcon[file.Name]],1);
			}
				
			return "";
		}

		Dictionary<string,char> glyphs = new Dictionary<string, char>{
			{"nf-cod-package", ''},
			{"nf-cod-preview", ''},
			{"nf-custom-folder_config", ''},
			{"nf-custom-folder_git", ''},
			{"nf-custom-folder_git_branch", ''},
			{"nf-custom-folder_github", ''},
			{"nf-custom-folder_npm", ''},
			{"nf-dev-aws", ''},
			{"nf-dev-docker", ''},
			{"nf-dev-html5_multimedia", ''},
			{"nf-fa-font", ''},
			{"nf-fa-font_awesome", ''},
			{"nf-fa-fonticons", ''},
			{"nf-fa-github_alt", ''},
			{"nf-fa-users", ''},
			{"nf-fa-windows", ''},
			{"nf-mdi-apps", ''},
			{"nf-mdi-azure", 'ﴃ'},
			{"nf-mdi-cached", ''},
			{"nf-mdi-contacts", '﯉'},
			{"nf-mdi-desktop_classic", 'ﲾ'},
			{"nf-mdi-folder_download", ''},
			{"nf-mdi-folder_image", ''},
			{"nf-mdi-folder_star", 'ﮛ'},
			{"nf-mdi-library_music", ''},
			{"nf-mdi-movie", ''},
			{"nf-mdi-movie_roll", 'ﳜ'},
			{"nf-mdi-onedrive", ''},
			{"nf-mdi-ship_wheel", 'ﴱ'},
			{"nf-mdi-test_tube", 'ﭧ'},
			{"nf-mdi-timer", '祥'},
			{"nf-mdi-timer_10", '福'},
			{"nf-mdi-timer_3", '靖'},
			{"nf-mdi-timer_off", '精'},
			{"nf-mdi-timer_sand", '羽'},
			{"nf-mdi-timer_sand_empty", 'ﮫ'},
			{"nf-mdi-timer_sand_full", 'ﲊ'},
			{"nf-mdi-umbraco", '煮'},
			{"nf-oct-file_binary", ''},
			{"nf-oct-file_symlink_directory", ''},
			{"nf-oct-repo", ''},
			{"nf-oct-repo_clone", ''},
			{"nf-oct-repo_force_push", ''},
			{"nf-oct-repo_forked", ''},
			{"nf-oct-repo_pull", ''},
			{"nf-oct-repo_push", ''},
			{"nf-oct-terminal", ''},
			{"nf-seti-config", ''},
			{"nf-seti-project", ''},
		};

		Dictionary<string, string> _filenameToIcon = new Dictionary<string, string> ()
		{
			{"docs","nf-oct-repo"},
			{"documents","nf-oct-repo"},
			{"desktop","nf-mdi-desktop_classic"},
			{"benchmark","nf-mdi-timer"},
			{"demo","nf-cod-preview"},
			{"samples","nf-cod-preview"},
			{"contacts","nf-mdi-contacts"},
			{"apps","nf-mdi-apps"},
			{"applications","nf-mdi-apps"},
			{"artifacts","nf-cod-package"},
			{"shortcuts","nf-oct-file_symlink_directory"},
			{"links","nf-oct-file_symlink_directory"},
			{"fonts","nf-fa-font"},
			{"images","nf-mdi-folder_image"},
			{"photos","nf-mdi-folder_image"},
			{"pictures","nf-mdi-folder_image"},
			{"videos","nf-mdi-movie"},
			{"movies","nf-mdi-movie"},
			{"media","nf-dev-html5_multimedia"},
			{"music","nf-mdi-library_music"},
			{"songs","nf-mdi-library_music"},
			{"onedrive","nf-mdi-onedrive"},
			{"downloads","nf-mdi-folder_download"},
			{"src","nf-oct-terminal"},
			{"development","nf-oct-terminal"},
			{"projects","nf-seti-project"},
			{"bin","nf-oct-file_binary"},
			{"tests","nf-mdi-test_tube"},
			{"windows","nf-fa-windows"},
			{"users","nf-fa-users"},
			{"favorites","nf-mdi-folder_star"},
			{".config","nf-seti-config"},
			{".cache","nf-mdi-cached"},
			{".vscode","nf-custom-folder_config"},
			{".vscode-insiders","nf-custom-folder_config"},
			{".git","nf-custom-folder_git"},
			{".github","nf-custom-folder_github"},
			{"github","nf-fa-github_alt"},
			{"node_modules","nf-custom-folder_npm"},
			{".azure","nf-mdi-azure"},
			{".aws","nf-dev-aws"},
			{".kube","nf-mdi-ship_wheel"},
			{".docker","nf-dev-docker"},
			{"umbraco","nf-mdi-umbraco"},
	};
	}
}