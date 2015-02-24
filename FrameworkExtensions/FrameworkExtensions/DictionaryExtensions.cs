namespace System.Collections.Generic
{
    public static class DictionaryExtensions
    {
        public static U GetValueOrDefault<T, U>(this IDictionary<T, U> dict, T key)
        {
            U value;
            return dict.TryGetValue(key, out value) ? value : default(U);
        }

        public static U GetValueOrDefault<T, U>(this IDictionary<T, U> dict, T key, U @default)
        {
            U value;
            return dict.TryGetValue(key, out value) ? value : @default;
        }
    }
}
