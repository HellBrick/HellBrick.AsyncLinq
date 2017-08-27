using System;

namespace HellBrick.AsyncLinq
{
	public class PreviousItemNotCompletedException : Exception
	{
		private static readonly string _errorMessage
			= $"You must not request an item from {nameof( IAsyncEnumerator<object> )} until the previous item has been awaited.";

		public PreviousItemNotCompletedException()
			: base( _errorMessage )
		{
		}
	}
}
