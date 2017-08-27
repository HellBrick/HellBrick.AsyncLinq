using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace HellBrick.AsyncLinq.Test
{
	public class AsyncEnumeratorMethodBuilderTests
	{
		[Fact]
		public async Task SingleYieldBreakReturnsEmptySequence()
		{
			IAsyncEnumerator<int> enumerator = SingleYieldBreak();
			int[] items = await enumerator.ToArray().ConfigureAwait( true );
			items.Should().BeEmpty();

			async IAsyncEnumerator<int> SingleYieldBreak() => await AsyncYield.Break<int>();
		}

		[Fact]
		public async Task ItemsWithoutTrueAwaitsAreReturnedCorrectly()
		{
			int[] items = Enumerable.Range( 42, 10 ).ToArray();
			int[] enumeratedItems = await ForeachThroughItems().ToArray().ConfigureAwait( true );

			enumeratedItems.Should().HaveEquivalentItems( items );

			async IAsyncEnumerator<int> ForeachThroughItems()
			{
				foreach ( int item in items )
					await AsyncYield.Item( item );

				return await AsyncYield.Break<int>();
			}
		}

		[Fact]
		public async Task YieldBreakCanInterruptEnumerator()
		{
			bool doNotInlineMePlease = true;
			int[] itemsBeforeYield = Enumerable.Range( 42, 10 ).ToArray();
			int[] itemsAfterYield = Enumerable.Range( 666, 10 ).ToArray();
			int[] enumeratedItems = await ForeachBreakAndForeach().ToArray().ConfigureAwait( true );

			enumeratedItems.Should().HaveEquivalentItems( itemsBeforeYield );

			async IAsyncEnumerator<int> ForeachBreakAndForeach()
			{
				foreach ( int item in itemsBeforeYield )
					await AsyncYield.Item( item );

				if ( doNotInlineMePlease )
					return await AsyncYield.Break<int>();

				foreach ( int item in itemsAfterYield )
					await AsyncYield.Item( item );

				return await AsyncYield.Break<int>();
			}
		}

		[Fact]
		public async Task FinallyBlockIsExecutedOnReturnFromInsideTry()
		{
			bool isFinallyExecuted = false;
			Optional<int> emptyItem = await TryBreakFinally().GetNextAsync().WithSyncContext();
			emptyItem.HasValue.Should().BeFalse();
			isFinallyExecuted.Should().BeTrue();

			async IAsyncEnumerator<int> TryBreakFinally()
			{
				try
				{
					return await AsyncYield.Break<int>();
				}
				finally
				{
					isFinallyExecuted = true;
				}
			}
		}

		[Fact]
		public void FinallyBlockIsExecutedOnExceptionBeingThrownFromInsideTry()
		{
			bool isFinallyExecuted = false;
			Func<Task<Optional<int>>> getItemAction = async () => await TryThrowFinally().GetNextAsync();
			getItemAction.ShouldThrow<Exception>();
			isFinallyExecuted.Should().BeTrue();

			async IAsyncEnumerator<int> TryThrowFinally()
			{
				try
				{
					await Task.Yield();
					throw new Exception();
				}
				finally
				{
					isFinallyExecuted = true;
				}
			}
		}

		[Fact]
		public async Task TrueAwaitsAreSupported()
		{
			const int expectedValue = 42;
			TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
			IAsyncEnumerator<int> enumerator = AwaitTaskThenYieldItem();

			AsyncItem<int> nextItemTask = enumerator.GetNextAsync();
			nextItemTask.IsCompleted.Should().BeFalse();

			tcs.SetResult( expectedValue );
			Optional<int> yieldedItem = await nextItemTask.WithSyncContext();
			yieldedItem.HasValue.Should().BeTrue();
			yieldedItem.Value.Should().Be( expectedValue );

			async IAsyncEnumerator<int> AwaitTaskThenYieldItem()
			{
				int awaitedValue = await tcs.Task.ConfigureAwait( false );
				await AsyncYield.Item( awaitedValue );

				return await AsyncYield.Break<int>();
			}
		}

		[Fact]
		public async Task SynchronousExceptionIsPropagatedToGetNextCall()
		{
			const int expectedValue = 42;
			IAsyncEnumerator<int> enumerator = YieldItemThenThrow();

			Optional<int> item = await enumerator.GetNextAsync().WithSyncContext();
			item.Value.Should().Be( expectedValue );

			Func<Task<Optional<int>>> nextItemAct = async () => await enumerator.GetNextAsync();
			nextItemAct.ShouldThrow<Exception>();

			Optional<int> itemAfterException = await enumerator.GetNextAsync().WithSyncContext();
			itemAfterException.HasValue.Should().BeFalse();

			async IAsyncEnumerator<int> YieldItemThenThrow()
			{
				await AsyncYield.Item( expectedValue );
				throw new Exception( "All you enumerator are belong to us" );
			}
		}

		[Fact]
		public async Task AsyncExceptionIsPropagatedToGetNextCall()
		{
			TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
			IAsyncEnumerator<int> enumerator = AwaitAndYieldItem();

			AsyncItem<int> itemTask = enumerator.GetNextAsync();
			tcs.SetException( new Exception( "The punch has been spiked" ) );

			Func<Task<Optional<int>>> awaitFaultyItemAct = async () => await itemTask;
			awaitFaultyItemAct.ShouldThrow<Exception>();

			Optional<int> itemAfterException = await enumerator.GetNextAsync().WithSyncContext();
			itemAfterException.HasValue.Should().BeFalse();

			async IAsyncEnumerator<int> AwaitAndYieldItem()
			{
				int awaitedItem = await tcs.Task.ConfigureAwait( false );
				await AsyncYield.Item( awaitedItem );
				return await AsyncYield.Break<int>();
			}
		}

		[Fact]
		public async Task ExecutionDoesNotStartUntilNextAsyncIsCalled()
		{
			bool executionStarted = false;

			IAsyncEnumerator<int> enumerator = SideEffectEnumerator();
			executionStarted.Should().BeFalse();

			Optional<int> nextItem = await enumerator.GetNextAsync().WithSyncContext();
			executionStarted.Should().BeTrue();

			async IAsyncEnumerator<int> SideEffectEnumerator()
			{
				executionStarted = true;
				await AsyncYield.Item( 42 );
				return await AsyncYield.Break<int>();
			}
		}

		[Fact]
		public async Task CanCallGetNextAsyncInfinitelyAfterSequenceHasEnded()
		{
			IAsyncEnumerator<int> enumerator = EmptyEnumerator();
			for ( int i = 0; i < 10; i++ )
			{
				Optional<int> asyncItem = await enumerator.GetNextAsync().WithSyncContext();
				asyncItem.HasValue.Should().BeFalse();
			}

			async IAsyncEnumerator<int> EmptyEnumerator()
			{
				return await AsyncYield.Break<int>();
			}
		}

		[Fact]
		public void GetNextThrowsIfCalledBeforePreviousGetNextCompletes()
		{
			TaskCompletionSource<int>[] taskSources = Enumerable.Range( 0, 2 ).Select( _ => new TaskCompletionSource<int>() ).ToArray();
			IAsyncEnumerator<int> enumerator = TaskDependentEnumerator();

			AsyncItem<int> firstItemTask = enumerator.GetNextAsync();
			Func<Task<Optional<int>>> despicableAct = async () => await enumerator.GetNextAsync();

			despicableAct.ShouldThrow<PreviousItemNotCompletedException>();

			async IAsyncEnumerator<int> TaskDependentEnumerator()
			{
				foreach ( TaskCompletionSource<int> tcs in taskSources )
				{
					int item = await tcs.Task.ConfigureAwait( false );
					await AsyncYield.Item( item );
				}

				return await AsyncYield.Break<int>();
			}
		}

		[Fact]
		public async Task CanSwitchToAndFromYieldItems()
		{
			int[] itemsBefore = Enumerable.Range( 100, 5 ).ToArray();
			int[] innerEnumeratorItems = Enumerable.Range( 100, 5 ).ToArray();
			int[] itemsAfter = Enumerable.Range( 300, 5 ).ToArray();

			int[] enumeratedItems = await YieldItemsCombo().ToArray().ConfigureAwait( false );
			int[] expectedItems
				= itemsBefore
				.Concat( innerEnumeratorItems )
				.Concat( itemsAfter )
				.ToArray();

			enumeratedItems.Should().HaveEquivalentItems( expectedItems );

			async IAsyncEnumerator<int> YieldItemsCombo()
			{
				foreach ( int item in itemsBefore )
					await AsyncYield.Item( item );

				await AsyncYield.Items( InnerEnumerator() );

				foreach ( int item in itemsAfter )
					await AsyncYield.Item( item );

				return await AsyncYield.Break<int>();

				async IAsyncEnumerator<int> InnerEnumerator()
				{
					foreach ( int item in innerEnumeratorItems )
					{
						await Task.Yield();
						await AsyncYield.Item( item );
					}

					return await AsyncYield.Break<int>();
				}
			}
		}

		[Fact]
		public async Task AsyncLocalsAreFlowedCorrectlyBetweenYields()
		{
			int[] yieldedItems = await AsyncLocalEnumerator().ToArray().ConfigureAwait( true );
			yieldedItems.Should().HaveEquivalentItems( new int[] { 42, 43, 44 } );

			async IAsyncEnumerator<int> AsyncLocalEnumerator()
			{
				AsyncLocal<int> asyncLocal = new AsyncLocal<int>();

				asyncLocal.Value = 42;
				await AsyncYield.Item( asyncLocal.Value );

				asyncLocal.Value++;
				await AsyncYield.Item( asyncLocal.Value );

				await Task.Delay( 100 ).ConfigureAwait( false );

				asyncLocal.Value++;
				await AsyncYield.Item( asyncLocal.Value );

				return await AsyncYield.Break<int>();
			}
		}

		[Fact]
		public async Task AsynchronousInnerExceptionIsPropagatedToGetNextCall()
		{
			TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
			IAsyncEnumerator<int> enumerator = Outer();

			AsyncItem<int> itemTask = enumerator.GetNextAsync();
			tcs.SetException( new Exception( "Catch me if you can" ) );
			Func<Task<Optional<int>>> getNextAction = async () => await itemTask;
			getNextAction.ShouldThrow<Exception>();

			Optional<int> itemAfterException = await enumerator.GetNextAsync().WithSyncContext();
			itemAfterException.HasValue.Should().BeFalse();

			async IAsyncEnumerator<int> Outer()
			{
				await AsyncYield.Items( Inner() );
				return await AsyncYield.Break<int>();

				async IAsyncEnumerator<int> Inner()
				{
					int value = await tcs.Task.ConfigureAwait( false );
					await AsyncYield.Item( value );
					await AsyncYield.Item( 42 );
					return await AsyncYield.Break<int>();
				}
			}
		}

		[Fact]
		public async Task SynchronousInnerExceptionIsPropagatedToGetNextCall()
		{
			IAsyncEnumerator<int> enumerator = Outer();

			Func<Task<Optional<int>>> itemAct = async () => await enumerator.GetNextAsync();
			itemAct.ShouldThrow<Exception>();

			Optional<int> itemAfterException = await enumerator.GetNextAsync().WithSyncContext();
			itemAfterException.HasValue.Should().BeFalse();

			async IAsyncEnumerator<int> Outer()
			{
				await AsyncYield.Items( new VeryBadAsyncEnumeratorThatThrowsOnGetNext() );
				await AsyncYield.Item( 42 );
				return await AsyncYield.Break<int>();
			}
		}

		private class VeryBadAsyncEnumeratorThatThrowsOnGetNext : IAsyncEnumerator<int>
		{
			public AsyncItem<int> GetNextAsync() => throw new Exception( "Don't do this. Ever." );
		}
	}
}
