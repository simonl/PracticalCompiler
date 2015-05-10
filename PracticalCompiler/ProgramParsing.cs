using System;
using System.Collections.Generic;
using System.Linq;

namespace PracticalCompiler
{
    public static class ProgramParsing
    {
        public static IStream<T> Many<S, T>(IParser<S, T> parser, IStream<S> stream)
        {
            return new Stream<T>(
                unrollF: () =>
                {
                    if (stream.Unroll().Tag == Step.Empty)
                    {
                        return new Step<T>.Empty();
                    }

                    var result = parser.Parse(stream).Wait().Throw();

                    return new Step<T>.Node(result.Content, Many<S, T>(parser, result.Stream));
                });
        } 

        public static IParser<Token, Term> CompilationUnit()
        {
            return Script().Fmap(LetBindings);
        }
        
        public static IParser<Token, Statement> CommandLine()
        {
            var command = Parsers.Alternatives<Token, Statement>(
                Literal(Symbols.Let).Continue(_ => Element()),
                Expression().Fmap(_ => new Statement(null, new Option<Term>.None(), new Option<Term>.Some(_))));

            return command;
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
            var arrows = Separated(PrefixedTerm(), new Token.Symbol(Symbols.Arrow))
                .Fmap(components => components.ReduceRight((left, right) => MergeQuantifiedComponents(Polarity.Forall, left, right)));

            var userOp = Operator().Fmap(_ => (Term)new Term.Variable(_));

            var custom = Separated(arrows, userOp)
                .Fmap(components => components.ReduceRight(MergeUserOperator));

            var packs = Separated(custom, new Token.Symbol(Symbols.Ampersand))
                .Fmap(components => components.ReduceRight((left, right) => MergeQuantifiedComponents(Polarity.Exists, left, right)));

            var annotations = Separated(packs, new Token.Symbol(Symbols.HasType))
                .Fmap(components => components.ReduceRight(MergeAnnotationComponents));
            
            var pairs = Separated(annotations, new Token.Symbol(Symbols.Comma))
                .Fmap(components => components.ReduceRight(MergeConsComponents));

            return pairs;
        }

        private static T ReduceRight<O, T>(this Association<O, T> association, Func<O, T, T, T> merge)
        {
            var index = association.Operators.Length;
            var element = association.Operands[index];

            while (index != 0)
            {
                index--;

                element = merge(association.Operators[index], association.Operands[index], element);
            }

            return element;
        }

        private static Term MergeConsComponents(Term left, Term right)
        {
            return new Term.Cons(new ConsNode(left, right));
        }

        private static Term MergeAnnotationComponents(Term term, Term type)
        {
            return new Term.Annotation(new Annotated(type, term));
        }

