using System;

namespace PracticalCompiler
{
    public enum Tokens
    {
        Identifier,
        Symbol,
        Operator,
        Bracket,
        Text,
        Number,
    }

    public enum Symbols
    {
        Type,
        TypeOf,
        Import,
        Struct,
        New,
        Let,
        Lambda,
        
        Dot,
        Equals,
        Separator,

        Arrow,
        HasType,
        SubType,
    }
    
    public enum Brackets
    {
        Angle,
        Curly,
        Square,
        Round,
    }

    public abstract class Token
    {
        public readonly Tokens Tag;

        private Token(Tokens tag)
        {
            Tag = tag;
        }

        public sealed class Identifier : Token, IEquatable<Identifier>
        {
            public readonly string Content;

            public Identifier(string content)
                : base(Tokens.Identifier)
            {
                Content = content;
            }

            public bool Equals(Identifier other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return string.Equals(Content, other.Content);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is Identifier && Equals((Identifier) obj);
            }

            public override int GetHashCode()
            {
                return (Content != null ? Content.GetHashCode() : 0);
            }
        }

        public sealed class Symbol : Token, IEquatable<Symbol>
        {
            public readonly Symbols Content;

            public Symbol(Symbols content)
                : base(Tokens.Symbol)
            {
                Content = content;
            }

            public bool Equals(Symbol other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Content == other.Content;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is Symbol && Equals((Symbol) obj);
            }

            public override int GetHashCode()
            {
                return (int) Content;
            }
        }

        public sealed class Operator : Token, IEquatable<Operator>
        {
            public readonly string Content;

            public Operator(string content)
                : base(Tokens.Operator)
            {
                Content = content;
            }

            public bool Equals(Operator other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Content == other.Content;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is Operator && Equals((Operator) obj);
            }

            public override int GetHashCode()
            {
                return (Content != null ? Content.GetHashCode() : 0);
            }
        }

        public sealed class Bracket : Token, IEquatable<Bracket>
        {
            public readonly IBracket<Brackets> Content;

            public Bracket(IBracket<Brackets> content)
                : base(Tokens.Bracket)
            {
                Content = content;
            }

            public bool Equals(Bracket other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Equals(Content, other.Content);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is Bracket && Equals((Bracket) obj);
            }

            public override int GetHashCode()
            {
                return (Content != null ? Content.GetHashCode() : 0);
            }
        }

        public sealed class Text : Token, IEquatable<Text>
        {
            public readonly string Content;

            public Text(string content)
                : base(Tokens.Text)
            {
                Content = content;
            }

            public bool Equals(Text other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return string.Equals(Content, other.Content);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is Text && Equals((Text) obj);
            }

            public override int GetHashCode()
            {
                return (Content != null ? Content.GetHashCode() : 0);
            }
        }

        public sealed class Number : Token, IEquatable<Number>
        {
            public readonly uint Content;

            public Number(uint content)
                : base(Tokens.Number)
            {
                Content = content;
            }

            public bool Equals(Number other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Content == other.Content;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is Number && Equals((Number) obj);
            }

            public override int GetHashCode()
            {
                return (int) Content;
            }
        }
    }
}