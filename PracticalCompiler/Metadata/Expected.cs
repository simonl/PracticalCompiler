using System;
using System.Collections.Generic;

namespace PracticalCompiler.Metadata
{
    public enum Expected
    {
        Unkown,
        Definite,
    }

    public abstract class Expected<T>
    {
        public readonly Expected Tag;

        private Expected(Expected tag)
        {
            Tag = tag;
        }

        public sealed class Unknown : Expected<T>
        {
            public Unknown() : base(Expected.Unkown)
            {
                
            }
        }

        public sealed class Definite : Expected<T>
        {
            public readonly T Content;

            public Definite(T content)
                : base(Expected.Definite)
            {
                Content = content;
            }
        }
    }

    public static class Knowledge
    {
        public static IEnumerable<T> Known<T>(this Expected<T> expected)
        {
            switch (expected.Tag)
            {
                case Expected.Unkown:

                    yield break;
                case Expected.Definite:
                    var definite = (Expected<T>.Definite) expected;

                    yield return definite.Content;
                    yield break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        } 
    }
}