        private static Term MergeUserOperator(Term @operator, Term left, Term right)
        {
            if (left.Tag == Productions.Generic)
            {
                var generic = (Term.Generic) left;

                switch (generic.Constraint.Tag)
                {
                    case TypeConstraints.None:

                        return new Term.Apply(new FunctionApply(
                            @operator: @operator,
                            argument: new Term.Lambda(new LambdaTerm(new Declaration(new Option<Term>.Some(TypeChecking.DefaultGenericType), new Option<Term>.None(), generic.Identifier), right))));
                    case TypeConstraints.Type:
                        var annotation = (TypeConstraint.Type) generic.Constraint;

                        return new Term.Apply(new FunctionApply(
                            @operator: @operator, 
                            argument: new Term.Lambda(new LambdaTerm(new Declaration(new Option<Term>.Some(annotation.Content), new Option<Term>.None(), generic.Identifier), right))));
                    case TypeConstraints.Class:

                        throw new ArgumentException("User defined generic operators cannot have class bounds.");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return new Term.Apply(new FunctionApply(new Term.Apply(new FunctionApply(@operator, left)), @right));
        }

        private static Term MergeQuantifiedComponents(Polarity polarity, Term left, Term right)
        {
            if (left.Tag == Productions.Generic)
            {
                var generic = (Term.Generic) left;

                var identifier = generic.Identifier.Some();

                return new Term.Quantified(new QuantifiedType(polarity, generic.Constraint, identifier, right));
            }

            return new Term.Quantified(new QuantifiedType(polarity, new TypeConstraint.Type(left), new Option<string>.None(), right));
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

            return term;
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
                OperatorReference(),
                Identifier().Fmap(identifier => (Term)new Term.Variable(identifier)),
                NumberLiteral().Fmap(number => (Term)new Term.Constant(Program.BaseType.ShiftDown<TypedTerm>(new TypedTerm.Variable("int")).ShiftDown<dynamic>(number))),
                ConstantString().Fmap(text => (Term)new Term.Constant(Program.BaseType.ShiftDown<TypedTerm>(new TypedTerm.Variable("string")).ShiftDown<dynamic>(text))),
                StructAccess().Fmap(name => (Term)new Term.Access(new MemberAccess(null, name))),
                Parsers.Delay(() => Between(Expression(), Brackets.Round))
            );

            return term;
        }

        private static IParser<Token, Term> OperatorReference()
        {
            return Operator().Between(Brackets.Round).Fmap(identifier => (Term) new Term.Variable(identifier));
        } 

        private static IParser<Token, string> StructAccess()
        {
            return Literal(Symbols.Dot).Continue(_ => Identifier());
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

        public static IParser<Token, string> Operator()
        {
            return Parsers.Take<Token>().Satisfies(token => token.Tag == Tokens.Operator).Fmap(token => ((PracticalCompiler.Token.Operator)token).Content);
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
            var declaration = Literal(Symbols.HasType).Continue(_ => Expression()).Fmap(_ => (TypeConstraint)new TypeConstraint.Type(_));

            var classification = Literal(Symbols.SubType).Continue(_ => Expression()).Fmap(_ => (TypeConstraint)new TypeConstraint.Class(_));

            var none = Parsers.Returns<Token, TypeConstraint>(new TypeConstraint.None());

            var generic = Identifier()
                .Continue(identifier => Parsers.Alternatives(declaration, classification, none)
                    .Continue(constraint => Parsers.Returns<Token, Term>(new Term.Generic(identifier, constraint))));

            return generic.Between(Brackets.Square);
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

                    switch (generic.Constraint.Tag)
                    {
                        case TypeConstraints.None:

                            type = new Option<Term>.Some(TypeChecking.DefaultGenericType);

                            break;
                        case TypeConstraints.Type:
                            var annotation = (TypeConstraint.Type) generic.Constraint;

                            type = new Option<Term>.Some(annotation.Content);

                            break;
                        case TypeConstraints.Class:

                            throw new ArgumentException("Lambda parameter cannot be bounded by a class.");
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    term = new Term.Variable(generic.Identifier);
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

                    return Parsers.Returns<Token, Declaration>(new Declaration(type, new Option<Term>.None(), variable.Content));
                }

                return Parsers.Fails<Token, Declaration>("Parameter should be a variable declaration.");
            });
        }

        public static IParser<char, Token> Token()
        {
            return Parsers.Peek<char>()
                .Continue<char, char, Token>(lead =>
                {
                    if (IsAlpha(lead))
                    {
                        return Word().Fmap(word => RecognizeKeywords(word) ?? new Token.Identifier(word));
                    }

                    if (Char.IsDigit(lead))
                    {
                        return Number().Fmap(number => (Token)new Token.Number(number));
                    }

                    if (IsSymbol(lead))
                    {
                        return Symbol().Fmap(symbol => RecognizeSymbols(symbol) ?? new Token.Operator(symbol));
                    }

                    if (lead == '"')
                    {
                        return String().Fmap(text => (Token)new Token.Text(text));
                    }

                    return Bracket<Brackets>(Bracketing).Fmap(bracket => (Token) new Token.Bracket(bracket));
                });
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
                case "<:": return new Token.Symbol(Symbols.SubType);

                case ":": return new Token.Symbol(Symbols.HasType);
                case "->": return new Token.Symbol(Symbols.Arrow);
                case "&": return new Token.Symbol(Symbols.Ampersand);
                case ",": return new Token.Symbol(Symbols.Comma);
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
            return Separated(parser, Parsers.Single(separator));
        }

