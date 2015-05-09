using System;

namespace PracticalCompiler.Metadata
{
    public interface IExpectation<T, M>
    {
        ISet<T> Extension(M meta);

        M Unknown(); // Extension Unknown == always true
        M Definite(T element); // Extension (Definite x) .Contains y -> (x == y)
        M Union(M left, M right); // Union (Extension left) (Extension right) <: Extension (Union left right)
        // (sum T \x. 1) > 0
    }

    public sealed class Expectation<T, M> : IExpectation<T, M>
    {
        private readonly Func<M, ISet<T>> ExtensionF;
        private readonly Func<M> UnknownF;
        private readonly Func<T, M> DefiniteF;
        private readonly Func<M, M, M> UnionF; 

        public Expectation(Func<M, ISet<T>> extensionF, Func<M> unknownF, Func<T, M> definiteF, Func<M, M, M> unionF)
        {
            ExtensionF = extensionF;
            UnknownF = unknownF;
            DefiniteF = definiteF;
            UnionF = unionF;
        }

        public ISet<T> Extension(M meta)
        {
            return this.ExtensionF(meta);
        }

        public M Unknown()
        {
            return this.UnknownF();
        }

        public M Definite(T element)
        {
            return this.DefiniteF(element);
        }

        public M Union(M left, M right)
        {
            return this.UnionF(left, right);
        }
    }
}