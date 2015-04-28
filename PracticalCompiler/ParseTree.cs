using System;

namespace PracticalCompiler
{
    public sealed class ParseTree
    {
        public readonly Script Content;

        public ParseTree(Script content)
        {
            Content = content;
        }
    }

    public sealed class Script
    {
        public readonly Element[] Bindings;
        public readonly Term Body;

        public Script(Element[] bindings, Term body)
        {
            Bindings = bindings;
            Body = body;
        }
    }

    public enum Elements
    {
        Declaration,
        Definition,
    }

    public abstract class Element
    {
        public readonly Elements Tag;

        private Element(Elements tag)
        {
            Tag = tag;
        }

        public sealed class Declaration : Element
        {
            public readonly PracticalCompiler.Declaration Content;

            public Declaration(PracticalCompiler.Declaration content)
                : base(Elements.Declaration)
            {
                Content = content;
            }
        }

        public sealed class Definition : Element
        {
            public readonly PracticalCompiler.Definition Content;

            public Definition(PracticalCompiler.Definition content)
                : base(Elements.Definition)
            {
                Content = content;
            }
        }
    }

    public sealed class Definition
    {
        public readonly string Identifier;
        public readonly Term Body;

        public Definition(string identifier, Term body)
        {
            Identifier = identifier;
            Body = body;
        }
    }
}