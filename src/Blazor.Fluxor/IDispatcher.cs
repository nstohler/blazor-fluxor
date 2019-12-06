using System;

namespace Blazor.Fluxor
{
	/// <summary>
	/// Interface that blazor components/pages should use to dispatch actions
	/// through the store
	/// </summary>
	public interface IDispatcher
	{
		/// <summary>
		/// Dispatches an action to all features added to the store and ensures all effects with a regstered
		/// interest in the action type are notified.
		/// </summary>
		/// <remarks>
		/// The return type is a Task because the store may also dispatch long-running side effects from 
		/// effects (<see cref="IEffect"/>).
		/// </remarks>
		/// <param name="action">The action to dispatch to all features</param>
		/// <param name="unknown"></param>
		
		void Dispatch(object action);

		//void Dispatch<T>(object action, Action<IResultAction<T>> resultAction);
		void Dispatch<T>(object action, Action<T> resultAction);
		
		void Dispatch<T1, T2>(object action, Action<T1> resultAction1, Action<T2> resultAction2);
		
		void Dispatch<T1, T2, T3>(object action, Action<T1> resultAction1, Action<T2> resultAction2, Action<T3> resultAction3);


	}
}
