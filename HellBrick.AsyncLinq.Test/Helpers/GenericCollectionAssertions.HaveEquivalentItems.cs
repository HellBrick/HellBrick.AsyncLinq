using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Collections;

namespace HellBrick.AsyncLinq.Test
{
	internal static class GenericCollectionAssertions
	{
		public static AndConstraint<GenericCollectionAssertions<T>> HaveEquivalentItems<T>( this GenericCollectionAssertions<T> should, IReadOnlyCollection<T> expectedCollection )
			=> should.HaveSameCount( expectedCollection )
			.And.StartWith( expectedCollection );
	}
}
