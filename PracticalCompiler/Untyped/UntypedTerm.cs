using System;

namespace PracticalCompiler.Untyped
{
    public enum UntypedTerms
    {
        Variable,
        Lambda,
        Apply,
    }

    public abstract class UntypedTerm
    {
        public readonly UntypedTerms Tag;

        private UntypedTerm(UntypedTerms tag)
        {
            Tag = tag;
        }

        public sealed class Variable : UntypedTerm
        {
            public readonly string Identifier;

            public Variable(string identifier)
                : base(UntypedTerms.Variable)
            {
                Identifier = identifier;
            }
        }

        public sealed class Lambda : UntypedTerm
        {
            public readonly string Parameter;
            public readonly UntypedTerm Body;

            public Lambda(string parameter, UntypedTerm body)
                : base(UntypedTerms.Lambda)
            {
                Parameter = parameter;
                Body = body;
            }
        }

        public sealed class Apply : UntypedTerm
        {
            public readonly UntypedTerm Operator;
            public readonly UntypedTerm Operand;

            public Apply(UntypedTerm @operator, UntypedTerm operand)
                : base(UntypedTerms.Apply)
            {
                Operator = @operator;
                Operand = operand;
            }
        }
    }

    public sealed class Reduction<T>
    {
        public readonly Environment<T> Environment;
        public readonly T Term;

        public Reduction(Environment<T> environment, T term)
        {
            Environment = environment;
            Term = term;
        }
    }

    public static class UntypedLanguage
    {
        public static string Print(UntypedTerm term, bool complex = true)
        {
            switch (term.Tag)
            {
                case UntypedTerms.Variable:
                {
                    var variable = (UntypedTerm.Variable) term;

                    return variable.Identifier;
                }
                case UntypedTerms.Lambda:
                {
                    var lambda = (UntypedTerm.Lambda) term;

                    var body = Print(lambda.Body, complex: false);

                    return string.Format("(lambda {0} {1})", lambda.Parameter, body);
                }
                case UntypedTerms.Apply:
                {
                    var apply = (UntypedTerm.Apply) term;

                    var @operator = Print(apply.Operator, complex: true);
                    var operand = Print(apply.Operand, complex: false);

                    return string.Format(complex ? "{0} {1}" : "({0} {1})", @operator, operand);
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static UntypedTerm Normalize(UntypedTerm term, out uint count)
        {
            count = 0;
            return Normalize(term, null, ref count);
        }

        public static UntypedTerm Normalize(UntypedTerm term, Environment<UntypedTerm> environment, ref uint count)
        {
            switch (term.Tag)
            {
                case UntypedTerms.Variable:
                {
                    var variable = (UntypedTerm.Variable) term;

                    return environment.Lookup(variable.Identifier);
                }
                case UntypedTerms.Lambda:
                {
                    var lambda = (UntypedTerm.Lambda) term;

                    var substitution = environment.Maps(lambda.Parameter) ? ("_" + count++) : lambda.Parameter;

                    environment = environment.Push(lambda.Parameter, new UntypedTerm.Variable(substitution));

                    var body = Normalize(lambda.Body, environment, ref count);

                    return new UntypedTerm.Lambda(substitution, body);
                }
                case UntypedTerms.Apply:
                {
                    var apply = (UntypedTerm.Apply) term;

                    var @operator = Normalize(apply.Operator, environment, ref count);
                    var operand = Normalize(apply.Operand, environment, ref count);

                    switch (@operator.Tag)
                    {
                        case UntypedTerms.Lambda:
                            var lambda = (UntypedTerm.Lambda) @operator;

                            environment = environment.Push(lambda.Parameter, operand);

                            return Normalize(lambda.Body, environment, ref count);
                        case UntypedTerms.Variable:
                        case UntypedTerms.Apply:

                            return new UntypedTerm.Apply(@operator, operand);
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}