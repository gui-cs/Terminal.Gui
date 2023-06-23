using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace Terminal.Gui {

	class FileSystemTreeBuilder : ITreeBuilder<IFileSystemInfo>, IComparer<IFileSystemInfo> {


		public FileSystemTreeBuilder () : this (new FileSystem ())
		{

		}
		public FileSystemTreeBuilder (IFileSystem fileSystem)
		{
			FileSystem = fileSystem;
			Sorter = this;
		}

		public bool SupportsCanExpand => true;

		public IFileSystem FileSystem { get; }

		/// <summary>
		/// Gets or sets a flag indicating whether to show files as leaf elements
		/// in the tree. Defaults to true.
		/// </summary>
		public bool IncludeFiles { get; } = true;


		public IComparer<IFileSystemInfo> Sorter { get; set; }


		public bool CanExpand (IFileSystemInfo toExpand)
		{
			return this.TryGetChildren (toExpand).Any ();
		}

		public IEnumerable<IFileSystemInfo> GetChildren (IFileSystemInfo forObject)
		{
			return this.TryGetChildren (forObject).OrderBy(k=>k,Sorter);
		}

		private IEnumerable<IFileSystemInfo> TryGetChildren (IFileSystemInfo entry)
		{
			if (entry is IFileInfo) {
				return Enumerable.Empty<IFileSystemInfo> ();
			}

			var dir = (IDirectoryInfo)entry;

			try {
				return dir.GetFileSystemInfos ().Where (e => IncludeFiles || e is IDirectoryInfo);

			} catch (Exception) {

				return Enumerable.Empty<IFileSystemInfo> ();
			}
		}
		public int Compare (IFileSystemInfo x, IFileSystemInfo y)
		{
			if (x is IDirectoryInfo && y is not IDirectory) {
				return -1;
			}

			if (x is not IDirectoryInfo && y is IDirectory) {
				return 1;
			}

			return 0;
		}
	}
}