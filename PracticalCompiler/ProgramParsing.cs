using System;
using System.Collections.Generic;
using System.Linq;

namespace PracticalCompiler
{
    public static class ProgramParsing
    { 
        public static IParser<Token, Term> CompilationUnit()
        {
            return Script().Fmap(LetBindings);
        }
        
        public static IParser<Token, Statement> CommandLine()
        {
            var command = Parsers.Alternatives<Token, Statement>(
                Literal(Symbols.Let).Continue(_ => Element()),
                Expression().Fmap(_ => new Statement(null, new Option<Term>.None(), new Option<Term>.Some(_))));
            
            return command.Fmap(element => element.Content);
        }

        public static Term LetBindings(Script script)
        {
            var definitions = AccumulateDefinitions(script.Bindings);

            return definitions.ReduceRight(script.Body, merge: (@def, body) => new Term.LetBinding(@def, body));
        }

        private static Definition[] AccumulateDefinitions(Statement[] bindings)
        {
            var declarations = new Dictionary<string, Term>(bindings.Length);

            var definitions = new List<Definition>(bindings.Length);

            foreach (var element in bindings)
            {
                foreach (var type in element.Declaration.Each())
                {
                    declarations.Add(element.Identifier, type);
                }

                foreach (var term in element.Definition.Each())
                {
                    var identifier = element.Identifier;

                    if (declarations.ContainsKey(identifier))
                    {
                        var declared = declarations[identifier];

                        declarations.Remove(identifier);

                        var body = new Term.Annotation(new Annotated(declared, term));

                        definitions.Add(new Definition(identifier, body));
                    }
                    else
                    {
                        definitions.Add(new Definition(identifier, term));
                    }
                }
            }

            return definitions.ToArray();
        }

        public static IParser<Token, Script> Script()
        {
            var elements = Element().Before(Literal(Symbols.Separator)).Repeat();

            return elements.Continue(bindings =>
                Expression().Continue(body => 
                    Parsers.Returns<Token, Script>(new Script(bindings, body))));
        }

        public static IParser<Token, Statement> Element()
        {
            var declaration = Literal(Symbols.HasType).Continue(_ => Expression()).Option();

            var definition = Literal(Symbols.Equals).Continue(_ => Expression()).Option();

            return Identifier().Continue(identifier =>
                declaration.Continue(type => 
                definition.Continue(term => Parsers.Returns<Token, Statement>(new Statement(identifier, type, term)))));
        }

        public static IParser<Token, Term> Expression()
        {
            return Separated(PrefixedTerm(), new Token.Symbol(Symbols.Arrow))
                .Fmap(components => components.ReduceRight(MergeArrowComponents))
                .Continue(arrows => Expression().After(Literal(Symbols.HasType)).Option()
                    .Fmap(annotation =>
                    {
                        foreach (var type in annotation.Each())
                        {
                            return new Term.Annotation(new Annotated(type, arrows));
                        }

                        return arrows;
                    }));
        }

        private static Term MergeArrowComponents(Term @from, Term to)
        {
            if (@from.Tag == Productions.Generic)
            {
                var generic = (Term.Generic) @from;

                var type = generic.Content.Type.Or(DefaultGenericType);

                return new Term.Arrow(new ArrowType(type, new Option<string>.Some(generic.Content.Identifier), to));
            }

            return new Term.Arrow(new ArrowType(@from, new Option<string>.None(), to));
        }

        private static Term.Universe DefaultGenericType
        {
            get { return new Term.Universe(new Universes(0)); }
        }

        public static IParser<Token, Term> PrefixedTerm()
        {
            var term = Parsers.Alternatives<Token, Term>(
                Lambda(),
                Literal(Symbols.TypeOf).Continue(_ => SimpleTerm().Fmap(content => (Term)new Term.TypeOf(content))),
                Literal(Symbols.Import).Continue(_ => ConstantString().Fmap(filename => (Term)new Term.Import(filename))),
                Literal(Symbols.Struct).Continue(_ => StructType().Between(Brackets.Curly).Fmap(members => (Term)new Term.Module(members))),
                Literal(Symbols.New).Continue(_ => NewStruct()),
                Term()
            );

            return term.Fmap(element => element.Content);
        }
        
