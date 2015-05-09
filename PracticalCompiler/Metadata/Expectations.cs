using System;
using System.Collections.Generic;

namespace PracticalCompiler.Metadata
{
    public static class Expectations
    {
        public static IExpectation<Unit, Unit> Trivial()
        {
            return new Expectation<Unit, Unit>(
                unknownF: () => Unit.Singleton,
                definiteF: _ => Unit.Singleton,
                unionF: (left, right) => Unit.Singleton,
                extensionF: meta =>
                {
                    return new Set<Unit>(containsF: _ => true);
                });
        } 

        public static IExpectation<T, Expected<T>> Exact<T>(IEqualityComparer<T> comparer)
        {
            return new Expectation<T, Expected<T>>(
                unknownF: () =>
                {
                    return new Expected<T>.Unknown();
                },
                definiteF: element =>
                {
                    return new Expected<T>.Definite(element);
                },
                unionF: (left, right) =>
                {
                    foreach (var expectedL in left.Known())
                    {
                        foreach (var expectedR in right.Known())
                        {
                            if (comparer.Equals(expectedL, expectedR))
                            {
                                return left;
                            }
                        }
                    }

                    return new Expected<T>.Unknown();
                },
                extensionF: meta =>
                {
                    return new Set<T>(
                        containsF: actual =>
                        {
                            foreach (var expected in meta.Known())
                            {
                                return comparer.Equals(expected, actual);
                            }

                            return true;
                        });
                });
        }

        public static IExpectation<uint, IRange> Range()
        {
            return new Expectation<uint, IRange>(
                extensionF: Ranges.Extension,
                unknownF: Ranges.Unknown,
                definiteF: Ranges.Definite,
                unionF: Ranges.Union);
        } 

        private static IExpectation<Response<T>, IExpectedResponse<M>> Responding<T, M>(IExpectation<T, M> resultMeta)
        {
            var tagMeta = Exact<Response>(EqualityComparer<Response>.Default);
            var errorMeta = Trivial();

            return new Expectation<Response<T>, IExpectedResponse<M>>(
                unknownF: () => new ExpectedResponse<M>(
                    onTag: tagMeta.Unknown(),
                    onError: errorMeta.Unknown().Some(),
                    onResult: resultMeta.Unknown().Some()), 
                definiteF: response =>
                {
                    switch (response.Tag)
                    {
                        case Response.Failure:

                            return new ExpectedResponse<M>(
                                onTag: tagMeta.Definite(response.Tag),
                                onError: errorMeta.Definite(Unit.Singleton).Some(),
                                onResult: new Option<M>.None());
                        case Response.Success:
                            var success = (Response<T>.Success) response;
                            
                            return new ExpectedResponse<M>(
                                onTag: tagMeta.Definite(response.Tag),
                                onError: new Option<Unit>.None(),
                                onResult: resultMeta.Definite(success.Result).Some());
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                },
                unionF: (left, right) =>
                {
                    var onTag = tagMeta.Union(left.OnTag, right.OnTag);
                    
                    var onFailure = left.OnError.Merge(right.OnError, errorMeta.Union);
                    var onSuccess = left.OnResult.Merge(right.OnResult, resultMeta.Union);

                    return new ExpectedResponse<M>(
                        onTag: onTag,
                        onError: onFailure,
                        onResult: onSuccess);
                },
                extensionF: meta =>
                {
                    return new Set<Response<T>>(
                        containsF: response =>
                        {
                            if (tagMeta.Extension(meta.OnTag).Contains(response.Tag))
                            {
                                switch (response.Tag)
                                {
                                    case Response.Failure:

                                        return errorMeta.Extension(meta.OnError.Get()).Contains(Unit.Singleton);
                                    case Response.Success:
                                        var success = (Response<T>.Success) response;

                                        return resultMeta.Extension(meta.OnResult.Get()).Contains(success.Result);
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                            }

                            return false;
                        });
                });
        }  
    }
}