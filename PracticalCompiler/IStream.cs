using System;
using System.IO;

namespace PracticalCompiler
{
    public interface IStream<T>
    {
        Step<T> Unroll();
    }

    public sealed class Stream<T> : IStream<T>
    {
        private readonly Func<Step<T>> UnrollF;

        public Stream(Func<Step<T>> unrollF)
        {
            UnrollF = unrollF;
        }

        public Step<T> Unroll()
        {
            return this.UnrollF();
        }
    }

    public enum Step
    {
        Empty,
        Node,
    }

    public abstract class Step<T>
    {
        public readonly Step Tag;

        private Step(Step tag)
        {
            Tag = tag;
        }

        public sealed class Empty : Step<T>
        {
            public Empty()
                : base(Step.Empty)
            {
                
            }
        }

        public sealed class Node : Step<T>
        {
            public readonly T Head;
            public readonly IStream<T> Tail; 

            public Node(T head, IStream<T> tail)
                : base(Step.Node)
            {
                Head = head;
                Tail = tail;
            }
        }
    }

    public static class Streams
    {
        public static IStream<T> ToStream<T>(this T[] array)
        {
            return ArrayStream<T>(array, 0);
        }

        public static IStream<char> ToStream(this string text)
        {
            return text.ToCharArray().ToStream();
        }

        private static IStream<T> ArrayStream<T>(T[] array, uint index)
        {
            return new Stream<T>(
                unrollF: () =>
                {
                    if (index < array.Length)
                    {
                        var head = array[index];

                        var tail = ArrayStream<T>(array, index + 1);

                        return new Step<T>.Node(head, tail);
                    }

                    return new Step<T>.Empty();
                });
        } 
    }
}