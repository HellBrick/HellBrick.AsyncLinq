using System;

namespace HellBrick.AsyncLinq
{
	public class PreviousItemNotCompletedException : Exception
	{
		private static readonly string _errorMessage
			= $"{nameof( IAsyncEnumerator<object> )}.{nameof( IAsyncEnumerator<object>.GetNextAsync )} must not be called until the previous item task has completed.";

		public PreviousItemNotCompletedException()
			: base( _errorMessage )
		{
		}
	}
}