        public static IParser<S, T[]> Separated<S, T>(this IParser<S, T> parser, IParser<S, Unit> separator)
        {
            return parser
                .Continue(first => parser.After(separator).Repeat()
                    .Continue(rest => Parsers.Returns<S, T[]>(ArrayOperations.Concatenate(new T[] { first }, rest))));
        }

        public static IParser<S, Association<O, T>> Separated<S, O, T>(this IParser<S, T> parser, IParser<S, O> separator)
        {
            return parser
                .Continue(first => separator.AndThen(parser).Repeat()
                    .Continue(rest => Parsers.Returns<S, Association<O, T>>(new Association<O, T>(
                        @operators: rest.Fmap(_ => _.Item1),
                        @operands: ArrayOperations.Concatenate(new T[] { first }, rest.Fmap(_ => _.Item2))))));
        }

        //
        //

        public static IParser<char, string> LineComment()
        {
            var start = Parsers.Sequence(Parsers.Single('/'), Parsers.Single('/')).Fmap(_ => Unit.Singleton);
            var online = Parsers.Take<char>().Satisfies(c => c != '\n');

            return start.Continue(_ => online.Repeat()).Fmap(text => new string(text));
        }

        public static IParser<char, Unit> BlockComment2()
        {
            var start = Parsers.Sequence(Parsers.Single('/'), Parsers.Single('*')).Fmap(_ => Unit.Singleton);

            return start.Continue(_ => BlockBody());
        }

        public static IParser<char, Unit> BlockBody()
        {
            var end = Parsers.Sequence(Parsers.Single('*'), Parsers.Single('/')).Fmap(_ => Unit.Singleton);

            return Parsers.Peeks<char>(2)
                .Continue<char, char[], Unit>(lead =>
                {
                    if (lead.Length == 2)
                    {
                        if (lead[0] == '*')
                        {
                            if (lead[1] == '/')
                            {
                                return end;
                            }
                        }

                        if (lead[0] == '/')
                        {
                            if (lead[1] == '*')
                            {
                                return BlockComment();
                            }
                        }
                    }

                    return Parsers.Take<char>().Continue(_ => BlockBody());
                });
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

            return alternatives;
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
            
            /*
            var altenatives = Parsers.Peeks<char>(2)
                .Continue<char, char[], Option<T>>(lead =>
                {
                    if (lead.Length == 0)
                    {
                        return Parsers.Fails<char, Option<T>>("End of stream.");
                    }

                    if (Char.IsWhiteSpace(lead[0]))
                    {
                        return Whitespace().Fmap(_ => (Option<T>) new Option<T>.None());
                    }

                    if (lead[0] == '/')
                    {
                        if (lead.Length != 1)
                        {
                            if (lead[1] == '/')
                            {
                                return LineComment().Fmap(_ => (Option<T>) new Option<T>.None());
                            }

                            if (lead[1] == '*')
                            {
                                return BlockComment().Fmap(_ => (Option<T>) new Option<T>.None());
                            }
                        }
                    }

                    return parser.Fmap(_ => (Option<T>) new Option<T>.Some(_));
                });
            */
                
            var segments = altenatives.Repeat().Fmap(ArrayOperations.Filter<T>);

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
            return Text(IsAlpha);
        }

        private static bool IsAlpha(char @char)
        {
            return Char.IsLetter(@char) || @char == '_';
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

            return bracket;
        }

        public static IParser<char, Boundaries> Bracket(IBracketing bracketing)
        {
            return Parsers.Take<char>().Continue<char, char, Boundaries>(
                generate: @char =>
                {
                    var boundary = RecognizeBrackets(bracketing, @char);

                    if (boundary == null)
                    {
                        return Parsers.Fails<char, Boundaries>("Character is not a bracket: " + @char);
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