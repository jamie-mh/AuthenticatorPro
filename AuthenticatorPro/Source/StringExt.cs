using System;

namespace AuthenticatorPro
{
    public static class StringExt
    {
        public static string Truncate(this string value, int maxLength)
        {
            if(String.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}