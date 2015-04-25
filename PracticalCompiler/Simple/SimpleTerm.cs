namespace PracticalCompiler.Simple
{
    public interface ISimpleClassification<out T>
    {
        SimpleUniverse Universe { get; }
        SimpleTerm Type { get; }
        T Term { get; }
    }

    public enum SimpleUniverse { Types }

    public sealed class SimpleType
    {
        public readonly Polarity Polarity;
        public readonly SimpleTerm Quantifier;
        public readonly SimpleTerm Body;

        public SimpleType(Polarity polarity, SimpleTerm quantifier, SimpleTerm body)
        {
            Polarity = polarity;
            Quantifier = quantifier;
            Body = body;
        }
    }

    public abstract class SimpleConstruct
    {
        public sealed class Forall : SimpleConstruct
        {
            public readonly SimpleLambda Content;

            public Forall(SimpleLambda content)
            {
                Content = content;
            }
        }

        public sealed class Exists : SimpleConstruct
        {
            public readonly SimplePair Content;

            public Exists(SimplePair content)
            {
                Content = content;
            }
        }
    }

    public sealed class SimplePair
    {
        public readonly SimpleTerm Left;
        public readonly SimpleTerm Right;

        public SimplePair(SimpleTerm left, SimpleTerm right)
        {
            Left = left;
            Right = right;
        }
    }

    public sealed class SimpleLambda
    {
        public readonly string Parameter;
        public readonly SimpleTerm Body;

        public SimpleLambda(string parameter, SimpleTerm body)
        {
            Parameter = parameter;
            Body = body;
        }
    }

    public abstract class SimpleOperand
    {
        public sealed class Forall : SimpleOperand
        {
            public readonly SimpleApply Content;

            public Forall(SimpleApply content)
            {
                Content = content;
            }
        }

        public sealed class Exists : SimpleOperand
        {
            public readonly SimpleUnpack Content;

            public Exists(SimpleUnpack content)
            {
                Content = content;
            }
        }
    }

    public sealed class SimpleApply
    {
        public readonly SimpleTerm Argument;

        public SimpleApply(SimpleTerm argument)
        {
            Argument = argument;
        }
    }

    public sealed class SimpleUnpack
    {
        public readonly string Left;
        public readonly string Right;
        public readonly SimpleTerm Continuation;

        public SimpleUnpack(string left, string right, SimpleTerm continuation)
        {
            Left = left;
            Right = right;
            Continuation = continuation;
        }
    }

    public abstract class SimpleTerm
    {
        public readonly Classes Class;

        private SimpleTerm(Classes @class)
        {
            Class = @class;
        }

        public sealed class Universe : SimpleTerm
        {
            public readonly SimpleUniverse Content;

            public Universe(SimpleUniverse content)
                : base(Classes.Universe)
            {
                Content = content;
            }
        }

        public sealed class Type : SimpleTerm
        {
            public readonly SimpleType Content;

            public Type(SimpleType content) 
                : base(Classes.Type)
            {
                Content = content;
            }
        }

        public abstract class Term : SimpleTerm
        {
            public readonly Operations Tag;

            private Term(Operations tag)
                : base(Classes.Term)
            {
                Tag = tag;
            }

            public sealed class Construct : Term
            {
                public readonly SimpleConstruct Content;

                public Construct(SimpleConstruct content)
                    : base(Operations.Construct)
                {
                    Content = content;
                }
            }

            public sealed class Destruct : Term
            {
                public readonly ISimpleClassification<SimpleTerm> Operator;
                public readonly SimpleOperand Operand;

                public Destruct(ISimpleClassification<SimpleTerm> @operator, SimpleOperand operand)
                    : base(Operations.Destruct)
                {
                    Operator = @operator;
                    Operand = operand;
                }
            }
        }
    }
}