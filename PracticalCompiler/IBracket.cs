using System;
using System.Collections.Generic;

namespace PracticalCompiler
{
    public interface IBracket<out B>
    {
        B Type { get; }
        Boundaries Boundary { get; }
    }

    public enum Boundaries
    {
        Open,
        Close,
    }

    public sealed class Bracket<B> : IBracket<B>, IEquatable<IBracket<B>>
    {
        public B Type { get; private set; }
        public Boundaries Boundary { get; private set; }

        public Bracket(B type, Boundaries boundary)
        {
            Type = type;
            Boundary = boundary;
        }

        public bool Equals(IBracket<B> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EqualityComparer<B>.Default.Equals(Type, other.Type) && Boundary == other.Boundary;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is IBracket<B> && Equals((IBracket<B>)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (EqualityComparer<B>.Default.GetHashCode(Type) * 397) ^ (int)Boundary;
            }
        }
    }

    public interface IBracketing
    {
        char At(Boundaries boundary);
    }

    public sealed class Bracketing : IBracketing
    {
        private readonly char Open;
        private readonly char Close;

        public Bracketing(char open, char close)
        {
            Open = open;
            Close = close;
        }

        public char At(Boundaries boundary)
        {
            switch (boundary)
            {
                case Boundaries.Open:

                    return this.Open;
                case Boundaries.Close:

                    return this.Close;
                default:
                    throw new InvalidProgramException("Should never happen.");
            }
        }
    }
}