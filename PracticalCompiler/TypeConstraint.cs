namespace PracticalCompiler
{
    public enum TypeConstraints
    {
        None,
        Type,
        Class,
    }

    public abstract class TypeConstraint
    {
        public readonly TypeConstraints Tag;

        private TypeConstraint(TypeConstraints tag)
        {
            Tag = tag;
        }

        public sealed class None : TypeConstraint
        {
            public None()
                : base(TypeConstraints.None)
            {
                
            }
        }

        public sealed class Type : TypeConstraint
        {
            public readonly Term Content;

            public Type(Term content)
                : base(TypeConstraints.Type)
            {
                Content = content;
            }
        }

        public sealed class Class : TypeConstraint
        {
            public readonly Term Content;

            public Class(Term content)
                : base(TypeConstraints.Class)
            {
                Content = content;
            }
        }
    }
}