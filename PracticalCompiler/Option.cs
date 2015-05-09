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
        public static T Get<T>(this Option<T> option)
        {
            foreach (var element in option.Each())
            {
                return element;
            }

            throw new AccessViolationException();
        }

        public static Option<T> Some<T>(this T element)
        {
            return new Option<T>.Some(element);
        } 

        public static T Or<T>(this Option<T> option, T @default)
        {
            foreach (var element in option.Each())
            {
                return element;
            }

            return @default;
        }

        public static Option<T> Merge<T>(this Option<T> first, Option<T> second, Func<T, T, T> merge)
        {
            foreach (var left in first.Each())
            {
                foreach (var right in second.Each())
                {
                    return merge(left, right).Some();
                }

                return first;
            }

            return second;
        } 

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