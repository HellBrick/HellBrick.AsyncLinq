﻿using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace HellBrick.AsyncLinq
{
	internal class StateMachineAsyncEnumerator<T> : IAsyncEnumerator<T>
	{
		private bool _isYieldBreakReached;
		private TaskCompletionSource<Optional<T>> _nextItemTaskCompletionSource;
		private IAsyncEnumerator<T> _currentEnumerator;

		public IAsyncStateMachine BoxedStateMachine { get; set; }

		/// <remarks>It's important to update the fields before completing the source to prevent the continuation thread from seeing obsolete values.</remarks>
		public void CompleteCurrentItem( Optional<T> item )
		{
			TaskCompletionSource<Optional<T>> tcs = Interlocked.Exchange( ref _nextItemTaskCompletionSource, null );
			Volatile.Write( ref _isYieldBreakReached, !item.HasValue );
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
			tcs.SetException( exception );
		}

		public void SetCurrentEnumerator( IAsyncEnumerator<T> itemEnumerator )
		{
			Volatile.Write( ref _currentEnumerator, itemEnumerator );
			AwaitNextInnerEnumeratorItemAndCompleteTcsAsync( itemEnumerator );
		}

		public Task<Optional<T>> GetNextAsync()
		{
			if ( Volatile.Read( ref _isYieldBreakReached ) )
				return Optional<T>.NoValueTask;

			TaskCompletionSource<Optional<T>> previousTcs = Volatile.Read( ref _nextItemTaskCompletionSource );
			TaskCompletionSource<Optional<T>> newTcs = new TaskCompletionSource<Optional<T>>( TaskCreationOptions.RunContinuationsAsynchronously );

			if ( previousTcs != null || Interlocked.CompareExchange( ref _nextItemTaskCompletionSource, newTcs, previousTcs ) != previousTcs )
				return Task.FromException<Optional<T>>( new PreviousItemNotCompletedException() );

			IAsyncEnumerator<T> currentEnumerator = Volatile.Read( ref _currentEnumerator );
			if ( currentEnumerator != null )
				AwaitNextInnerEnumeratorItemAndCompleteTcsAsync( currentEnumerator );
			else
				BoxedStateMachine.MoveNext();

			return newTcs.Task;
		}

		private async void AwaitNextInnerEnumeratorItemAndCompleteTcsAsync( IAsyncEnumerator<T> currentEnumerator )
		{
			try
			{
				Optional<T> item = await currentEnumerator.GetNextAsync().ConfigureAwait( false );
				if ( item.HasValue )
				{
					TaskCompletionSource<Optional<T>> tcs = Interlocked.Exchange( ref _nextItemTaskCompletionSource, null );
					tcs.SetResult( item );
				}
				else
				{
					Volatile.Write( ref _currentEnumerator, null );
					BoxedStateMachine.MoveNext();
				}
			}
			catch ( Exception ex )
			{
				CompleteCurrentItem( ex );
			}
		}
	}
}