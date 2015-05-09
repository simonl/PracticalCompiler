using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using PracticalCompiler.Untyped;

namespace PracticalCompiler
{
    public static class Program
    {
        /*
            term r = Type | Func (r, r) | Variable var | Lambda (option r, var, r) | Apply (r, r) | Annotation (r, r)

            g : graph term

            top : index term g

         */

        public static void Main(string[] args)
        {
            //InteractiveMain(args);
            PerformanceMain(args);
            //StackMain(args);
        }

        private static void UntypedMain(string[] args)
        {
            var constant = new UntypedTerm.Lambda("x", new UntypedTerm.Lambda("y", new UntypedTerm.Variable("x")));
            var identity = new UntypedTerm.Lambda("x", new UntypedTerm.Variable("x"));
            var distribute = new UntypedTerm.Lambda("f", new UntypedTerm.Lambda("x", new UntypedTerm.Lambda("c", new UntypedTerm.Apply(new UntypedTerm.Apply(new UntypedTerm.Variable("f"), new UntypedTerm.Apply(new UntypedTerm.Variable("x"), new UntypedTerm.Variable("c"))), new UntypedTerm.Variable("c")))));
            var loop = new UntypedTerm.Lambda("x", new UntypedTerm.Apply(new UntypedTerm.Variable("x"), new UntypedTerm.Variable("x")));

            var stuffing = new UntypedTerm.Apply(new UntypedTerm.Apply(distribute, distribute), constant);
            var stuff = new UntypedTerm.Apply(new UntypedTerm.Apply(distribute, constant), identity);
            var broke = new UntypedTerm.Lambda("y", new UntypedTerm.Apply(constant, new UntypedTerm.Variable("y")));
            var bottom = new UntypedTerm.Apply(loop, loop);

            var term = broke;
            uint count;

            Console.WriteLine(UntypedLanguage.Print(term));
            Console.WriteLine(UntypedLanguage.Print(UntypedLanguage.Normalize(term, out count)));
            Console.WriteLine("Substituted " + count + " variables.");
        }

        public static void PerformanceMain(string[] args)
        {
            var main = File.ReadAllText("Main.fun");

            for (int i = 0; i < 500; i++)
            {
                //Console.WriteLine(i);
                Tokenize(main);
            }
        }

        public static void StackMain(string[] args)
        {
            for (int i = 0; i < 500; i++)
            {
                uint count = 10000;

                var stream = 'a'.Repeat(count).ToStream();
                var parser = Parsers.Take<char>().Repeating(count);

                uint jumps;
                var result = parser.Parse(stream).Wait(out jumps).Throw();

                var remaining = result.Stream.AsEnumerable().Any();
            }
        }

        public static T[] Repeat<T>(this T instance, uint count)
        {
            var array = new T[count];

            for (uint index = 0; index < count; index++)
            {
                array[index] = instance;
            }

            return array;
        }

        public static void InteractiveMain(string[] args)
        {
            var intType = BaseType.ShiftDown<TypedTerm>(new TypedTerm.Variable("int"));
            var stringType = BaseType.ShiftDown<TypedTerm>(new TypedTerm.Variable("string"));
            var operation = BaseType.ShiftDown<TypedTerm>(new TypedTerm.Type(new TypeStruct.Arrow(new TypedQuantifier(intType.Declared(), new TypedTerm.Type(new TypeStruct.Arrow(new TypedQuantifier(intType.Declared(), intType.Term)))))));

            var environment = (null as Environment<Classification<dynamic>>)
                .Push("string", BaseType.ShiftDown<dynamic>(null))
                .Push("int", BaseType.ShiftDown<dynamic>(null))
                .Push("plus", operation.ShiftDown<dynamic>(new Func<dynamic, dynamic>(x => new Func<dynamic, dynamic>(y => x + y))))
                .Push("times", operation.ShiftDown<dynamic>(new Func<dynamic, dynamic>(x => new Func<dynamic, dynamic>(y => x * y))));

            var prelude = new string[]
            {
                "let main = import \"Main.fun\""
            };

            foreach (var line in prelude)
            {
                Console.Write("> ");
                Console.WriteLine(line);
                Handle(ref environment, line);
            }

            while (true)
            {
                Console.Write("> ");
                var line = Console.ReadLine();

                line = line.Trim();

                if (line == "")
                {
                    continue;
                }

                if (line.Equals("exit"))
                {
                    return;
                }

                Handle(ref environment, line);
            }
        }

        private static void Handle(ref Environment<Classification<dynamic>> environment, string line)
        {
            var time = Stopwatch.StartNew();

            try
            {
                var result = Interpret(ref environment, line);

                Console.WriteLine(result.Term);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                time.Stop();

                Console.WriteLine("Chrono: " + time.Elapsed);
            }
        }

        private static Classification<dynamic> Interpret(ref Environment<Classification<dynamic>> environment, string line)
        {
            var tokens = ProgramParsing.Separated(ProgramParsing.Token()).ParseCompletely(line.ToStream());

            var command = ProgramParsing.CommandLine().ParseCompletely(tokens.ToStream());

            foreach (var defun in command.Definition.Each())
            {
                var term = defun;
                foreach (var type in command.Declaration.Each())
                {
                    term = new Term.Annotation(new Annotated(type, term));
                }

                var typed = Execute(environment, term);

                if (command.Identifier != null)
                {
                    if (environment.Maps(command.Identifier))
                    {
                        Console.WriteLine("Shadowing is not allowed!");
                    }
                    else
                    {
                        environment = environment.Push(command.Identifier, typed);
                    }
                }

                return typed;
            }

            throw new ArgumentException("Command line must include evaluable expression.");
        }

