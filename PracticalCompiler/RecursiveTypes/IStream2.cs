using System;

namespace PracticalCompiler.RecursiveTypes
{
    /*
    
        unfold k : (f:+k -> k) -> (r:k) -> (r => f r) -> (r => fix f)
        unfold f r step = recur
            where recur = roll . fmap recur . step
            
        fold k : (f:+k -> k) -> (r:k) -> (f r => r) -> (fix f => r)
        fold f r step = recur
            where recur = step . fmap recur . unroll

     * 
     * 
     * 
     
        unfold : (f:+N -> N) -> (r:P, !(r -> f ?r), r) -> fix f
        unfold f (r, step, x) = recur x
            where   recur : r -> fix f
                    recur x = fmap f (?r, fix f) (\tr. xr <- tr; force recur xr) { force step x }

        fold : (f:+P -> P) -> fix f -> ((r:N) -> !(f !r -> r) -> r)
        fold f xs r step = recur xs
            where   recur : fix f -> r
                    recur xs = ys <- fmap f (fix f, !r) (\tr. return { force recur tr }); force step ys

    */

    public interface IThunk<out T>
    {
        T Force();
    }

    public sealed class Thunk<T> : IThunk<T>
    {
        private readonly Func<T> ForceF;

        public Thunk(Func<T> forceF)
        {
            ForceF = forceF;
        }

        public T Force()
        {
            return this.ForceF();
        }
    }

    public static class ThunkUtils
    {
        public static IThunk<T> Delay<S, T>(this Func<S, T> func, S arg)
        {
            return new Thunk<T>(() => func(arg));
        }

        public static IThunk<B> Fmap<A, B>(this IThunk<A> thunk, Func<A, B> convert)
        {
            return new Thunk<B>(() => convert(thunk.Force()));
        } 
    }

    public interface IStream<T>
    {
        IStreamF<IStream<T>, T> Unroll();
    }

    public sealed class Stream<T> : IStream<T>
    {
        private readonly Func<IStreamF<IStream<T>, T>> UnrollF;

        public Stream(Func<IStreamF<IStream<T>, T>> unrollF)
        {
            UnrollF = unrollF;
        }

        public IStreamF<IStream<T>, T> Unroll()
        {
            return this.UnrollF();
        }
    }

    public interface IStreamF<out R, out T>
    {
        T Head();
        R Tail();
    }

    public sealed class StreamF<R, T> : IStreamF<R, T>
    {
        private readonly Func<T> HeadF;
        private readonly Func<R> TailF;  
        
        public StreamF(Func<T> headF, Func<R> tailF)
        {
            HeadF = headF;
            TailF = tailF;
        }

        public T Head()
        {
            return this.HeadF();
        }

        public R Tail()
        {
            return this.TailF();
        }
    }

    public static class StreamUtils
    {
        public static Func<IStream<T>, R> Fold<R, T>(Func<IStreamF<IThunk<R>, T>, R> step)
        {
            return number => step(number.Unroll().Fmap(Fold<R, T>(step).Delay));
        } 

        public static Func<R, IStream<T>> Unfold<R, T>(Func<R, IStreamF<R, T>> step)
        {
            return seed => new Stream<T>(() => step(seed).Fmap(Unfold<R, T>(step)));
        }

        public static IStreamF<B, T> Fmap<A, B, T>(this IStreamF<A, T> node, Func<A, B> convert)
        {
            return new StreamF<B, T>(
                headF: () => node.Head(),
                tailF: () => convert(node.Tail()));
        }

        public static IStream<B> Fmap<A, B>(this IStream<A> stream, Func<A, B> convert)
        {
            return new Stream<B>(
                unrollF: () =>
                {
                    return new StreamF<IStream<B>, B>(
                        headF: () => convert(stream.Unroll().Head()),
                        tailF: () => stream.Unroll().Tail().Fmap<A, B>(convert));
                });
        } 
    }

    public interface IObserver<in T>
    {
        IObserverF<IObserver<T>, T> Unroll();
    }

    public sealed class Observer<T> : IObserver<T>
    {
        private readonly Func<IObserverF<IObserver<T>, T>> UnrollF;

        public Observer(Func<IObserverF<IObserver<T>, T>> unrollF)
        {
            UnrollF = unrollF;
        }

        public IObserverF<IObserver<T>, T> Unroll()
        {
            return this.UnrollF();
        }
    }

    public interface IObserverF<out R, in T>
    {
        void Stop();
        R Send(T message);
    }