        public static IParser<Token, Term> Term()
        {
            return SimpleTerm().Some().Fmap(components => components.ReduceLeft((@operator, argument) =>
            {
                if (argument.Tag == Productions.Access)
                {
                    var access = (PracticalCompiler.Term.Access) argument;

                    if (access.Content.Operator == null)
                    {
                        return new Term.Access(new MemberAccess(@operator, access.Content.Name));
                    }
                }

                return new Term.Apply(new FunctionApply(@operator, argument));
            }));
        }

        public static IParser<Token, Term> SimpleTerm()
        {
            var term = Parsers.Alternatives<Token, Term>(
                Generic(),
                Literal(Symbols.Type).Fmap(_ => (Term)new Term.Universe(new Universes(0))),
                Identifier().Fmap(identifier => (Term)new Term.Variable(identifier)),
                NumberLiteral().Fmap(number => (Term)new Term.Constant(Program.BaseType.ShiftDown<TypedTerm>(new TypedTerm.Variable("int")).ShiftDown<dynamic>(number))),
                ConstantString().Fmap(text => (Term)new Term.Constant(Program.BaseType.ShiftDown<TypedTerm>(new TypedTerm.Variable("string")).ShiftDown<dynamic>(text))),
                Literal(Symbols.Dot).Continue(_ => Identifier().Fmap(name => (Term)new Term.Access(new MemberAccess(null, name)))),
                Parsers.Delay(() => Between(Expression(), Brackets.Round))
            );

            return term.Fmap(element => element.Content);
        }

        public static IParser<Token, ModuleType> StructType()
        {
            var member = Identifier().Continue(name =>
                Literal(Symbols.HasType).Continue(colon => 
                    Expression().Continue(type =>
                        Literal(Symbols.Separator).Fmap(semicolon => 
                            new KeyValuePair<string, Term>(name, type)))));

            return member.Repeat().Fmap(members => new ModuleType(members));
        }

        public static IParser<Token, Term> NewStruct()
        {
            var elements = Element().Before(Literal(Symbols.Separator)).Repeat();

            var @new = elements.Between(Brackets.Curly)
                .Fmap(bindings => (Term)new Term.New(new NewStruct(AccumulateDefinitions(bindings))));

            return Parsers.Option(SimpleTerm()).Continue(hint =>
            {
                foreach (var type in hint.Each())
                {
                    return @new.Fmap(term => (Term)new Term.Annotation(new Annotated(type, term)));
                }

                return @new;
            });
        }

        private static IParser<Token, Unit> Literal(Symbols symbol)
        {
            return Parsers.Single<Token>(new Token.Symbol(symbol));
        }

        public static IParser<Token, string> Identifier()
        {
            return Parsers.Take<Token>().Satisfies(token => token.Tag == Tokens.Identifier).Fmap(token => ((PracticalCompiler.Token.Identifier)token).Content);
        }

        public static IParser<Token, uint> NumberLiteral()
        {
            return Parsers.Take<Token>().Satisfies(token => token.Tag == Tokens.Number).Fmap(token => ((PracticalCompiler.Token.Number)token).Content);
        }

        public static IParser<Token, string> ConstantString()
        {
            return Parsers.Take<Token>().Satisfies(token => token.Tag == Tokens.Text).Fmap(token => ((PracticalCompiler.Token.Text)token).Content);
        }

        public static IParser<Token, Term> Generic()
        {
            return Identifier()
                .Continue(parameter => Literal(Symbols.HasType).Continue(_ => Expression()).Option().Continue(type => 
                Parsers.Returns<Token, Term>(new Term.Generic(new Declaration(type, parameter)))))
                .Between(Brackets.Square);
        }

        public static IParser<Token, Term> Lambda()
        {
            return Literal(Symbols.Lambda)
                .Continue(lambda => Parameter()
                    .Continue(parameter => Literal(Symbols.Dot)
                        .Continue(tobody => Expression()
                            .Continue(body => Parsers.Returns<Token, Term>(new Term.Lambda(new LambdaTerm(parameter, body)))))));
        }

