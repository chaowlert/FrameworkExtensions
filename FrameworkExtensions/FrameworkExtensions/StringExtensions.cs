using System.Collections.Generic;
using System.Text;

public static class StringExtensions
{
    public static string Join(this IEnumerable<string> values, string separator)
    {
        return string.Join(separator, values);
    }

    public static string Utf8Substring(this string text, int byteLimit)
    {
        int byteCount = 0;
        char[] buffer = new char[1];
        for (int i = 0; i < text.Length; i++)
        {
            buffer[0] = text[i];
            byteCount += Encoding.UTF8.GetByteCount(buffer);
            if (byteCount > byteLimit)
            {
                // Couldn't add this character. Return its index
                return text.Substring(0, i);
            }
        }
        return text;
    }
}