    public sealed class ObserverF<R, T> : IObserverF<R, T>
    {
        private readonly Action StopF;
        private readonly Func<T, R> SendF;

        public ObserverF(Action stopF, Func<T, R> sendF)
        {
            StopF = stopF;
            SendF = sendF;
        }

        public void Stop()
        {
            this.StopF();
        }

        public R Send(T message)
        {
            return this.SendF(message);
        }
    }

    public static class ObserverUtils
    {
        public static Func<IObserver<T>, R> Fold<R, T>(Func<IObserverF<IThunk<R>, T>, R> step)
        {
            return number => step(number.Unroll().Fmap(Fold<R, T>(step).Delay));
        } 

        public static Func<R, IObserver<T>> Unfold<R, T>(Func<R, IObserverF<R, T>> step)
        {
            return seed => new Observer<T>(() => step(seed).Fmap(Unfold<R, T>(step)));
        }

        public static IObserverF<B, T> Fmap<A, B, T>(this IObserverF<A, T> node, Func<A, B> convert)
        {
            return new ObserverF<B, T>(
                stopF: () => node.Stop(),
                sendF: message => convert(node.Send(message)));
        }

        public static IObserver<B> Fmap<A, B>(this IObserver<A> observer, Func<B, A> convert)
        {
            return new Observer<B>(
                unrollF: () =>
                {
                    return new ObserverF<IObserver<B>, B>(
                        stopF: () => observer.Unroll().Stop(),
                        sendF: message => observer.Unroll().Send(convert(message)).Fmap<A, B>(convert));
                });
        } 
    }

    public interface INumber
    {
        INumberF<INumber> Unroll();
    }

    public sealed class Number : INumber
    {
        private readonly Func<INumberF<INumber>> UnrollF;

        public Number(Func<INumberF<INumber>> unrollF)
        {
            UnrollF = unrollF;
        }

        public INumberF<INumber> Unroll()
        {
            return this.UnrollF();
        }
    }
    
    public interface INumberBuilder<out R, in N>
    {
        R Zero();
        R Increment(N number);
    }

    public sealed class NumberBuilder<R, N> : INumberBuilder<R, N>
    {
        private readonly Func<R> ZeroF;
        private readonly Func<N, R> IncrementF; 

        public NumberBuilder(Func<R> zeroF, Func<N, R> incrementF)
        {
            ZeroF = zeroF;
            IncrementF = incrementF;
        }

        public R Zero()
        {
            return this.ZeroF();
        }

        public R Increment(N number)
        {
            return this.IncrementF(number);
        }
    }

    public interface INumberF<out N>
    {
        R Extract<R>(INumberBuilder<R, N> builder);
    }

    public sealed class ZeroF<N> : INumberF<N>
    {
        public R Extract<R>(INumberBuilder<R, N> builder)
        {
            return builder.Zero();
        }
    }

    public sealed class IncrementF<N> : INumberF<N>
    {
        private readonly N Number;

        public IncrementF(N number)
        {
            Number = number;
        }

        public R Extract<R>(INumberBuilder<R, N> builder)
        {
            return builder.Increment(this.Number);
        }
    }
    
    public static class NaturalUtils
    {
        public static R Fold<R>(this INumber number, Func<INumberF<IThunk<R>>, R> step)
        {
            return Fold(step)(number);
        }

        public static Func<INumber, R> Fold<R>(Func<INumberF<IThunk<R>>, R> step)
        {
            return number => step(number.Unroll().Fmap(Fold<R>(step).Delay));
        } 

        public static Func<R, INumber> Unfold<R>(Func<R, INumberF<R>> step)
        {
            return seed => new Number(() => step(seed).Fmap(Unfold<R>(step)));
        }

        public static IObserver<T> Roll<T>(this IObserverF<IObserver<T>, T> node)
        {
            return new Observer<T>(() => node);
        }

        public static INumberF<B> Fmap<A, B>(this INumberF<A> node, Func<A, B> convert)
        {
            return node.Extract<INumberF<B>>(
                builder: new NumberBuilder<INumberF<B>, A>(
                    zeroF: () => new ZeroF<B>(),
                    incrementF: number => new IncrementF<B>(convert(number))));
        }

        public static R Reduce<R>(this INumber number, R seed, Func<R, R> step)
        {
            return Fold<R>(node => node.Extract<R>(new NumberBuilder<R, IThunk<R>>(() => seed, _ => step(_.Force()))))(number);
        }
    }
}