using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace PracticalCompiler
{
    public sealed class EndOfFileExpected : Exception
    {
        
    }

    public static class Parsers
    {
        public static IParsed<S, B> Fmap<S, A, B>(this IParsed<S, A> parsed, Func<A, B> convert)
        {
            return new Parsed<S, B>(
                content: convert(parsed.Content),
                stream: parsed.Stream);
        }

        public static T ParseCompletely<S, T>(this IParser<S, T> parser, IStream<S> stream)
        {
            return parser.Before(Parsers.Terminated<S>()).Parse(stream).Wait().Throw().Content;
        }

        public static IParser<S, T> Delay<S, T>(Func<IParser<S, T>> parser)
        {
            return new Parser<S, T>(parseF: stream => parser().Parse(stream));
        }

        public static IParser<S, S> Peek<S>()
        {
            return Peeks<S>().Continue(
                generate: leading =>
                {
                    if (leading.Length == 0)
                    {
                        return Fails<S, S>("End of stream.");
                    }

                    return Returns<S, S>(leading[0]);
                });
        }
        
        public static IParser<S, S[]> Peeks<S>(int lookahead = 1)
        {
            return Peek<S, S[]>(lookahead: stream => stream.AsEnumerable().Take(lookahead).ToArray());
        }

        public static IParser<S, T> Peek<S, T>(Func<IStream<S>, T> lookahead)
        {
            return new Parser<S, T>(
                parseF: stream =>
                {
                    var result = lookahead(stream);
                    
                    IParsed<S, T> parsed = new Parsed<S, T>(result, stream);

                    return parsed.Succeed().Now();
                });
        }

        public static IParser<S, S> Take<S>()
        {
            return new Parser<S, S>(
                parseF: stream =>
                {
                    var step = stream.Unroll();

                    switch (step.Tag)
                    {
                        case Step.Empty:

                            return "End of stream.".Fail<IParsed<S, S>>().Now();
                        case Step.Node:
                            var node = (Step<S>.Node) step;

                            IParsed<S, S> result = new Parsed<S, S>(node.Head, node.Tail);

                            return result.Succeed().Now();
                        default:
                            throw new InvalidProgramException("Should never happen.");
                    }
                });
        } 

        public static IParser<S, Unit> Terminated<S>()
        {
            return new Parser<S, Unit>(
                parseF: stream =>
                {
                    var step = stream.Unroll();

                    switch (step.Tag)
                    {
                        case Step.Empty:

                            IParsed<S, Unit> result = new Parsed<S, Unit>(Unit.Singleton, stream);

                            return result.Succeed().Now();
                        case Step.Node:

                            return "Some stream content remaining.".Fail<IParsed<S, Unit>>().Now();
                        default:
                            throw new InvalidProgramException("Should never happen.");
                    }
                });
        }

        public static IParser<S, Unit> Single<S>(S token)
        {
            return Take<S>().Satisfies(_ => _.Equals(token)).Fmap(_ => Unit.Singleton);
        }

        public static IParser<S, T> Satisfies<S, T>(this IParser<S, T> parser, Func<T, bool> condition)
        {
            return parser.Continue<S, T, T>(result => condition(result) ? Returns<S, T>(result) : Fails<S, T>("Parsing result did not satisfy constraint."));
        }

        public static IParser<S, T> Returns<S, T>(T result)
        {
            return new Parser<S, T>(
                parseF: stream =>
                {
                    IParsed<S, T> parsed = new Parsed<S, T>(result, stream);

                    return parsed.Succeed().Now();
                });
        }

        public static IParser<S, T> Fails<S, T>(string error)
        {
            return new Parser<S, T>(
                parseF: stream =>
                {
                    return error.Fail<IParsed<S, T>>().Now();
                });
        }

        public static IParser<S, A> Before<S, A, B>(this IParser<S, A> parser, IParser<S, B> after)
        {
            return parser.Continue(result => after.Continue(_ => Returns<S, A>(result)));
        }

        public static IParser<S, B> After<S, A, B>(this IParser<S, B> parser, IParser<S, A> before)
        {
            return before.Continue(_ => parser);
        } 

        public static IParser<S, B> Continue<S, A, B>(this IParser<S, A> parser, Func<A, IParser<S, B>> generate)
        {
            return new Parser<S, B>(
                parseF: stream =>
                {
                    var response = parser.Parse(stream).Wait();

                    switch (response.Tag)
                    {
                        case Response.Failure:
                            var failure = (Response<IParsed<S, A>>.Failure) response;

                            return failure.Error.Fail<IParsed<S, B>>().Now();
                        case Response.Success:
                            var success = (Response<IParsed<S, A>>.Success) response;

                            var next = generate(success.Result.Content);
                            
                            return Events.Delay(() => next.Parse(success.Result.Stream));
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });
        }

        public static IParser<S, Tuple<A, B>> AndThen<S, A, B>(this IParser<S, A> first, IParser<S, B> second)
        {
            return first.Continue(x => second.Continue(y => Returns<S, Tuple<A, B>>(Tuple.Create(x, y))));
        } 

        public static IParser<S, T> OrElse<S, T>(this IParser<S, T> first, IParser<S, T> second)
        {
            return new Parser<S, T>(
                parseF: stream =>
                {
                    var response = first.Parse(stream).Wait();
                    
                    switch (response.Tag)
                    {
                        case Response.Failure:
                            var failure = (Response<IParsed<S, T>>.Failure) response;
                            
                            return Events.Delay(() => second.Parse(stream));
                        case Response.Success:
                            var success = (Response<IParsed<S, T>>.Success) response;

                            return response.Now();
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });
        }

        public static IParser<S, T> Alternatives<S, T>(params IParser<S, T>[] parsers)
        {
            return Alternatives(parsers, 0);
        }

        public static IParser<S, T> Alternatives<S, T>(IParser<S, T>[] parsers, uint index)
        {
            if (index < parsers.Length)
            {
                if (index + 1 == parsers.Length)
                {
                    return parsers[index];
                }

                var next = Alternatives(parsers, index + 1);

                return parsers[index].OrElse(next);
            }

            return Fails<S, T>("No alternatives were successful.");
        }

        public static IParser<S, T[]> Sequence<S, T>(params IParser<S, T>[] parsers)
        {
            return new Parser<S, T[]>(
                parseF: stream =>
                {
                    var array = new T[parsers.Length];

                    return Sequence<S, T>(parsers, array, 0).Parse(stream);
                });
        }

        private static IParser<S, T[]> Sequence<S, T>(IParser<S, T>[] parsers, T[] array, int index)
        {
            if (index < parsers.Length)
            {
                return parsers[index].Continue(element =>
                {
                    array[index] = element;

                    return Sequence(parsers, array, index + 1);
                });
            }

            return Returns<S, T[]>(array);
        }

        public static IParser<S, B> Fmap<S, A, B>(this IParser<S, A> parser, Func<A, B> convert)
        {
            return parser.Continue(result => Returns<S, B>(convert(result)));
        }

        public static IParser<S, Option<T>> Option<S, T>(this IParser<S, T> parser)
        {
            var alternatives = Alternatives<S, Option<T>>(
                parser.Fmap(result => (Option<T>) new Option<T>.Some(result)),
                Returns<S, Option<T>>(new Option<T>.None()));

            return alternatives;
        }

        public static IParser<S, T[]> Some<S, T>(this IParser<S, T> parser)
        {
            return parser.Repeat().Satisfies(result => result.Length != 0);
        }

        public static IParser<S, T[]> Repeat<S, T>(this IParser<S, T> parser)
        {
            return new Parser<S, T[]>(
                parseF: stream =>
                {
                    var results = new List<T>();

                    while (true)
                    {
                        var response = parser.Parse(stream).Wait();

                        if (response.Throws())
                        {
                            break;
                        }

                        foreach (var parsed in response.Option().Each())
                        {
                            results.Add(parsed.Content);

                            stream = parsed.Stream;
                        }
                    }

                    IParsed<S, T[]> result = new Parsed<S, T[]>(results.ToArray(), stream);

                    return result.Succeed().Now();
                });
        }

        public static IParser<S, T[]> Repeating<S, T>(this IParser<S, T> parser)
        {
            return Delay(parser: () =>
                {
                    var list = new List<T>();

                    return Repeating(parser, list);
                });
        }

        public static IParser<S, T[]> Repeating<S, T>(IParser<S, T> parser, IList<T> list)
        {
            var repeating = parser.Continue(element =>
            {
                list.Add(element);

                return Repeating<S, T>(parser, list);
            });

            var terminal = Delay(() => Returns<S, T[]>(list.ToArray()));

            return repeating.OrElse(terminal);
        }
    }
}