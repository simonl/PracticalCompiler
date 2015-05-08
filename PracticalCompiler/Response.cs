using System;

namespace PracticalCompiler
{
    public enum Response
    {
        Failure,
        Success,
    }

    public abstract class Response<T>
    {
        public readonly Response Tag;

        private Response(Response tag)
        {
            Tag = tag;
        }

        public sealed class Failure : Response<T>
        {
            public readonly Exception Error;

            public Failure(Exception error)
                : base(Response.Failure)
            {
                Error = error;
            }
        }

        public sealed class Success : Response<T>
        {
            public readonly T Result;

            public Success(T result)
                : base(Response.Success)
            {
                Result = result;
            }
        }
    }

    public static class Responses
    {
        public static Response<T> Succeed<T>(this T instance)
        {
            return new Response<T>.Success(instance);
        }

        public static bool Throws<T>(this Response<T> response)
        {
            switch (response.Tag)
            {
                case Response.Failure:

                    return true;
                case Response.Success:

                    return false;
                default:
                    throw new InvalidProgramException("Should never happen.");
            }
        }

        public static T Throw<T>(this Response<T> response)
        {
            switch (response.Tag)
            {
                case Response.Failure:
                    var failure = (Response<T>.Failure)response;

                    throw failure.Error;
                case Response.Success:
                    var success = (Response<T>.Success)response;

                    return success.Result;
                default:
                    throw new InvalidProgramException("Should never happen.");
            }
        }

        public static Option<T> Option<T>(this Response<T> response)
        {
            switch (response.Tag)
            {
                case Response.Failure:

                    return new Option<T>.None();
                case Response.Success:
                    var success = (Response<T>.Success) response;

                    return new Option<T>.Some(success.Result);
                default:
                    throw new InvalidProgramException("Should never happen.");
            }
        }

        public static Response<B> Continue<A, B>(this Response<A> response, Func<A, Response<B>> generate)
        {
            switch (response.Tag)
            {
                case Response.Failure:
                    var failure = (Response<A>.Failure) response;

                    return new Response<B>.Failure(failure.Error);
                case Response.Success:
                    var success = (Response<A>.Success) response;

                    return generate(success.Result);
                default:
                    throw new InvalidProgramException("Should never happen.");
            }
        }

        public static Response<B> Fmap<A, B>(this Response<A> response, Func<A, B> convert)
        {
            return response.Continue(generate:  result => new Response<B>.Success(convert(result)));
        }
    }
}