using System;
using System.Threading.Tasks;
using FluentAssertions;
using HellBrick.AsyncLinq.Test.Helpers;
using Xunit;

namespace HellBrick.AsyncLinq.Test
{
	public class ToArrayTests
	{
		[Fact]
		public async Task ReturnsEmptyArrayForEmptySequence()
		{
			int[] array = await AsyncEnumerator.Empty<int>().ToArray().ConfigureAwait( true );
			array.Should().BeEmpty();
		}

		[Fact]
		public async Task ReturnsItemsInOrder()
		{
			Task<int>[] itemTasks = new Task<int>[]
			{
				Task.FromResult( 42 ),
				Task.Delay( 100 ).ContinueWith( _ => 64 ),
				Task.FromResult( 128 )
			};

			IAsyncEnumerator<int> enumerator = new TaskAsyncEnumerator<int>( itemTasks );
			int[] array = await enumerator.ToArray().ConfigureAwait( true );

			int[] expectedItems = await Task.WhenAll( itemTasks ).ConfigureAwait( true );
			array.Should().HaveEquivalentItems( expectedItems );
		}

		[Fact]
		public void ItemExceptionIsPropagated()
		{
			IAsyncEnumerator<int> enumerator = new TaskAsyncEnumerator<int>
			(
				Task.FromResult( 42 ),
				Task.FromException<int>( new InvalidOperationException() )
			);

			Func<Task<int[]>> faultyAct = () => enumerator.ToArray();
			faultyAct.ShouldThrow<InvalidOperationException>();
		}
	}
}
