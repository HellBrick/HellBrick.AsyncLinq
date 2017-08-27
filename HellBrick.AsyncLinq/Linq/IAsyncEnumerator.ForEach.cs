using System;
using System.Threading.Tasks;

namespace HellBrick.AsyncLinq
{
	public static partial class AsyncEnumerator
	{
		public static Task ForEach<T>( this IAsyncEnumerator<T> asyncEnumerator, Action<T> action )
			=> asyncEnumerator.ForEach( action, ( item, actionArg ) => actionArg( item ) );

		public static async Task ForEach<TItem, TState>( this IAsyncEnumerator<TItem> asyncEnumerator, TState state, Action<TItem, TState> action )
		{
			while ( true )
			{
				Optional<TItem> item = await asyncEnumerator.GetNextAsync().ConfigureAwait( false );
				if ( !item.HasValue )
					break;

				action( item.Value, state );
			}
		}
	}
}
