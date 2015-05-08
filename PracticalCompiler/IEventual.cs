using System;

namespace PracticalCompiler
{
    public interface IEventual<T>
    {
        Event<T> Unroll();
    }

    public sealed class Eventual<T> : IEventual<T>
    {
        private readonly Func<Event<T>> UnrollF;

        public Eventual(Func<Event<T>> unrollF)
        {
            UnrollF = unrollF;
        }

        public Event<T> Unroll()
        {
            return this.UnrollF();
        }
    }

    public enum Event
    {
        Now,
        Later,
    }

    public abstract class Event<T>
    {
        public readonly Event Tag;

        private Event(Event tag)
        {
            Tag = tag;
        }

        public sealed class Now : Event<T>
        {
            public readonly T Content;

            public Now(T content)
                : base(Event.Now)
            {
                Content = content;
            }
        }

        public sealed class Later : Event<T>
        {
            public readonly IEventual<T> Content;

            public Later(IEventual<T> content)
                : base(Event.Later)
            {
                Content = content;
            }
        }
    }

    public static class Events
    {
        public static IEventual<T> Now<T>(this T result)
        {
            return new Eventual<T>(unrollF: () => new Event<T>.Now(result));
        }

        public static IEventual<T> Delay<T>(Func<IEventual<T>> generate)
        {
            return new Eventual<T>(unrollF: () => new Event<T>.Later(generate()));
        } 

        public static T Wait<T>(this IEventual<T> eventual)
        {
            while (true)
            {
                var @event = eventual.Unroll();

                switch (@event.Tag)
                {
                    case Event.Now:
                        var now = (Event<T>.Now) @event;

                        return now.Content;
                    case Event.Later:
                        var later = (Event<T>.Later) @event;

                        eventual = later.Content;

                        break;
                    default:
                        throw new InvalidProgramException("Should never happen.");
                }
            }
        }

        public static IEventual<B> Fmap<A, B>(this IEventual<A> eventual, Func<A, B> convert)
        {
            return new Eventual<B>(
                unrollF: () =>
                {
                    var @event = eventual.Unroll();

                    switch (@event.Tag)
                    {
                        case Event.Now:
                            var now = (Event<A>.Now) @event;

                            return new Event<B>.Now(convert(now.Content));
                        case Event.Later:
                            var later = (Event<A>.Later) @event;

                            return new Event<B>.Later(later.Content.Fmap<A, B>(convert));
                        default:
                            throw new InvalidProgramException("Should never happen.");
                    }
                });
        }

        public static IEventual<T> Join<T>(this IEventual<IEventual<T>> eventual)
        {
            return new Eventual<T>(
                unrollF: () =>
                {
                    var @event = eventual.Unroll();

                    switch (@event.Tag)
                    {
                        case Event.Now:
                            var now = (Event<IEventual<T>>.Now) @event;

                            return new Event<T>.Later(now.Content);
                        case Event.Later:
                            var later = (Event<IEventual<T>>.Later) @event;

                            return new Event<T>.Later(later.Content.Join());
                        default:
                            throw new InvalidProgramException("Should never happen.");
                    }
                });
        }

        public static IEventual<B> Continue<A, B>(this IEventual<A> eventual, Func<A, IEventual<B>> generate)
        {
            return eventual.Fmap(generate).Join();
        } 
    }
}