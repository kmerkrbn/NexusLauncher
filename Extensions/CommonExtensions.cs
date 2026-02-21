using System;

namespace MClauncher.Extensions
{
    public static class StringExtensions
    {
        public static bool IsNullOrEmptyOrWhitespace(this string? value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        public static string OrDefault(this string? value, string defaultValue = "")
        {
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }
    }

    public static class IntExtensions
    {
        public static int Clamp(this int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
