using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

public static class StringExtensions
{
    public static string Join(this IEnumerable<string> values, string separator)
    {
        return string.Join(separator, values);
    }

    public static string RemoveSign(this string text)
    {
        return Regex.Replace(text, @"\W+", "");
    }

    public static string Coalesce(this string value, string fallback)
    {
        return string.IsNullOrEmpty(value) ? fallback : value;
    }

    public static bool IsEmailAddress(this string value)
    {
        return Regex.IsMatch(value, @"^([0-9a-zA-Z]([-\.\w]*[0-9a-zA-Z])*@([0-9a-zA-Z][-\w]*[0-9a-zA-Z]\.)+[a-zA-Z]{2,9})$");
    }

}
