using System;

namespace PracticalCompiler
{
    public interface IParser<S, T>
    {
        IEventual<Response<IParsed<S, T>>> Parse(IStream<S> stream);
    }

    public sealed class Parser<S, T> : IParser<S, T>
    {
        private readonly Func<IStream<S>, IEventual<Response<IParsed<S, T>>>> ParseF;

        public Parser(Func<IStream<S>, IEventual<Response<IParsed<S, T>>>> parseF)
        {
            ParseF = parseF;
        }

        public IEventual<Response<IParsed<S, T>>> Parse(IStream<S> stream)
        {
            return this.ParseF(stream);
        }
    }
    
    public interface IParsed<S, out T>
    {
        T Content { get; }
        IStream<S> Stream { get; }
    }

    public sealed class Parsed<S, T> : IParsed<S, T>
    {
        public T Content { get; private set; }
        public IStream<S> Stream { get; private set; }

        public Parsed(T content, IStream<S> stream)
        {
            Content = content;
            Stream = stream;
        }
    }
}