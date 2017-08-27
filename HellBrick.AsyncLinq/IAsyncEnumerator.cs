using System.Threading.Tasks;

namespace HellBrick.AsyncLinq
{
	public interface IAsyncEnumerator<T>
	{
		Task<Optional<T>> GetNextAsync();
	}
}
