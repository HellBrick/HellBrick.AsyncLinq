using System.Threading.Tasks;

namespace HellBrick.AsyncLinq
{
	public static partial class AsyncEnumerator
	{
		public static Task<T[]> ToArray<T>( this IAsyncEnumerator<T> asyncEnumerator )
			=> asyncEnumerator.ToList()
			.ContinueWith( listTask => listTask.GetAwaiter().GetResult().ToArray() );
	}
}
