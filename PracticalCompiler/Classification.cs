namespace PracticalCompiler
{
    public sealed class Classification<T>
    {
        public readonly Universes Universe;
        public readonly TypedTerm Type;
        public readonly T Term;

        public Classification(Universes universe, TypedTerm type, T term)
        {
            Universe = universe;
            Type = type;
            Term = term;
        }
    }
}