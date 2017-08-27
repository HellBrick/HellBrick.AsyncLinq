using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace HellBrick.AsyncLinq
{
	internal class StateMachineAsyncEnumerator<T> : IAsyncEnumerator<T>
	{
		private static class State
		{
			/// <summary>The enumerator has no pending item and is ready to serve an item request.</summary>
			public const int None = 0;

			/// <summary>An item has been requested but haven't been completed yet.</summary>
			public const int ItemRequested = 1;
		}

		private int _state = State.None;

		private bool _isYieldBreakReached;
		private TaskCompletionSource<Optional<T>> _nextItemTaskCompletionSource;

		public IAsyncStateMachine BoxedStateMachine { get; set; }

		/// <remarks>It's important to update the fields before completing the source to prevent the continuation thread from seeing obsolete values.</remarks>
		public void CompleteCurrentItem( Optional<T> item )
		{
			TaskCompletionSource<Optional<T>> tcs = Interlocked.Exchange( ref _nextItemTaskCompletionSource, null );
			Volatile.Write( ref _isYieldBreakReached, !item.HasValue );
			Volatile.Write( ref _state, State.None );
			tcs.SetResult( item );

			/// If a yield break happens, we're still going to need to advance the state machine one more time to execute the return statement.
			/// It's necessary for the finally block to be executed when breaking from inside the try block.
			if ( !item.HasValue )
				BoxedStateMachine.MoveNext();
		}

		public void CompleteCurrentItem( Exception exception )
		{
			TaskCompletionSource<Optional<T>> tcs = Interlocked.Exchange( ref _nextItemTaskCompletionSource, null );
			Volatile.Write( ref _isYieldBreakReached, true );
			Volatile.Write( ref _state, State.None );
			tcs.SetException( exception );
		}

		public AsyncItem<T> GetNextAsync()
		{
			if ( Volatile.Read( ref _isYieldBreakReached ) )
				return AsyncItem<T>.NoItem;

			if ( Interlocked.CompareExchange( ref _state, State.ItemRequested, State.None ) != State.None )
				return new AsyncItem<T>( Task.FromException<Optional<T>>( new PreviousItemNotCompletedException() ) );

			TaskCompletionSource<Optional<T>> newTcs = new TaskCompletionSource<Optional<T>>( TaskCreationOptions.RunContinuationsAsynchronously );
			Volatile.Write( ref _nextItemTaskCompletionSource, newTcs );

			BoxedStateMachine.MoveNext();
			return new AsyncItem<T>( newTcs.Task );
		}
	}
}
