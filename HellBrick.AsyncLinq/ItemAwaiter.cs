using System;
using System.Runtime.CompilerServices;

namespace HellBrick.AsyncLinq
{
	public struct ItemAwaiter<T> : ICriticalNotifyCompletion
	{
		private readonly StateMachineAsyncEnumerator<T> _enumerator;

		internal ItemAwaiter( StateMachineAsyncEnumerator<T> enumerator ) => _enumerator = enumerator;

		public bool IsCompleted => _enumerator.IsCompleted;
		public Optional<T> GetResult() => _enumerator.GetCurrentItem();

		public void OnCompleted( Action continuation ) => _enumerator.NextItemTask.ConfigureAwait( false ).GetAwaiter().OnCompleted( continuation );
		public void UnsafeOnCompleted( Action continuation ) => _enumerator.NextItemTask.ConfigureAwait( false ).GetAwaiter().UnsafeOnCompleted( continuation );
	}

	public struct SyncContextItemAwaitable<T> : ICriticalNotifyCompletion
	{
		private readonly StateMachineAsyncEnumerator<T> _enumerator;

		internal SyncContextItemAwaitable( StateMachineAsyncEnumerator<T> enumerator ) => _enumerator = enumerator;

		public SyncContextItemAwaitable<T> GetAwaiter() => this;

		public bool IsCompleted => _enumerator.IsCompleted;
		public Optional<T> GetResult() => _enumerator.GetCurrentItem();

		public void OnCompleted( Action continuation ) => _enumerator.NextItemTask.ConfigureAwait( true ).GetAwaiter().OnCompleted( continuation );
		public void UnsafeOnCompleted( Action continuation ) => _enumerator.NextItemTask.ConfigureAwait( true ).GetAwaiter().UnsafeOnCompleted( continuation );
	}
}