        private static IParser<Token, Declaration> Parameter()
        {
            return SimpleTerm().Continue(term =>
            {
                Option<Term> type = new Option<Term>.None();
                if (term.Tag == Productions.Generic)
                {
                    var generic = (Term.Generic) term;

                    type = new Option<Term>.Some(generic.Content.Type.Or(DefaultGenericType));

                    term = new Term.Variable(generic.Content.Identifier);
                }

                if (term.Tag == Productions.Annotation)
                {
                    var annotation = (Term.Annotation) term;

                    type = new Option<Term>.Some(annotation.Content.Type);

                    term = annotation.Content.Term;
                }

                if (term.Tag == Productions.Variable)
                {
                    var variable = (Term.Variable) term;

                    return Parsers.Returns<Token, Declaration>(new Declaration(type, variable.Content));
                }

                return Parsers.Fails<Token, Declaration>(new ArgumentException("Parameter should be a variable declaration."));
            });
        }

        public static IParser<char, Token> Token()
        {
            var token = Parsers.Alternatives<char, Token>(
                Word().Fmap(RecognizeKeywords).Satisfies(_ => _ != null),
                Word().Fmap(word => (Token)new Token.Identifier(word)),
                Number().Fmap(number => (Token)new Token.Number(number)),
                Symbol().Fmap(RecognizeSymbols).Satisfies(_ => _ != null),
                Symbol().Fmap(symbol => (Token)new Token.Operator(symbol)),
                Bracket<Brackets>(Bracketing).Fmap(bracket => (Token)new Token.Bracket(bracket)),
                String().Fmap(text => (Token)new Token.Text(text)));

            return token.Fmap(element => element.Content);
        }

        public static Token RecognizeKeywords(string word)
        {
            switch (word)
            {
                case "typeof": return new Token.Symbol(Symbols.TypeOf);
                case "import": return new Token.Symbol(Symbols.Import);
                case "lambda": return new Token.Symbol(Symbols.Lambda);
                case "struct": return new Token.Symbol(Symbols.Struct);
                case "type": return new Token.Symbol(Symbols.Type);
                case "new": return new Token.Symbol(Symbols.New);
                case "let": return new Token.Symbol(Symbols.Let);
                default: return null;
            }
        }

        public static Token RecognizeSymbols(string symbol)
        {
            switch (symbol)
            {
                case ".": return new Token.Symbol(Symbols.Dot);
                case "=": return new Token.Symbol(Symbols.Equals);
                case ";": return new Token.Symbol(Symbols.Separator);
                case ":": return new Token.Symbol(Symbols.HasType);
                case "->": return new Token.Symbol(Symbols.Arrow);
                case "<:": return new Token.Symbol(Symbols.SubType);
                default: return null;
            }
        }

        public static IBracketing Bracketing(Brackets type)
        {
            switch (type)
            {
                case Brackets.Angle: return new Bracketing('<', '>');
                case Brackets.Curly: return new Bracketing('{', '}');
                case Brackets.Square: return new Bracketing('[', ']');
                case Brackets.Round: return new Bracketing('(', ')');
                default:
                    throw new InvalidProgramException("Should never happen.");
            }
        }

        //
        //


        public static IParser<Token, T> Between<T>(this IParser<Token, T> parser, Brackets type)
        {
            var open = new Token.Bracket(new Bracket<Brackets>(type, Boundaries.Open));
            var close = new Token.Bracket(new Bracket<Brackets>(type, Boundaries.Close));

            return Parsers.Single<Token>(open).Continue(opened => 
                parser.Continue(result =>
                    Parsers.Single<Token>(close).Continue(closed =>
                        Parsers.Returns<Token, T>(result))));
        }

        public static IParser<S, T[]> Separated<S, T>(this IParser<S, T> parser, S separator)
        {
            return parser
                .Continue(first => parser.After(Parsers.Single(separator)).Repeat()
                    .Continue(rest => Parsers.Returns<S, T[]>(ArrayOperations.Concatenate(new T[] { first }, rest))));
        }

        //
        //

        public static IParser<char, string> LineComment()
        {
            var start = Parsers.Sequence(Parsers.Single('/'), Parsers.Single('/')).Fmap(_ => Unit.Singleton);
            var online = Parsers.Take<char>().Satisfies(c => c != '\n');

            return start.Continue(_ => online.Repeat()).Fmap(text => new string(text));
        }

