using System;
using System.Collections.Generic;

namespace PracticalCompiler.Metadata
{
    public enum Cardinality
    {
        Finite,
        Infinite,
    }

    public abstract class Number
    {
        public readonly Cardinality Tag;

        private Number(Cardinality tag)
        {
            Tag = tag;
        }

        public sealed class Finite : Number
        {
            public readonly uint Count;

            public Finite(uint count) 
                : base(Cardinality.Finite)
            {
                Count = count;
            }
        }

        public sealed class Infinite : Number
        {
            public Infinite()
                : base(Cardinality.Infinite)
            {
                
            }
        }
    }

    public static class Numbers
    {
        public static IEnumerable<uint> Count(this Number number)
        {
            switch (number.Tag)
            {
                case Cardinality.Finite:
                    var finite = (Number.Finite) number;

                    yield return finite.Count;
                    yield break;
                case Cardinality.Infinite:

                    yield break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static ISet<uint> Extension(this Number number)
        {
            return new Set<uint>(
                containsF: count =>
                {
                    switch (number.Tag)
                    {
                        case Cardinality.Finite:
                            var finite = (Number.Finite) number;

                            return count < finite.Count;
                        case Cardinality.Infinite:

                            return true;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });
        }

        public static Number Maximum(Number left, Number right)
        {
            foreach (var countL in left.Count())
            {
                foreach (var countR in right.Count())
                {
                    var count = Math.Max(countL, countR);

                    return new Number.Finite(count);
                }
            }

            return new Number.Infinite();
        }
    }
}