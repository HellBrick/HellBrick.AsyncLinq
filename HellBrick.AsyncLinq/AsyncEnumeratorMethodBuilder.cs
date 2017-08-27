using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace HellBrick.AsyncLinq
{
	public partial struct AsyncEnumeratorMethodBuilder<T> : IEquatable<AsyncEnumeratorMethodBuilder<T>>
	{
		private readonly StateMachineAsyncEnumerator<T> _enumerator;

		private AsyncEnumeratorMethodBuilder( StateMachineAsyncEnumerator<T> enumerator ) => _enumerator = enumerator;
		public static AsyncEnumeratorMethodBuilder<T> Create() => new AsyncEnumeratorMethodBuilder<T>( new StateMachineAsyncEnumerator<T>() );

		public void Start<TStateMachine>( ref TStateMachine stateMachine )
			where TStateMachine : IAsyncStateMachine
		{
			IAsyncStateMachine boxedStateMachine = stateMachine;
			stateMachine.SetStateMachine( boxedStateMachine );
			_enumerator.BoxedStateMachine = boxedStateMachine;
		}

		public void SetStateMachine( IAsyncStateMachine boxedStateMachine ) { }

		/// <remarks>
		/// This method is called when the async method returns.
		/// The compiler forces the user to return some value when the async method return type is generic (which <see cref="IAsyncEnumerator{T}"/>) obviously is,
		/// but the return value isn't really needed in the context of the enumerator state machine we're building.
		/// That's why <paramref name="result"/> is simply ignored.
		/// </remarks>
		public void SetResult( T result ) { }

		public void SetException( Exception exception ) => _enumerator.CompleteCurrentItem( exception );

		public IAsyncEnumerator<T> Task => _enumerator;

		public void AwaitOnCompleted<TAwaiter, TStateMachine>( ref TAwaiter awaiter, ref TStateMachine stateMachine )
			where TAwaiter : INotifyCompletion
			where TStateMachine : IAsyncStateMachine
			=> OnCompleted( ref awaiter, ( a, c ) => a.OnCompleted( c ) );

		public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>( ref TAwaiter awaiter, ref TStateMachine stateMachine )
			where TAwaiter : ICriticalNotifyCompletion
			where TStateMachine : IAsyncStateMachine
			=> OnCompleted( ref awaiter, ( a, c ) => a.UnsafeOnCompleted( c ) );

		private void OnCompleted<TAwaiter>( ref TAwaiter awaiter, Action<TAwaiter, Action> onCompletedScheduler )
		{
			if ( typeof( TAwaiter ) == typeof( AsyncYield.AsyncItemAwaitable<T> ) )
			{
				ref AsyncYield.AsyncItemAwaitable<T> itemAwaiter = ref Unsafe.As<TAwaiter, AsyncYield.AsyncItemAwaitable<T>>( ref awaiter );
				Optional<T> newItem = new Optional<T>( itemAwaiter.Item );
				_enumerator.CompleteCurrentItem( newItem );
			}
			else if ( typeof( TAwaiter ) == typeof( AsyncYield.AsyncBreakAwaitable<T> ) )
			{
				_enumerator.CompleteCurrentItem( Optional<T>.NoValue );
			}
			else
			{
				IAsyncStateMachine boxedStateMachine = _enumerator.BoxedStateMachine;
				onCompletedScheduler( awaiter, () => boxedStateMachine.MoveNext() );
			}
		}

		#region IEquatable<AsyncEnumeratorMethodBuilder<T>>

		public override int GetHashCode() => EqualityComparer<StateMachineAsyncEnumerator<T>>.Default.GetHashCode( _enumerator );
		public bool Equals( AsyncEnumeratorMethodBuilder<T> other ) => EqualityComparer<StateMachineAsyncEnumerator<T>>.Default.Equals( _enumerator, other._enumerator );
		public override bool Equals( object obj ) => obj is AsyncEnumeratorMethodBuilder<T> && Equals( (AsyncEnumeratorMethodBuilder<T>) obj );

		public static bool operator ==( AsyncEnumeratorMethodBuilder<T> x, AsyncEnumeratorMethodBuilder<T> y ) => x.Equals( y );
		public static bool operator !=( AsyncEnumeratorMethodBuilder<T> x, AsyncEnumeratorMethodBuilder<T> y ) => !x.Equals( y );

		#endregion
	}
}
