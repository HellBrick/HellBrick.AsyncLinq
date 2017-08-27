namespace HellBrick.AsyncLinq
{
	public static partial class AsyncEnumerator
	{
		public static IAsyncEnumerator<T> Empty<T>() => EmptyAsyncEnumerator<T>.Instance;

		private class EmptyAsyncEnumerator<T> : IAsyncEnumerator<T>
		{
			public static IAsyncEnumerator<T> Instance { get; } = new EmptyAsyncEnumerator<T>();

			public AsyncItem<T> GetNextAsync() => AsyncItem<T>.NoItem;
		}
	}
}
