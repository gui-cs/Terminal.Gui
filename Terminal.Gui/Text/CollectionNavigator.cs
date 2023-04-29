using System;
using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui {

	/// <inheritdoc/>
	/// <remarks>This implementation is based on a static <see cref="Collection"/> of objects.</remarks>
	public class CollectionNavigator : CollectionNavigatorBase
	{
		/// <summary>
		/// The collection of objects to search. <see cref="object.ToString()"/> is used to search the collection.
		/// </summary>
		public IEnumerable<object> Collection { get; set; }

		/// <summary>
		/// Constructs a new CollectionNavigator.
		/// </summary>
		public CollectionNavigator () { }

		/// <summary>
		/// Constructs a new CollectionNavigator for the given collection.
		/// </summary>
		/// <param name="collection"></param>
		public CollectionNavigator (IEnumerable<object> collection) => Collection = collection;

		/// <inheritdoc/>
		protected override IEnumerable<KeyValuePair<int, object>> GetCollection ()
		{
			if (Collection == null) {
				throw new InvalidOperationException ("Collection is null");
			}

			return Collection.Select ((item, idx) => KeyValuePair.Create(idx, item));
		}


	}
}
