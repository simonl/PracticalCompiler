using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

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
            return parser.Before(Parsers.Terminated<S>()).Parse(stream).Throw().Content;
        }

        public static IParser<S, T> Delay<S, T>(Func<IParser<S, T>> parser)
        {
            return new Parser<S, T>(parseF: stream => parser().Parse(stream));
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

                            return new Response<IParsed<S, S>>.Failure(new EndOfStreamException());
                        case Step.Node:
                            var node = (Step<S>.Node) step;

                            return new Response<IParsed<S, S>>.Success(new Parsed<S, S>(node.Head, node.Tail));
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

                            return new Response<IParsed<S, Unit>>.Success(new Parsed<S, Unit>(Unit.Singleton, stream));
                        case Step.Node:

                            return new Response<IParsed<S, Unit>>.Failure(new EndOfFileExpected());
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
            return parser.Continue<S, T, T>(result => condition(result) ? Returns<S, T>(result) : Fails<S, T>(new ConstraintException()));
        }

        public static IParser<S, T> Returns<S, T>(T result)
        {
            return new Parser<S, T>(
                parseF: stream =>
                {
                    return new Response<IParsed<S, T>>.Success(new Parsed<S, T>(result, stream));
                });
        }

        public static IParser<S, T> Fails<S, T>(Exception error)
        {
            return new Parser<S, T>(
                parseF: stream =>
                {
                    return new Response<IParsed<S, T>>.Failure(error);
                });
        }

        public static IParser<S, B> Continue<S, A, B>(this IParser<S, A> parser, Func<A, IParser<S, B>> generate)
        {
            return new Parser<S, B>(
                parseF: stream =>
                {
                    var response = parser.Parse(stream);

                    return response.Continue(
                        generate: parsed =>
                        {
                            var next = generate(parsed.Content);

                            return next.Parse(parsed.Stream);
                        });
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

        public static IParser<S, IElement<T>> Alternatives<S, T>(params IParser<S, T>[] parsers)
        {
            return new Parser<S, IElement<T>>(
                parseF: stream =>
                {
                    var errors = new Exception[parsers.Length];

                    for (uint index = 0; index < parsers.Length; index++)
                    {
                        var parser = parsers[index];

                        var response = parser.Parse(stream);

                        switch (response.Tag)
                        {
                            case Response.Failure:
                                var failure = (Response<IParsed<S, T>>.Failure) response;

                                errors[index] = failure.Error;

                                break;
                            case Response.Success:
                                var success = (Response<IParsed<S, T>>.Success) response;

                                var element = success.Result.Fmap(result => new Element<T>(index, result));

                                return new Response<IParsed<S, IElement<T>>>.Success(element);
                            default:
                                throw new InvalidProgramException("Should never happen.");
                        }
                    }

                    return new Response<IParsed<S, IElement<T>>>.Failure(new AggregateException(errors));
                });
        }

        public static IParser<S, T[]> Sequence<S, T>(params IParser<S, T>[] parsers)
        {
            return new Parser<S, T[]>(
                parseF: stream =>
                {
                    var results = new T[parsers.Length];

                    for (uint index = 0; index < parsers.Length; index++)
                    {
                        var parser = parsers[index];

                        var response = parser.Parse(stream);

                        switch (response.Tag)
                        {
                            case Response.Failure:
                                var failure = (Response<IParsed<S, T>>.Failure) response;

                                return new Response<IParsed<S, T[]>>.Failure(new ElementException(index, failure.Error));
                            case Response.Success:
                                var success = (Response<IParsed<S, T>>.Success) response;

                                results[index] = success.Result.Content;

                                stream = success.Result.Stream;

                                break;
                            default:
                                throw new InvalidProgramException("Should never happen.");
                        }
                    }

                    return new Response<IParsed<S, T[]>>.Success(new Parsed<S, T[]>(results, stream));
                });
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

            return alternatives.Fmap(element => element.Content);
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
                        var response = parser.Parse(stream);

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

                    return new Response<IParsed<S, T[]>>.Success(new Parsed<S, T[]>(results.ToArray(), stream));
                });
        }
    }
}