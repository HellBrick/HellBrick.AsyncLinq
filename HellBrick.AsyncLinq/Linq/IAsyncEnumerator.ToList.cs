using System.Collections.Generic;
using System.Threading.Tasks;

namespace HellBrick.AsyncLinq
{
	public static partial class AsyncEnumerator
	{
		public static async Task<List<T>> ToList<T>( this IAsyncEnumerator<T> asyncEnumerator )
		{
			List<T> itemList = new List<T>();
			await asyncEnumerator.ForEach( itemList, ( item, list ) => list.Add( item ) ).ConfigureAwait( false );
			return itemList;
		}
	}
}
