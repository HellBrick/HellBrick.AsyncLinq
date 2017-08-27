namespace HellBrick.AsyncLinq
{
	public static partial class AsyncEnumerator
	{
		public static IAsyncEnumerator<T> Empty<T>() => EmptyAsyncEnumerator<T>.Instance;

		private static class EmptyAsyncEnumerator<T>
		{
			public static IAsyncEnumerator<T> Instance { get; } = CreateEmptyEnumerator();
			private static async IAsyncEnumerator<T> CreateEmptyEnumerator() => await AsyncYield.Break<T>();
		}
	}
}
