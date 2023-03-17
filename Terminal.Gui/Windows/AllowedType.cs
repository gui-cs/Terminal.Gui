using System;
using System.Data;
using System.IO;
using System.Linq;

namespace Terminal.Gui {

	/// <summary>
	/// Describes a requirement on what <see cref="FileInfo"/> can be selected
	/// in a <see cref="FileDialog2"/>.
	/// </summary>
	public class AllowedType {

		/// <summary>
		/// Initializes a new instance of the <see cref="AllowedType"/> class.
		/// </summary>
		/// <param name="description">The human readable text to display.</param>
		/// <param name="extensions">Extension(s) to match e.g. .csv.</param>
		public AllowedType (string description, params string [] extensions)
		{
			if (extensions.Length == 0) {
				throw new ArgumentException ("You must supply at least one extension");
			}

			this.Description = description;
			this.Extensions = extensions;
		}

		/// <summary>
		/// Gets a value of <see cref="AllowedType"/> that matches any file.
		/// </summary>
		public static AllowedType Any { get; } = new AllowedType ("Any Files", ".*");

		/// <summary>
		/// Gets or Sets the human readable description for the file type
		/// e.g. "Comma Separated Values".
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Gets or Sets the permitted file extension(s) (e.g. ".csv").
		/// </summary>
		public string [] Extensions { get; set; }

		/// <summary>
		/// Gets a value indicating whether this instance is the
		/// static <see cref="Any"/> value which indicates matching
		/// any files.
		/// </summary>
		public bool IsAny => this == Any;

		/// <summary>
		/// Returns <see cref="Description"/> plus all <see cref="Extensions"/> separated by semicolons.
		/// </summary>
		public override string ToString ()
		{
			return $"{this.Description} ({string.Join (";", this.Extensions.Select (e => '*' + e).ToArray ())})";
		}

		internal bool Matches (string extension, bool strict)
		{
			if (this.IsAny) {
				return !strict;
			}

			return this.Extensions.Any (e => e.Equals (extension));
		}
	}
	
}