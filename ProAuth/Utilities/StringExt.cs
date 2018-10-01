using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace ProAuth.Utilities
{
    public static class StringExt
    {
        public static string Truncate(this string value, int maxLength)
        {
            if(string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength); 
        }
    }
}