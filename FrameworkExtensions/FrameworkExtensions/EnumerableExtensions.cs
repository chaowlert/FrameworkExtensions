using System.Collections.Generic;

namespace System.Linq
{
    public static class EnumerableExtensions
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }

        public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> func, IComparer<TKey> comparer = null)
        {
            if (comparer == null)
                comparer = Comparer<TKey>.Default;

            var e = source.GetEnumerator();
            if (!e.MoveNext())
                throw new InvalidOperationException("No element");

            var max = func(e.Current);
            var maxItem = e.Current;

            while (e.MoveNext())
            {
                var current = func(e.Current);
                if (comparer.Compare(current, max) <= 0)
                    continue;
                maxItem = e.Current;
                max = current;
            }
            return maxItem;
        }

        public static TSource MaxByOrDefault<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> func, IComparer<TKey> comparer = null)
        {
            if (comparer == null)
                comparer = Comparer<TKey>.Default;

            var e = source.GetEnumerator();
            if (!e.MoveNext())
                return default(TSource);

            var max = func(e.Current);
            var maxItem = e.Current;

            while (e.MoveNext())
            {
                var current = func(e.Current);
                if (comparer.Compare(current, max) <= 0)
                    continue;
                maxItem = e.Current;
                max = current;
            }
            return maxItem;
        }

        public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> func, IComparer<TKey> comparer = null)
        {
            if (comparer == null)
                comparer = Comparer<TKey>.Default;

            var e = source.GetEnumerator();
            if (!e.MoveNext())
                throw new InvalidOperationException("No element");

            var min = func(e.Current);
            var minItem = e.Current;

            while (e.MoveNext())
            {
                var current = func(e.Current);
                if (comparer.Compare(current, min) >= 0)
                    continue;
                minItem = e.Current;
                min = current;
            }
            return minItem;
        }

        public static TSource MinByOrDefault<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> func, IComparer<TKey> comparer = null)
        {
            if (comparer == null)
                comparer = Comparer<TKey>.Default;

            var e = source.GetEnumerator();
            if (!e.MoveNext())
                return default(TSource);

            var min = func(e.Current);
            var minItem = e.Current;

            while (e.MoveNext())
            {
                var current = func(e.Current);
                if (comparer.Compare(current, min) >= 0)
                    continue;
                minItem = e.Current;
                min = current;
            }
            return minItem;
        }

        public static T AggregateBalance<T>(this IEnumerable<T> source, Func<T, T, T> func)
        {
            while (true)
            {
                var list = new List<T>();
                var previous = default(T);
                var pair = false;
                foreach (var item in source)
                {
                    if (pair)
                        list.Add(func(previous, item));
                    else
                        previous = item;
                    pair = !pair;
                }
                if (pair)
                    list.Add(previous);

                if (list.Count == 0)
                    throw new InvalidOperationException("No element");
                if (list.Count == 1)
                    return list[0];
                source = list;
            }
        }
    }
}
