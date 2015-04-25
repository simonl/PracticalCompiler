namespace PracticalCompiler
{
    public sealed class Environment<T>
    {
        public readonly string Identifier;
        public readonly T Binding;
        public readonly Environment<T> Next;

        public Environment(string identifier, T binding, Environment<T> next)
        {
            Identifier = identifier;
            Binding = binding;
            Next = next;
        }
    }
}