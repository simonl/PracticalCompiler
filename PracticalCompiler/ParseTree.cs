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
        public readonly Statement[] Bindings;
        public readonly Term Body;

        public Script(Statement[] bindings, Term body)
        {
            Bindings = bindings;
            Body = body;
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

    public sealed class Statement
    {
        public readonly string Identifier;
        public readonly Option<Term> Declaration;
        public readonly Option<Term> Definition;

        public Statement(string identifier, Option<Term> declaration, Option<Term> definition)
        {
            Identifier = identifier;
            Declaration = declaration;
            Definition = definition;
        }
    }
}