        private static Classification<dynamic> Execute(Environment<Classification<dynamic>> environment, string program)
        {
            var tokens = Tokenize(program);

            var term = Parse(tokens);

            return Execute(environment, term);
        }

        private static Term Parse(Token[] tokens)
        {
            return ProgramParsing.CompilationUnit().ParseCompletely(tokens.ToStream());
        }

        private static Token[] Tokenize(string program)
        {
            return ProgramParsing.Separated(ProgramParsing.Token()).ParseCompletely(program.ToStream());
        }

        private static Classification<dynamic> Execute(Environment<Classification<dynamic>> environment, Term term)
        {
            var substitution = environment.Fmap(expr => expr.Fmap<dynamic, TypedTerm>(_ => _ == null ? null : new TypedTerm.Constant(_)));

            var typed = TypeChecking.InferType(file => Import(environment, file), substitution, term);

            var result = Evaluate(environment.Fmap(expr => expr.Term), typed);

            return typed.Fmap(_ => result);
        }

        private static Classification<dynamic> Import(Environment<Classification<dynamic>> environment, string filename)
        {
            var program = File.ReadAllText(filename);

            return Execute(environment, program);
        }

        public static Classification<TypedTerm> BaseType
        {
            get { return Universe(0); }
        }

        public static Classification<TypedTerm> Universe(uint rank)
        {
            return new Classification<TypedTerm>(
                universe: new Universes(rank + 2),
                type: new TypedTerm.Universe(new Universes(rank + 1)),
                term: new TypedTerm.Universe(new Universes(rank)));
        } 
        
        public static Environment<Classification<TypedTerm>> Push(this Environment<Classification<TypedTerm>> environment, Classification<string> binding)
        {
            return environment.Push(binding.Term, binding.Fmap<string, TypedTerm>(_ => new TypedTerm.Variable(_)));
        }

        public static Environment<T> Push<T>(this Environment<T> environment, string identifier, T binding)
        {
            return new Environment<T>(identifier, binding, environment);
        }

        public static Environment<B> Fmap<A, B>(this Environment<A> environment, Func<A, B> convert)
        {
            if (environment == null)
            {
                return null;
            }

            return new Environment<B>(
                identifier: environment.Identifier,
                binding: convert(environment.Binding),
                next: environment.Next.Fmap<A, B>(convert));
        }

        public static IEnumerable<KeyValuePair<string, T>> Each<T>(this Environment<T> environment)
        {
            while (environment != null)
            {
                yield return new KeyValuePair<string, T>(environment.Identifier, environment.Binding);

                environment = environment.Next;
            }
        } 

        public static bool Maps<T>(this Environment<T> environment, string identifier)
        {
            return environment.Each().Any(binding => binding.Key == identifier);
        }

        public static T Lookup<T>(this Environment<T> environment, string identifier)
        {
            foreach (var binding in environment.Each())
            {
                if (binding.Key == identifier)
                {
                    return binding.Value;
                }
            }

            throw new KeyNotFoundException("Unknown identifier: " + identifier);
        }

        public static dynamic Evaluate(Environment<dynamic> environment, Classification<TypedTerm> expression)
        {
            if (expression.Universe.Rank != 0)
            {
                throw new InvalidOperationException("Cannot evaluate types.");
            }

            return Evaluate(environment, expression.Term);
        }

        public static dynamic Evaluate(Environment<dynamic> environment, TypedTerm term)
        {
            switch (term.Tag)
            {
                case TypedProductions.Constructor:
                {
                    var constructor = (TypedTerm.Constructor) term;

                    switch (constructor.Content.Type)
                    {
                        case TypeStructs.Arrow:
                            var lambda = (Constructors.Arrow) constructor.Content;

                            return new Func<dynamic, dynamic>(x => Evaluate(Push<dynamic>(environment, lambda.Content.Identifier, x), lambda.Content.Body));
                        case TypeStructs.Module:
                            var module = (Constructors.Module) constructor.Content;

                            return module.Content.Members.Fmap(member => Evaluate(environment, member));
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                case TypedProductions.Destructor:
                {
                    var destructor = (TypedTerm.Destructor) term;
                    var type = (TypedTerm.Type) destructor.Operator.Type;

                    var @operator = Evaluate(environment, destructor.Operator.Term);

                    switch (destructor.Content.Type)
                    {
                        case TypeStructs.Arrow:
                            var apply = (Destructors.Arrow) destructor.Content;
                            var arrow = (TypeStruct.Arrow) type.Content;

                            var operand = Evaluate(environment, apply.Content.Operand);

                            return @operator(operand);
                        case TypeStructs.Module:
                            var access = (Destructors.Module) destructor.Content;

                            return @operator[access.Content.Member];
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                case TypedProductions.Variable:
                {
                    var variable = (TypedTerm.Variable) term;

                    return environment.Lookup(variable.Identifier);
                }
                case TypedProductions.Constant:
                {
                    var constant = (TypedTerm.Constant)term;

                    return constant.Value;
                }
                case TypedProductions.Universe:
                case TypedProductions.Type:
                {
                    return null;
                }
                default:
                    throw new InvalidProgramException("Should never happen.");
            }
        }
    }
}
