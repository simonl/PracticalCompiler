using System;

namespace PracticalCompiler.Metadata
{
    public interface IRange
    {
        uint Minimum { get; }
        Number Maximum { get; }
    }

    public sealed class Range : IRange
    {
        public uint Minimum { get; private set; }
        public Number Maximum { get; private set; }

        public Range(uint minimum, Number maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }
    }

    public static class Ranges
    {
        public static ISet<uint> Extension(IRange meta)
        {
            return new Set<uint>(
                containsF: count =>
                {
                    if (count < meta.Minimum)
                    {
                        return false;
                    }

                    var maximum = meta.Maximum.Extension();

                    return maximum.Contains(count);
                });
        }

        public static IRange Unknown()
        {
            return new Range(
                minimum: 0,
                maximum: new Number.Infinite());
        }

        public static IRange Definite(uint count)
        {
            return new Range(
                minimum: count,
                maximum: new Number.Finite(count + 1));
        }

        public static IRange Union(IRange left, IRange right)
        {
            return new Range(
                minimum: Math.Min(left.Minimum, right.Minimum),
                maximum: Numbers.Maximum(left.Maximum, right.Maximum));
        }
    }
}