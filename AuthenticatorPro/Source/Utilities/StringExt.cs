using System;
using System.Text.RegularExpressions;

namespace AuthenticatorPro.Utilities
{
    public static class StringExt
    {
        public static string Truncate(this string value, int maxLength)
        {
            if(String.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        public static string GetSlug(this string value)
        {
            value = value.ToLower();

            // Remove apostrophes
            value = value.Replace("'", "");
            // Replace any non alphanumerics with a dash
            value = Regex.Replace(value, "[^a-zA-Z0-9]", "-");
            // Remove lonely dashes at the start or end of the string
            value = Regex.Replace(value, "([-]$|^[-])", "");
            // Remove multiple dashes
            value = Regex.Replace(value, "([-]{2,})", "-");

            return value;
        }
    }
}