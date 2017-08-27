using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace HellBrick.AsyncLinq
{
	public struct AsyncItem<T> : ICriticalNotifyCompletion, IEquatable<AsyncItem<T>>
	{
		private static readonly Task<Optional<T>> _noItemMarker = new Task<Optional<T>>( () => default );
		public static AsyncItem<T> NoItem { get; } = new AsyncItem<T>( _noItemMarker );

		private readonly T _item;
		private readonly Task<Optional<T>> _task;

		public AsyncItem( Task<Optional<T>> itemTask ) => (_item, _task) = (default, itemTask);
		public AsyncItem( T item ) => (_item, _task) = (item, default);

		public bool IsCompleted
			=> _task == _noItemMarker
			|| _task == null
			|| _task.GetAwaiter().IsCompleted;

		public AsyncItem<T> GetAwaiter() => this;

		public Optional<T> GetResult()
			=> _task == _noItemMarker ? Optional<T>.NoValue
			: _task != null ? _task.GetAwaiter().GetResult()
			: new Optional<T>( _item );

		public void OnCompleted( Action continuation ) => _task.ConfigureAwait( false ).GetAwaiter().OnCompleted( continuation );
		public void UnsafeOnCompleted( Action continuation ) => _task.ConfigureAwait( false ).GetAwaiter().UnsafeOnCompleted( continuation );

		public SyncContextAwaiter WithSyncContext() => new SyncContextAwaiter( this );

		public struct SyncContextAwaiter : ICriticalNotifyCompletion, IEquatable<SyncContextAwaiter>
		{
			private readonly AsyncItem<T> _asyncItem;

			public SyncContextAwaiter( AsyncItem<T> asyncItem ) => _asyncItem = asyncItem;

			public bool IsCompleted => _asyncItem.IsCompleted;
			public SyncContextAwaiter GetAwaiter() => this;
			public Optional<T> GetResult() => _asyncItem.GetResult();

			public void OnCompleted( Action continuation ) => _asyncItem._task.ConfigureAwait( true ).GetAwaiter().OnCompleted( continuation );
			public void UnsafeOnCompleted( Action continuation ) => _asyncItem._task.ConfigureAwait( true ).GetAwaiter().UnsafeOnCompleted( continuation );

			#region IEquatable<NoContextAsyncItemAwaiter>

			public override int GetHashCode() => EqualityComparer<AsyncItem<T>>.Default.GetHashCode( _asyncItem );
			public bool Equals( SyncContextAwaiter other ) => _asyncItem == other._asyncItem;
			public override bool Equals( object obj ) => obj is SyncContextAwaiter && Equals( (SyncContextAwaiter) obj );

			public static bool operator ==( SyncContextAwaiter x, SyncContextAwaiter y ) => x.Equals( y );
			public static bool operator !=( SyncContextAwaiter x, SyncContextAwaiter y ) => !x.Equals( y );

			#endregion
		}

		#region IEquatable<AsyncItem<T>>

		public override int GetHashCode()
		{
			unchecked
			{
				const int prime = -1521134295;
				int hash = 12345701;
				hash = hash * prime + EqualityComparer<T>.Default.GetHashCode( _item );
				hash = hash * prime + EqualityComparer<Task<Optional<T>>>.Default.GetHashCode( _task );
				return hash;
			}
		}

		public bool Equals( AsyncItem<T> other ) => EqualityComparer<T>.Default.Equals( _item, other._item ) && EqualityComparer<Task<Optional<T>>>.Default.Equals( _task, other._task );
		public override bool Equals( object obj ) => obj is AsyncItem<T> && Equals( (AsyncItem<T>) obj );

		public static bool operator ==( AsyncItem<T> x, AsyncItem<T> y ) => x.Equals( y );
		public static bool operator !=( AsyncItem<T> x, AsyncItem<T> y ) => !x.Equals( y );

		#endregion
	}
}
