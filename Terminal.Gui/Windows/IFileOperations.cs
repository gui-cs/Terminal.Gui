using System.Collections.Generic;
using System.IO;

namespace Terminal.Gui {
	/// <summary>
	/// Interface for defining how to handle file/directory 
	/// deletion, rename and newing attempts in <see cref="FileDialog"/>.
	/// </summary>
	public interface IFileOperations
	{
		/// <summary>
		/// Specifies how to handle file/directory deletion attempts
		/// in <see cref="FileDialog"/>.
		/// </summary>
		/// <param name="toDelete"></param>
        /// <returns><see langword="true"/> if operation was completed or 
        /// <see langword="false"/> if cancelled</returns>
		/// <remarks>Ensure you use a try/catch block with appropriate
		/// error handling (e.g. showing a <see cref="MessageBox"/></remarks>
		bool Delete(IEnumerable<FileSystemInfo> toDelete);


		/// <summary>
		/// Specifies how to handle file/directory rename attempts
		/// in <see cref="FileDialog"/>.
		/// </summary>
		/// <param name="toRename"></param>
        /// <returns><see langword="true"/> if operation was completed or 
        /// <see langword="false"/> if cancelled</returns>
		/// <remarks>Ensure you use a try/catch block with appropriate
		/// error handling (e.g. showing a <see cref="MessageBox"/></remarks>
		bool Rename(FileSystemInfo toRename);


		/// <summary>
		/// Specifies how to handle 'new directory' operation
		/// in <see cref="FileDialog"/>.
		/// </summary>
		/// <param name="inDirectory">The parent directory in which the new
		/// directory should be created</param>
        /// <returns><see langword="true"/> if operation was completed or 
        /// <see langword="false"/> if cancelled</returns>
		/// <remarks>Ensure you use a try/catch block with appropriate
		/// error handling (e.g. showing a <see cref="MessageBox"/></remarks>
		bool New(DirectoryInfo inDirectory);
	}
}