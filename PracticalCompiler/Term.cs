using System;
using System.Collections.Generic;

namespace PracticalCompiler
{
    public enum Productions
    {
        Universe,

        Pair,
        Cons,

        Arrow,
        Lambda,
        Apply,

        Module,
        New,
        Access,

        Generic,
        Variable,
        Annotation,
        LetBinding,
        Constant,
        TypeOf,
        Import,
    }

    public sealed class Universes
    {
        public readonly uint Rank;

        public Universes(uint rank)
        {
            Rank = rank;
        }
    }

    public sealed class PairType
    {
        public readonly Term Left;
        public readonly Option<string> Identifier;
        public readonly Term Right;

        public PairType(Term left, Option<string> identifier, Term right)
        {
            Left = left;
            Identifier = identifier;
            Right = right;
        }
    }

    public sealed class ConsNode
    {
        public readonly Term Left;
        public readonly Term Right;

        public ConsNode(Term left, Term right)
        {
            Left = left;
            Right = right;
        }
    }

    public sealed class ArrowType
    {
        public readonly Term From;
        public readonly Option<string> Identifier;
        public readonly Term To;

        public ArrowType(Term @from, Option<string> identifier, Term to)
        {
            From = @from;
            Identifier = identifier;
            To = to;
        }
    }

    public sealed class LambdaTerm
    {
        public readonly Declaration Parameter;
        public readonly Term Body;

        public LambdaTerm(Declaration parameter, Term body)
        {
            Parameter = parameter;
            Body = body;
        }
    }

    public sealed class FunctionApply
    {
        public readonly Term Operator;
        public readonly Term Argument;

        public FunctionApply(Term @operator, Term argument)
        {
            Operator = @operator;
            Argument = argument;
        }
    }

    public sealed class ModuleType
    {
        public readonly KeyValuePair<string, Term>[] Members;

        public ModuleType(KeyValuePair<string, Term>[] members)
        {
            Members = members;
        }
    }

    public sealed class NewStruct
    {
        public readonly Definition[] Members;

        public NewStruct(Definition[] members)
        {
            Members = members;
        }
    }

    public sealed class MemberAccess
    {
        public readonly Term Operator;
        public readonly string Name;

        public MemberAccess(Term @operator, string name)
        {
            Operator = @operator;
            Name = name;
        }
    }

    public sealed class Annotated
    {
        public readonly Term Type;
        public readonly Term Term;

        public Annotated(Term type, Term term)
        {
            Type = type;
            Term = term;
        }
    }

    public sealed class Declaration
    {
        public readonly Option<Term> Type;
        public readonly string Identifier;

        public Declaration(Option<Term> type, string identifier)
        {
            Type = type;
            Identifier = identifier;
        }
    }

    public abstract class Term
    {
        public readonly Productions Tag;

        private Term(Productions tag)
        {
            Tag = tag;
        }

        public sealed class Universe : Term
        {
            public readonly Universes Content;

            public Universe(Universes content)
                : base(Productions.Universe)
            {
                Content = content;
            }
        }

        public sealed class Pair : Term
        {
            public readonly PairType Content;

            public Pair(PairType content)
                : base(Productions.Pair)
            {
                Content = content;
            }
        }

        public sealed class Cons : Term
        {
            public readonly ConsNode Content;

            public Cons(ConsNode content)
                : base(Productions.Cons)
            {
                Content = content;
            }
        }

        public sealed class Arrow : Term
        {
            public readonly ArrowType Content;

            public Arrow(ArrowType content)
                : base(Productions.Arrow)
            {
                Content = content;
            }
        }

        public sealed class Lambda : Term
        {
            public readonly LambdaTerm Content;

            public Lambda(LambdaTerm content)
                : base(Productions.Lambda)
            {
                Content = content;
            }
        }

        public sealed class Apply : Term
        {
            public readonly FunctionApply Content;

            public Apply(FunctionApply content)
                : base(Productions.Apply)
            {
                Content = content;
            }
        }

        public sealed class Module : Term
        {
            public readonly ModuleType Content;

            public Module(ModuleType content)
                : base(Productions.Module)
            {
                Content = content;
            }
        }

        public sealed class New : Term
        {
            public readonly NewStruct Content;

            public New(NewStruct content)
                : base(Productions.New)
            {
                Content = content;
            }
        }

        public sealed class Access : Term
        {
            public readonly MemberAccess Content;

            public Access(MemberAccess content)
                : base(Productions.Access)
            {
                Content = content;
            }
        }

        public sealed class Generic : Term
        {
            public readonly Declaration Content;

            public Generic(Declaration content)
                : base(Productions.Generic)
            {
                Content = content;
            }
        }

        public sealed class Variable : Term
        {
            public readonly string Content;

            public Variable(string content)
                : base(Productions.Variable)
            {
                Content = content;
            }
        }

        public sealed class Annotation : Term
        {
            public readonly Annotated Content;

            public Annotation(Annotated content)
                : base(Productions.Annotation)
            {
                Content = content;
            }
        }

        public sealed class LetBinding : Term
        {
            public readonly Definition Content;
            public readonly Term Continuation;

            public LetBinding(Definition content, Term continuation)
                : base(Productions.LetBinding)
            {
                Content = content;
                Continuation = continuation;
            }
        }

        public sealed class Constant : Term
        {
            public readonly Classification<dynamic> Content;

            public Constant(Classification<dynamic> content)
                : base(Productions.Constant)
            {
                Content = content;
            }
        }

        public sealed class TypeOf : Term
        {
            public readonly Term Content;

            public TypeOf(Term content)
                : base(Productions.TypeOf)
            {
                Content = content;
            }
        }

        public sealed class Import : Term
        {
            public readonly string Filename;

            public Import(string filename)
                : base(Productions.Import)
            {
                Filename = filename;
            }
        }
    }
}