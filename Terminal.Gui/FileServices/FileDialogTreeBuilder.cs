﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace Terminal.Gui {

	class FileDialogTreeBuilder : ITreeBuilder<object> {

		public bool SupportsCanExpand => true;

		public bool CanExpand (object toExpand)
		{
			return this.TryGetDirectories (NodeToDirectory (toExpand)).Any ();
		}

		public IEnumerable<object> GetChildren (object forObject)
		{
			return this.TryGetDirectories (NodeToDirectory (forObject));
		}

		internal static IDirectoryInfo NodeToDirectory (object toExpand)
		{
			return toExpand is FileDialogRootTreeNode f ? f.Path : (IDirectoryInfo)toExpand;
		}

		internal string AspectGetter(object o)
		{
			return o is FileDialogRootTreeNode r ? r.DisplayName : ((IDirectoryInfo)o).Name;
		}

		private IEnumerable<IDirectoryInfo> TryGetDirectories (IDirectoryInfo directoryInfo)
		{
			try {
				return directoryInfo.EnumerateDirectories ();
			} catch (Exception) {

				return Enumerable.Empty<IDirectoryInfo> ();
			}
		}

	}
}