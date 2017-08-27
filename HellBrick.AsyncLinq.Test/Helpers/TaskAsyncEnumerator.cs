using System.Threading.Tasks;

namespace HellBrick.AsyncLinq.Test.Helpers
{
	internal class TaskAsyncEnumerator<T> : IAsyncEnumerator<T>
	{
		private readonly Task<T>[] _tasks;
		private int _tasksEnumerated = 0;

		public TaskAsyncEnumerator( params Task<T>[] tasks ) => _tasks = tasks;

		public Task<Optional<T>> GetNextAsync()
			=> _tasksEnumerated < _tasks.Length
			? _tasks[ _tasksEnumerated++ ].ContinueWith( t => new Optional<T>( t.GetAwaiter().GetResult() ) )
			: Optional<T>.NoValueTask;
	}
}
