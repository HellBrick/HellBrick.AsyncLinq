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
		private T _nextItem;
		private Exception _nextException;
		private TaskCompletionSource<Optional<T>> _nextItemTaskCompletionSource;

		public IAsyncStateMachine BoxedStateMachine { get; set; }

		/// <remarks>It's important to update the fields before completing the source to prevent the continuation thread from seeing obsolete values.</remarks>
		public void CompleteCurrentItem( Optional<T> item )
		{
			TaskCompletionSource<Optional<T>> tcs = Interlocked.Exchange( ref _nextItemTaskCompletionSource, null );
			Volatile.Write( ref _isYieldBreakReached, !item.HasValue );
			Volatile.Write( ref _state, State.None );

			if ( tcs != null )
				tcs.SetResult( item );
			else if ( item.HasValue )
				_nextItem = item.Value;

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

			if ( tcs != null )
				tcs.SetException( exception );
			else
				Volatile.Write( ref _nextException, exception );
		}

		public void StartAwaitingNextItem()
			=> Volatile.Write( ref _nextItemTaskCompletionSource, new TaskCompletionSource<Optional<T>>( TaskCreationOptions.RunContinuationsAsynchronously ) );

		public AsyncItem<T> GetNextAsync()
		{
			if ( Volatile.Read( ref _isYieldBreakReached ) )
				return AsyncItem<T>.NoItem;

			if ( Interlocked.CompareExchange( ref _state, State.ItemRequested, State.None ) != State.None )
				throw new PreviousItemNotCompletedException();

			BoxedStateMachine.MoveNext();

			TaskCompletionSource<Optional<T>> tcs = Volatile.Read( ref _nextItemTaskCompletionSource );
			if ( tcs != null )
				return new AsyncItem<T>( tcs.Task );

			Exception exception = Interlocked.Exchange( ref _nextException, null );
			if ( exception != null )
				return new AsyncItem<T>( Task.FromException<Optional<T>>( exception ) );

			if ( Volatile.Read( ref _isYieldBreakReached ) )
				return AsyncItem<T>.NoItem;

			T nextItem = _nextItem;
			_nextItem = default;
			return new AsyncItem<T>( nextItem );
		}
	}
}
