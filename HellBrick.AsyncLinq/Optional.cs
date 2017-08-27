using System;
using System.Collections.Generic;

namespace HellBrick.AsyncLinq
{
	public struct Optional<T> : IEquatable<Optional<T>>
	{
#pragma warning disable IDE0034 // Simplify 'default' expression
		public static Optional<T> NoValue { get; } = default( Optional<T> );
#pragma warning restore IDE0034 // Simplify 'default' expression

		public Optional( T value ) => (HasValue, Value) = (true, value);

		public bool HasValue { get; }
		public T Value { get; }

		public override string ToString() => HasValue ? Value.ToString() : "<no value>";

		public override int GetHashCode()
		{
			unchecked
			{
				const int prime = -1521134295;
				int hash = 12345701;
				hash = hash * prime + EqualityComparer<bool>.Default.GetHashCode( HasValue );
				hash = hash * prime + EqualityComparer<T>.Default.GetHashCode( Value );
				return hash;
			}
		}

		public bool Equals( Optional<T> other ) => EqualityComparer<bool>.Default.Equals( HasValue, other.HasValue ) && EqualityComparer<T>.Default.Equals( Value, other.Value );
		public override bool Equals( object obj ) => obj is Optional<T> && Equals( (Optional<T>) obj );

		public static bool operator ==( Optional<T> x, Optional<T> y ) => x.Equals( y );
		public static bool operator !=( Optional<T> x, Optional<T> y ) => !x.Equals( y );
	}
}
