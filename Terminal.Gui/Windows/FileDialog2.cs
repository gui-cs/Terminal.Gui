
using System;
using System.Collections.Generic;
using NStack;
using System.IO;
using System.Linq;
using Terminal.Gui.Resources;
using System.Data;

namespace Terminal.Gui {

    public class FileDialog2 : Dialog
    {
        public string Path {get => tbPath.Text.ToString(); set => tbPath.Text = value;}
		public const string HeaderFilename  = "Filename";
        public const string HeaderSize  = "Size";
        public const string HeaderModified  = "Modified";
        public const string HeaderType  = "Type";


		private TextField tbPath;

        /// <summary>
        /// True to use Utc dates for date modified
        /// </summary>
        public static bool UseUtcDates = false;

        DataTable dtFiles;
        TableView tableView;

        List<FileSystemInfoStats> fileStats = new List<FileSystemInfoStats>();
        
        public static ColorScheme ColorSchemeDirectory;
        public static ColorScheme ColorSchemeDefault;

        public FileDialog2()
        {
            var lblPath = new Label("Path:");
            
            this.Add(lblPath);
            tbPath = new TextField{
                X = Pos.Right(lblPath),
                Width = Dim.Fill()
            };
            this.Add(tbPath);

            tableView = new TableView{
                X = 0,
                Y = 1,
                Width = Dim.Fill(0),
                Height = Dim.Fill(1),
                FullRowSelect = true,
            };
            tableView.Style.ShowHorizontalHeaderOverline = false;
            tableView.Style.ShowVerticalCellLines = false;
            tableView.Style.ShowVerticalHeaderLines = false;
            tableView.Style.AlwaysShowHeaders = true;
            tableView.CellActivated += CellActivate;

            InitializeColorSchemes();

            SetupTableColumns();

            tableView.Table = dtFiles;
            this.Add(tableView);

            tbPath.TextChanged += (s)=>PathChanged();

            // TODO: delay or consider not doing this to avoid double load
            tbPath.Text = Environment.CurrentDirectory;
        }

		private void InitializeColorSchemes ()
		{
            if(ColorSchemeDirectory != null){
                return;
            }
			ColorSchemeDirectory = new ColorScheme{
                Normal = Driver.MakeAttribute(Color.Blue,Color.Black),
                Focus = Driver.MakeAttribute(Color.Black,Color.Blue),
            };


			ColorSchemeDefault = new ColorScheme{
                Normal = Driver.MakeAttribute(Color.Gray,Color.Black),
                Focus = Driver.MakeAttribute(Color.Black,Color.Gray),
            };            
		}

		private void SetupTableColumns ()
		{
			dtFiles = new DataTable();

            var nameStyle = tableView.Style.GetOrCreateColumnStyle(dtFiles.Columns.Add(HeaderFilename,typeof(int)));
            nameStyle.RepresentationGetter = (i)=> fileStats[(int)i].FileSystemInfo.Name;
            
            var sizeStyle = tableView.Style.GetOrCreateColumnStyle(dtFiles.Columns.Add(HeaderSize,typeof(int)));
            sizeStyle.RepresentationGetter = (i)=> fileStats[(int)i].HumanReadableLength;

            var dateModifiedStyle = tableView.Style.GetOrCreateColumnStyle(dtFiles.Columns.Add(HeaderModified,typeof(int)));
            dateModifiedStyle.RepresentationGetter = (i)=> fileStats[(int)i].DateModified?.ToString() ?? "";

            var typeStyle = tableView.Style.GetOrCreateColumnStyle(dtFiles.Columns.Add(HeaderType,typeof(int)));
            typeStyle.RepresentationGetter = (i)=> fileStats[(int)i].Type ?? "";

            tableView.Style.RowColorGetter = ColorGetter;
		}


		private void CellActivate (TableView.CellActivatedEventArgs obj)
		{
			var stats = RowToStats(obj.Row);
            
            if(stats.FileSystemInfo is DirectoryInfo d)
            {
                SetupAsDirectory(d);
                return;
            }
		}

		private ColorScheme ColorGetter (TableView.RowColorGetterArgs args)
		{
			var stats = RowToStats(args.RowIndex);

            if(stats.Type == "dir")            
            {
                return ColorSchemeDirectory;
            }

            return ColorSchemeDefault;
		}

		private FileSystemInfoStats RowToStats (int rowIndex)
		{
			return fileStats[(int)tableView.Table.Rows[rowIndex][0]];
		}

		private void PathChanged ()
		{
            var path = tbPath.Text?.ToString();

            if(string.IsNullOrWhiteSpace(path))
            {
                SetupAsClear();
                return;
            }

            var dir = new DirectoryInfo(path);

            if(dir.Exists)
            {
                SetupAsDirectory(dir);
            }
		}

		private void SetupAsDirectory (DirectoryInfo dir)
		{
			var entries = dir.GetFileSystemInfos();
            dtFiles.Rows.Clear();
            fileStats.Clear();
                

            foreach(var e in entries)
            {
                fileStats.Add(new FileSystemInfoStats(e));
            }

            for(int i=0;i<fileStats.Count;i++)
            {
                dtFiles.Rows.Add(i,i,i,i);
            }

            tableView.Update();
		}

		private void SetupAsClear ()
		{
            
		}
        class FileSystemInfoStats{
            
            public FileSystemInfo FileSystemInfo {get;}
            public string HumanReadableLength {get;}
			public DateTime? DateModified { get; }
            public string Type {get;}

			/*
			* Blue: Directory
			* Green: Executable or recognized data file
			* Cyan (Sky Blue): Symbolic link file
			* Yellow with black background: Device
			* Magenta (Pink): Graphic image file
			* Red: Archive file
			* Red with black background: Broken link
			*/

			public FileSystemInfoStats(FileSystemInfo fsi)
            {
                FileSystemInfo = fsi;
                
                if(fsi is FileInfo fi)
                {
                    HumanReadableLength = GetHumanReadableFileSize(fi.Length);
                    DateModified = FileDialog2.UseUtcDates ? File.GetLastWriteTimeUtc(fi.FullName) : File.GetLastWriteTime(fi.FullName);
                    Type = fi.Extension;
                }
                else
                {
                    HumanReadableLength = "";
                    Type = "dir";
                }
            }

            static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            static readonly List<string> ImageExtensions = new List<string> { ".JPG", ".JPEG", ".JPE", ".BMP", ".GIF", ".PNG" };

            // TODO: is executable;

            const long byteConversion = 1024;
            public static string GetHumanReadableFileSize(long value)
            {

                if (value < 0) { return "-" + GetHumanReadableFileSize(-value); }
                if (value == 0) { return "0.0 bytes"; }

                int mag = (int)Math.Log(value, byteConversion);
                double adjustedSize = (value / Math.Pow(1000, mag));


                return string.Format("{0:n2} {1}", adjustedSize, SizeSuffixes[mag]);
            }
        }
	}
}