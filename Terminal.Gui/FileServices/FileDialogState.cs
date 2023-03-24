﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Terminal.Gui.FileServices {

	internal class FileDialogState {

		public FileSystemInfoStats Selected { get; set; }
		protected readonly FileDialog Parent;
		public FileDialogState (DirectoryInfo dir, FileDialog parent)
		{
			this.Directory = dir;
			Parent = parent;

			this.RefreshChildren ();
		}

		public DirectoryInfo Directory { get; }

		public FileSystemInfoStats [] Children { get; protected set; }

		internal virtual void RefreshChildren ()
		{
			var dir = this.Directory;
			Children = GetChildren (dir).ToArray ();
		}

		protected virtual IEnumerable<FileSystemInfoStats> GetChildren (DirectoryInfo dir)
		{
			try {

				List<FileSystemInfoStats> children;

				// if directories only
				if (Parent.OpenMode == OpenMode.Directory) {
					children = dir.GetDirectories ().Select (e => new FileSystemInfoStats (e)).ToList ();
				} else {
					children = dir.GetFileSystemInfos ().Select (e => new FileSystemInfoStats (e)).ToList ();
				}

				// if only allowing specific file types
				if (Parent.AllowedTypes.Any () && Parent.OpenMode == OpenMode.File) {

					children = children.Where (
						c => c.IsDir () ||
						(c.FileSystemInfo is FileInfo f && Parent.IsCompatibleWithAllowedExtensions (f)))
						.ToList ();
				}

				// if theres a UI filter in place too
				if (Parent.CurrentFilter != null) {
					children = children.Where (MatchesApiFilter).ToList ();
				}


				// allow navigating up as '..'
				if (dir.Parent != null) {
					children.Add (new FileSystemInfoStats (dir.Parent) { IsParent = true });
				}

				return children;
			} catch (Exception) {
				// Access permissions Exceptions, Dir not exists etc
				return Enumerable.Empty<FileSystemInfoStats> ();
			}
		}

		protected bool MatchesApiFilter (FileSystemInfoStats arg)
		{
			return arg.IsDir () ||
			(arg.FileSystemInfo is FileInfo f && Parent.CurrentFilter.IsAllowed (f.FullName));
		}
	}
}