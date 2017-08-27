using System;
using System.Runtime.CompilerServices;

namespace HellBrick.AsyncLinq
{
	public static class AsyncYield
	{
		public static AsyncItemAwaitable<T> Item<T>( T item ) => new AsyncItemAwaitable<T>( item );
		public static AsyncBreakAwaitable<T> Break<T>() => new AsyncBreakAwaitable<T>();

		public struct AsyncItemAwaitable<T> : ICriticalNotifyCompletion, IEquatable<AsyncItemAwaitable<T>>
		{
			public AsyncItemAwaitable( T item ) => Item = item;

			public T Item { get; }
			public AsyncItemAwaitable<T> GetAwaiter() => this;

			public bool IsCompleted => false;
			public void GetResult() { }
			public void OnCompleted( Action continuation ) => throw new NotSupportedException();
			public void UnsafeOnCompleted( Action continuation ) => throw new NotSupportedException();

			#region IEquatable<AsyncItemAwaitable<T>>

			public override int GetHashCode() => System.Collections.Generic.EqualityComparer<T>.Default.GetHashCode( Item );
			public bool Equals( AsyncItemAwaitable<T> other ) => System.Collections.Generic.EqualityComparer<T>.Default.Equals( Item, other.Item );
			public override bool Equals( object obj ) => obj is AsyncItemAwaitable<T> && Equals( (AsyncItemAwaitable<T>) obj );

			public static bool operator ==( AsyncItemAwaitable<T> x, AsyncItemAwaitable<T> y ) => x.Equals( y );
			public static bool operator !=( AsyncItemAwaitable<T> x, AsyncItemAwaitable<T> y ) => !x.Equals( y );

			#endregion
		}

		public struct AsyncBreakAwaitable<T> : ICriticalNotifyCompletion, IEquatable<AsyncBreakAwaitable<T>>
		{
			public AsyncBreakAwaitable<T> GetAwaiter() => this;

			public bool IsCompleted => false;
			public T GetResult() => default;
			public void OnCompleted( Action continuation ) => throw new NotSupportedException();
			public void UnsafeOnCompleted( Action continuation ) => throw new NotSupportedException();

			#region IEquatable<AsyncBreakAwaitable<T>>

			public override int GetHashCode() => 0;
			public bool Equals( AsyncBreakAwaitable<T> other ) => true;
			public override bool Equals( object obj ) => obj is AsyncBreakAwaitable<T> && Equals( (AsyncBreakAwaitable<T>) obj );

			public static bool operator ==( AsyncBreakAwaitable<T> x, AsyncBreakAwaitable<T> y ) => x.Equals( y );
			public static bool operator !=( AsyncBreakAwaitable<T> x, AsyncBreakAwaitable<T> y ) => !x.Equals( y );

			#endregion
		}
	}
}