        public static IParser<char, Unit> BlockComment()
        {
            var start = Parsers.Sequence(Parsers.Single('/'), Parsers.Single('*')).Fmap(_ => Unit.Singleton);
            var end = Parsers.Sequence(Parsers.Single('*'), Parsers.Single('/')).Fmap(_ => Unit.Singleton);

            return Nested(start, end);
        }

        public static IParser<S, Unit> Nested<S>(IParser<S, Unit> start, IParser<S, Unit> end)
        {
            return start.Continue(_ => NestedBody(start, end));
        }

        public static IParser<S, Unit> NestedBody<S>(IParser<S, Unit> start, IParser<S, Unit> end)
        {
            var alternatives = Parsers.Alternatives(
                end,
                Parsers.Delay(() => Nested(start, end)).Continue(_ => NestedBody(start, end)),
                Parsers.Take<S>().Continue(_ => NestedBody(start, end)));

            return alternatives.Fmap(_ => _.Content);
        } 

        public static IParser<char, string> String()
        {
            return Parsers.Take<char>().Satisfies(c => c != '"').Repeat().After(Parsers.Single('"')).Before(Parsers.Single('"')).Fmap(text => new string(text));
        } 

        public static IParser<char, T[]> Separated<T>(IParser<char, T> parser)
        {
            var altenatives = Parsers.Alternatives<char, Option<T>>(
                Whitespace().Fmap(_ => (Option<T>)new Option<T>.None()),
                LineComment().Fmap(_ => (Option<T>)new Option<T>.None()),
                BlockComment().Fmap(_ => (Option<T>)new Option<T>.None()),
                parser.Fmap(_ => (Option<T>)new Option<T>.Some(_)));

            var segments = altenatives.Fmap(element => element.Content).Repeat().Fmap(ArrayOperations.Filter<T>);

            return segments;
        }

        public static IParser<char, string> Text(Func<char, bool> condition)
        {
            return Parsers.Take<char>().Satisfies(condition).Some().Fmap(text => new string(text));
        } 

        public static IParser<char, string> Whitespace()
        {
            return Text(Char.IsWhiteSpace);
        } 

        public static IParser<char, string> Word()
        {
            return Text(@char => Char.IsLetter(@char) || @char == '_');
        }

        public static IParser<char, uint> Number()
        {
            return Text(Char.IsDigit).Fmap(uint.Parse);
        }

        public static readonly string SymbolElements = "=*-/:;,.+!&~?|^%$#<>";

        public static bool IsSymbol(char @char)
        {
            return SymbolElements.IndexOfAny(new char[] { @char }) != -1;
        }

        public static IParser<char, string> Symbol()
        {
            return Text(IsSymbol);
        }

        public static IParser<char, IBracket<B>> Bracket<B>(Func<B, IBracketing> bracketing)
        {
            var alternatives = new List<IParser<char, IBracket<B>>>();

            foreach (B type in Enum.GetValues(typeof (B)))
            {
                var type1 = type;

                var alternative = Bracket(bracketing(type)).Fmap(_ => (IBracket<B>)new Bracket<B>(type1, _));

                alternatives.Add(alternative);
            }

            var bracket = Parsers.Alternatives<char, IBracket<B>>(alternatives.ToArray());

            return bracket.Fmap(element => element.Content);
        }

        public static IParser<char, Boundaries> Bracket(IBracketing bracketing)
        {
            return Parsers.Take<char>().Continue<char, char, Boundaries>(
                generate: @char =>
                {
                    var boundary = RecognizeBrackets(bracketing, @char);

                    if (boundary == null)
                    {
                        return Parsers.Fails<char, Boundaries>(new NotSupportedException("Character is not a bracket: " + @char));
                    }

                    return Parsers.Returns<char, Boundaries>(boundary.Value);
                });
        }
        
        public static Boundaries? RecognizeBrackets(IBracketing bracketing, char @char)
        {
            foreach (Boundaries boundary in Enum.GetValues(typeof (Boundaries)))
            {
                if (@char == bracketing.At(boundary))
                {
                    return boundary;
                }
            }

            return null;
        }
    }
}