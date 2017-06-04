namespace System.Collections.Generic
{
    public static class DictionaryExtensions
    {
        public static U GetValueOrDefault<T, U>(this IDictionary<T, U> dict, T key)
        {
            return dict.TryGetValue(key, out U value) ? value : default(U);
        }

        public static U GetValueOrDefault<T, U>(this IDictionary<T, U> dict, T key, U @default)
        {
            return dict.TryGetValue(key, out U value) ? value : @default;
        }
    }
}
