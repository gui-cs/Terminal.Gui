﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace Terminal.Gui {

	/// <summary>
	/// Wrapper for <see cref="FileSystemInfo"/> that contains additional information
	/// (e.g. <see cref="IsParent"/>) and helper methods.
	/// </summary>
	internal class FileSystemInfoStats {


		/* ---- Colors used by the ls command line tool ----
		 *
		* Blue: Directory
		* Green: Executable or recognized data file
		* Cyan (Sky Blue): Symbolic link file
		* Yellow with black background: Device
		* Magenta (Pink): Graphic image file
		* Red: Archive file
		* Red with black background: Broken link
		*/

		private const long ByteConversion = 1024;

		private static readonly string [] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
		private static readonly List<string> ImageExtensions = new List<string> { ".JPG", ".JPEG", ".JPE", ".BMP", ".GIF", ".PNG" };
		private static readonly List<string> ExecutableExtensions = new List<string> { ".EXE", ".BAT" };

		/// <summary>
		/// Initializes a new instance of the <see cref="FileSystemInfoStats"/> class.
		/// </summary>
		/// <param name="fsi">The directory of path to wrap.</param>
		public FileSystemInfoStats (IFileSystemInfo fsi)
		{
			this.FileSystemInfo = fsi;
			this.LastWriteTime = fsi.LastWriteTime;

			if (fsi is IFileInfo fi) {
				this.MachineReadableLength = fi.Length;
				this.HumanReadableLength = GetHumanReadableFileSize (this.MachineReadableLength);				
				this.Type = fi.Extension;
			} else {
				this.HumanReadableLength = string.Empty;
				this.Type = "dir";
			}
		}

		/// <summary>
		/// Gets the wrapped <see cref="FileSystemInfo"/> (directory or file).
		/// </summary>
		public IFileSystemInfo FileSystemInfo { get; }
		public string HumanReadableLength { get; }
		public long MachineReadableLength { get; }
		public DateTime? LastWriteTime { get; }
		public string Type { get; }

		/// <summary>
		/// Gets or Sets a value indicating whether this instance represents
		/// the parent of the current state (i.e. "..").
		/// </summary>
		public bool IsParent { get; internal set; }
		public string Name => this.IsParent ? ".." : this.FileSystemInfo.Name;

		public bool IsDir ()
		{
			return this.Type == "dir";
		}

		public bool IsImage ()
		{
			return this.FileSystemInfo is FileSystemInfo f &&
				ImageExtensions.Contains (
					f.Extension,
					StringComparer.InvariantCultureIgnoreCase);
		}

		public bool IsExecutable ()
		{
			// TODO: handle linux executable status
			return this.FileSystemInfo is FileSystemInfo f &&
				ExecutableExtensions.Contains (
					f.Extension,
					StringComparer.InvariantCultureIgnoreCase);
		}

		internal object GetOrderByValue (FileDialog dlg, string columnName)
		{
			if (dlg.Style.FilenameColumnName == columnName)
				return this.FileSystemInfo.Name;

			if (dlg.Style.SizeColumnName == columnName)
				return this.MachineReadableLength;

			if (dlg.Style.ModifiedColumnName == columnName)
				return this.LastWriteTime;

			if (dlg.Style.TypeColumnName == columnName)
				return this.Type;

			throw new ArgumentOutOfRangeException ("Unknown column " + nameof (columnName));
		}

		internal object GetOrderByDefault ()
		{
			if (this.IsDir ()) {
				return -1;
			}

			return 100;
		}

		private static string GetHumanReadableFileSize (long value)
		{

			if (value < 0) {
				return "-" + GetHumanReadableFileSize (-value);
			}

			if (value == 0) {
				return "0.0 bytes";
			}

			int mag = (int)Math.Log (value, ByteConversion);
			double adjustedSize = value / Math.Pow (1000, mag);


			return string.Format ("{0:n2} {1}", adjustedSize, SizeSuffixes [mag]);
		}
	}
}