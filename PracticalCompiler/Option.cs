using System;
using System.Collections;
using System.Collections.Generic;

namespace PracticalCompiler
{
    public enum Option
    {
        None,
        Some,
    }

    public abstract class Option<T>
    {
        public readonly Option Tag;

        private Option(Option tag)
        {
            Tag = tag;
        }

        public sealed class None : Option<T>
        {
            public None() 
                : base(Option.None)
            {
                
            }
        }

        public sealed class Some : Option<T>
        {
            public readonly T Content;

            public Some(T content)
                : base(Option.Some)
            {
                Content = content;
            }
        }
    }

    public static class Options
    {
        public static IEnumerable<T> Each<T>(this Option<T> option)
        {
            switch (option.Tag)
            {
                case Option.None:

                    yield break;
                case Option.Some:
                    var some = (Option<T>.Some) option;

                    yield return some.Content;
                    yield break;
                default:
                    throw new InvalidProgramException("Should never happen.");
            }
        }
    }
}