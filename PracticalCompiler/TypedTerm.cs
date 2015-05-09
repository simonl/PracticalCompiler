using System;
using System.Collections;
using System.Collections.Generic;

namespace PracticalCompiler
{
    public sealed class TypedQuantifier
    {
        public readonly Polarity Polarity;
        public readonly Classification<Unit> From;
        public readonly TypedTerm To;

        public TypedQuantifier(Polarity polarity, Classification<Unit> @from, TypedTerm to)
        {
            Polarity = polarity;
            From = @from;
            To = to;
        }
    }

    public sealed class TypedLambda
    {
        public readonly string Identifier;
        public readonly TypedTerm Body;

        public TypedLambda(string identifier, TypedTerm body)
        {
            Identifier = identifier;
            Body = body;
        }
    }

    public sealed class TypedApply
    {
        public readonly TypedTerm Operand;

        public TypedApply(TypedTerm operand)
        {
            Operand = operand;
        }
    }

    public sealed class Signature
    {
        public readonly Classification<string>[] Members;

        public Signature(Classification<string>[] members)
        {
            Members = members;
        }
    }

    public sealed class TypedModule
    {
        public readonly TypedTerm[] Members;

        public TypedModule(TypedTerm[] members)
        {
            Members = members;
        }
    }

    public sealed class TypedMemberAccess
    {
        public readonly uint Member;

        public TypedMemberAccess(uint member)
        {
            Member = member;
        }
    }

    public enum TypeStructs
    {
        Quantified,
        Module,
    }

    public abstract class TypeStruct
    {
        public readonly TypeStructs Tag;

        private TypeStruct(TypeStructs tag)
        {
            Tag = tag;
        }

        public sealed class Quantified : TypeStruct
        {
            public readonly TypedQuantifier Content;

            public Quantified(TypedQuantifier content)
                : base(TypeStructs.Quantified)
            {
                Content = content;
            }
        }

        public sealed class Module : TypeStruct
        {
            public readonly Signature Content;

            public Module(Signature content)
                : base(TypeStructs.Module)
            {
                Content = content;
            }
        }
    }

    public abstract class Constructors
    {
        public readonly TypeStructs Type;

        private Constructors(TypeStructs type)
        {
            Type = type;
        }

        public sealed class Arrow : Constructors
        {
            public readonly TypedLambda Content;

            public Arrow(TypedLambda content)
                : base(TypeStructs.Quantified)
            {
                Content = content;
            }
        }

        public sealed class Module : Constructors
        {
            public readonly TypedModule Content;

            public Module(TypedModule content)
                : base(TypeStructs.Module)
            {
                Content = content;
            }
        }
    }

    public abstract class Destructors
    {
        public readonly TypeStructs Type;

        private Destructors(TypeStructs type)
        {
            Type = type;
        }

        public sealed class Arrow : Destructors
        {
            public readonly TypedApply Content;

            public Arrow(TypedApply content)
                : base(TypeStructs.Quantified)
            {
                Content = content;
            }
        }

        public sealed class Module : Destructors
        {
            public readonly TypedMemberAccess Content;

            public Module(TypedMemberAccess content)
                : base(TypeStructs.Module)
            {
                Content = content;
            }
        }
    }

    public enum TypedProductions
    {
        Universe,
        Type,
        Constructor,
        Destructor,
        Variable,
        Constant,
    }

    public abstract class TypedTerm : IEquatable<TypedTerm>
    {
        #region Equality Members

        public bool Equals(TypedTerm other)
        {
            return this.IsEqualTo(other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            var other = obj as TypedTerm;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int) Tag;
        }

        #endregion

        public readonly TypedProductions Tag;

        private TypedTerm(TypedProductions tag)
        {
            Tag = tag;
        }

        public sealed class Universe : TypedTerm
        {
            public readonly Universes Content;

            public Universe(Universes content)
                : base(TypedProductions.Universe)
            {
                Content = content;
            }
        }

        public sealed class Type : TypedTerm
        {
            public readonly TypeStruct Content;

            public Type(TypeStruct content)
                : base(TypedProductions.Type)
            {
                Content = content;
            }
        }

        public sealed class Constructor : TypedTerm
        {
            public readonly Constructors Content;

            public Constructor(Constructors content)
                : base(TypedProductions.Constructor)
            {
                Content = content;
            }
        }

        public sealed class Destructor : TypedTerm
        {
            public readonly Classification<TypedTerm> Operator; 
            public readonly Destructors Content;

            public Destructor(Classification<TypedTerm> @operator, Destructors content)
                : base(TypedProductions.Destructor)
            {
                Operator = @operator;
                Content = content;
            }
        }

        public sealed class Variable : TypedTerm
        {
            public readonly string Identifier;

            public Variable(string identifier)
                : base(TypedProductions.Variable)
            {
                Identifier = identifier;
            }
        }

        public sealed class Constant : TypedTerm
        {
            public readonly dynamic Value;

            public Constant(dynamic value)
                : base(TypedProductions.Constant)
            {
                Value = value;
            }
        }
    }
}