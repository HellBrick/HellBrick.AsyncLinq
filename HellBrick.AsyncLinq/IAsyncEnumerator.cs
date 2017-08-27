using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace HellBrick.AsyncLinq
{
	[AsyncMethodBuilder( typeof( AsyncEnumeratorMethodBuilder<> ) )]
	public interface IAsyncEnumerator<T>
	{
		ItemAwaiter<T> GetAwaiter();
		SyncContextItemAwaitable<T> WithSyncContext();
	}
}
