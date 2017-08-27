using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using HellBrick.AsyncLinq.Test.Helpers;
using Xunit;

namespace HellBrick.AsyncLinq.Test
{
	public class ForEachTests
	{
		[Fact]
		public async Task LambdaIsCalledForEachItem()
		{
			List<int> callArguments = new List<int>();
			Task<int>[] itemTasks = new Task<int>[]
			{
				Task.FromResult( 42 ),
				Task.Delay( 100 ).ContinueWith( _ => 64 ),
				Task.FromResult( 128 )
			};

			IAsyncEnumerator<int> enumerator = new TaskAsyncEnumerator<int>( itemTasks );
			await enumerator.ForEach( item => callArguments.Add( item ) ).ConfigureAwait( true );

			int[] expectedItems = await Task.WhenAll( itemTasks ).ConfigureAwait( true );
			callArguments.Should().HaveEquivalentItems( expectedItems );
		}

		[Fact]
		public async Task LambdaIsNotCalledIfSequenceIsEmpty()
		{
			bool lambdaCalled = false;
			await AsyncEnumerator.Empty<int>().ForEach( _ => lambdaCalled = true ).ConfigureAwait( true );
			lambdaCalled.Should().BeFalse();
		}

		[Fact]
		public void LambdaExceptionIsPropagated()
		{
			const int dangerousItem = 42;
			IAsyncEnumerator<int> enumerator = new TaskAsyncEnumerator<int>
			(
				Task.Delay( 100 ).ContinueWith( _ => 64 ),
				Task.FromResult( dangerousItem )
			);

			Func<Task> act
				= () =>
				enumerator.ForEach
				(
					value =>
					{
						if ( value == dangerousItem )
							throw new InvalidOperationException();
					}
				);

			act.ShouldThrow<InvalidOperationException>();
		}

		[Fact]
		public void ItemExceptionIsPropagated()
		{
			IAsyncEnumerator<int> enumerator = new TaskAsyncEnumerator<int>
			(
				Task.Delay( 100 ).ContinueWith( _ => 64 ),
				Task.FromException<int>( new InvalidOperationException() )
			);

			Func<Task> act = () => enumerator.ForEach( _ => { } );
			act.ShouldThrow<InvalidOperationException>();
		}
	}
}
