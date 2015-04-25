using System;

namespace PracticalCompiler
{
    public sealed class Unit : IEquatable<Unit>
    {
        public static readonly Unit Singleton = new Unit();

        private Unit()
        {

        }

        public bool Equals(Unit other)
        {
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is Unit && Equals((Unit) obj);
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }
}