using System;
using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui {

	/// <summary>
	/// Interface for supplying data to a <see cref="TreeView{T}"/> on demand as root level nodes
	/// are expanded by the user
	/// </summary>
	public interface ITreeBuilder<T> {
		/// <summary>
		/// Returns true if <see cref="CanExpand"/> is implemented by this class
		/// </summary>
		/// <value></value>
		bool SupportsCanExpand { get; }

		/// <summary>
		/// Returns true/false for whether a model has children.  This method should be implemented
		/// when <see cref="GetChildren"/> is an expensive operation otherwise 
		/// <see cref="SupportsCanExpand"/> should return false (in which case this method will not
		/// be called)
		/// </summary>
		/// <remarks>Only implement this method if you have a very fast way of determining whether 
		/// an object can have children e.g. checking a Type (directories can always be expanded)
		/// </remarks>
		/// <param name="toExpand"></param>
		/// <returns></returns>
		bool CanExpand (T toExpand);

		/// <summary>
		/// Returns all children of a given <paramref name="forObject"/> which should be added to the 
		/// tree as new branches underneath it
		/// </summary>
		/// <param name="forObject"></param>
		/// <returns></returns>
		IEnumerable<T> GetChildren (T forObject);
	}

	/// <summary>
	/// Abstract implementation of <see cref="ITreeBuilder{T}"/>.
	/// </summary>
	public abstract class TreeBuilder<T> : ITreeBuilder<T> {

		/// <inheritdoc/>
		public bool SupportsCanExpand { get; protected set; } = false;

		/// <summary>
		/// Override this method to return a rapid answer as to whether <see cref="GetChildren(T)"/> 
		/// returns results.  If you are implementing this method ensure you passed true in base 
		/// constructor or set <see cref="SupportsCanExpand"/>
		/// </summary>
		/// <param name="toExpand"></param>
		/// <returns></returns>
		public virtual bool CanExpand (T toExpand)
		{

			return GetChildren (toExpand).Any ();
		}

		/// <inheritdoc/>
		public abstract IEnumerable<T> GetChildren (T forObject);

		/// <summary>
		/// Constructs base and initializes <see cref="SupportsCanExpand"/>
		/// </summary>
		/// <param name="supportsCanExpand">Pass true if you intend to 
		/// implement <see cref="CanExpand(T)"/> otherwise false</param>
		public TreeBuilder (bool supportsCanExpand)
		{
			SupportsCanExpand = supportsCanExpand;
		}
	}

	

	/// <summary>
	/// <see cref="ITreeBuilder{T}"/> implementation for <see cref="ITreeNode"/> objects
	/// </summary>
	public class TreeNodeBuilder : TreeBuilder<ITreeNode> {

		/// <summary>
		/// Initialises a new instance of builder for any model objects of 
		/// Type <see cref="ITreeNode"/>
		/// </summary>
		public TreeNodeBuilder () : base (false)
		{

		}

		/// <summary>
		/// Returns <see cref="ITreeNode.Children"/> from <paramref name="model"/>
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		public override IEnumerable<ITreeNode> GetChildren (ITreeNode model)
		{
			return model.Children;
		}
	}

	
	/// <summary>
	/// Implementation of <see cref="ITreeBuilder{T}"/> that uses user defined functions
	/// </summary>
	public class DelegateTreeBuilder<T> : TreeBuilder<T> {
		private Func<T, IEnumerable<T>> childGetter;
		private Func<T, bool> canExpand;

		/// <summary>
		/// Constructs an implementation of <see cref="ITreeBuilder{T}"/> that calls the user 
		/// defined method <paramref name="childGetter"/> to determine children
		/// </summary>
		/// <param name="childGetter"></param>
		/// <returns></returns>
		public DelegateTreeBuilder (Func<T, IEnumerable<T>> childGetter) : base (false)
		{
			this.childGetter = childGetter;
		}

		/// <summary>
		/// Constructs an implementation of <see cref="ITreeBuilder{T}"/> that calls the user 
		/// defined method <paramref name="childGetter"/> to determine children 
		/// and <paramref name="canExpand"/> to determine expandability
		/// </summary>
		/// <param name="childGetter"></param>
		/// <param name="canExpand"></param>
		/// <returns></returns>
		public DelegateTreeBuilder (Func<T, IEnumerable<T>> childGetter, Func<T, bool> canExpand) : base (true)
		{
			this.childGetter = childGetter;
			this.canExpand = canExpand;
		}

		/// <summary>
		/// Returns whether a node can be expanded based on the delegate passed during construction
		/// </summary>
		/// <param name="toExpand"></param>
		/// <returns></returns>
		public override bool CanExpand (T toExpand)
		{
			return canExpand?.Invoke (toExpand) ?? base.CanExpand (toExpand);
		}

		/// <summary>
		/// Returns children using the delegate method passed during construction
		/// </summary>
		/// <param name="forObject"></param>
		/// <returns></returns>
		public override IEnumerable<T> GetChildren (T forObject)
		{
			return childGetter.Invoke (forObject);
		}
	}
}