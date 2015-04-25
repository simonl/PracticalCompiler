using System;
using System.Collections.Generic;
using System.Linq;

namespace PracticalCompiler
{
    public static class ArrayOperations
    {
        public static IDictionary<K, V> Build<K, V>(IEnumerable<KeyValuePair<K, V>> entries)
        {
            var map = new Dictionary<K, V>();

            foreach (var entry in entries)
            {
                map.Add(entry.Key, entry.Value);
            }

            return map;
        } 

        public static IDictionary<K, B> Fmap<K, A, B>(this IDictionary<K, A> map, Func<A, B> convert)
        {
            return Build(map.Select(entry => new KeyValuePair<K, B>(entry.Key, convert(entry.Value))));
        }

        public static B[] Fmap<A, B>(this A[] array, Func<A, B> convert)
        {
            var result = new B[array.Length];

            foreach (var index in CountUp(array.Length))
            {
                result[index] = convert(array[index]);
            }

            return result;
        }

        public static T[] Concatenate<T>(T[] first, params T[] second)
        {
            T[] array = new T[first.Length + second.Length];

            uint index = 0;

            foreach (var element in first)
            {
                array[index++] = element;
            }

            foreach (var element in second)
            {
                array[index++] = element;
            }

            return array;
        }

        public static T ReduceRight<T>(this T[] array, Func<T, T, T> merge)
        {
            var indices = CountDown(array.Length).ToArray();

            T result = array[indices[0]];
            foreach (var index in indices.Skip(1))
            {
                result = merge(array[index], result);
            }

            return result;
        }

        public static R ReduceRight<T, R>(this T[] array, R initial, Func<T, R, R> merge)
        {
            var result = initial;
            foreach (var index in CountDown(array.Length))
            {
                result = merge(array[index], result);
            }

            return result;
        }

        public static T ReduceLeft<T>(this T[] array, Func<T, T, T> merge)
        {
            var indices = CountUp(array.Length).ToArray();

            T result = array[indices[0]];
            foreach (var index in indices.Skip(1))
            {
                result = merge(result, array[index]);
            }

            return result;
        }

        public static R ReduceLeft<T, R>(this T[] array, R initial, Func<R, T, R> merge)
        {
            var result = initial;
            foreach (var index in CountUp(array.Length))
            {
                initial = merge(result, array[index]);
            }

            return result;
        }

        public static IEnumerable<int> CountUp(int max)
        {
            for (int index = 0; index < max; index++)
            {
                yield return index;
            }
        }

        public static IEnumerable<int> CountDown(int max)
        {
            int index = max;
            while (0 < index)
            {
                index--;

                yield return index;
            }
        }

        public static T[] Filter<T>(this Option<T>[] options)
        {
            var result = new List<T>(options.Length);

            foreach (var option in options)
            {
                foreach (var element in option.Each())
                {
                    result.Add(element);
                }
            }

            return result.ToArray();
        }
    }
}