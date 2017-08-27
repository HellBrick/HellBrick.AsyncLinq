using System.Threading.Tasks;

namespace HellBrick.AsyncLinq.Test.Helpers
{
	internal class TaskAsyncEnumerator<T> : IAsyncEnumerator<T>
	{
		private readonly Task<T>[] _tasks;
		private int _tasksEnumerated = 0;

		public TaskAsyncEnumerator( params Task<T>[] tasks ) => _tasks = tasks;

		public AsyncItem<T> GetNextAsync()
			=> _tasksEnumerated < _tasks.Length
			? new AsyncItem<T>( _tasks[ _tasksEnumerated++ ].ContinueWith( t => new Optional<T>( t.GetAwaiter().GetResult() ) ) )
			: AsyncItem<T>.NoItem;
	}
}
