using System.Threading.Tasks;

namespace HellBrick.AsyncLinq.Test.Helpers
{
	internal static class TaskAsyncEnumerator
	{
		public static async IAsyncEnumerator<T> Create<T>( params Task<T>[] tasks )
		{
			foreach ( Task<T> task in tasks )
			{
				T item = await task.ConfigureAwait( false );
				await AsyncYield.Item( item );
			}

			return await AsyncYield.Break<T>();
		}
	}
}
