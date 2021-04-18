using System;
using Android.Util;

namespace AuthenticatorPro.Droid.Util
{
    internal static class Logger
    {
        private const string Tag = "AUTHPRO";

        public static void Error(string message)
        {
            Log.Error(Tag, message);
        }
        
        public static void Error(Exception e)
        {
            Log.Error(Tag, e.ToString());
        }
        
        public static void Info(string message)
        {
            Log.Info(Tag, message);
        }
    }
}