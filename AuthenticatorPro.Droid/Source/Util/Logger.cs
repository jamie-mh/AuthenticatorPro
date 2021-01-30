using System;
using Android.Util;

namespace AuthenticatorPro.Droid.Util
{
    internal static class Logger
    {
        private const string Tag = "AUTHPRO";

        public static void Error(string message)
        {
            Log.Error(Tag, $"{message}. Report at {Constants.GitHubRepo}");
        }
        
        public static void Error(Exception e)
        {
            Log.Error(Tag, $"Unexpected exception: {e}. Report at {Constants.GitHubRepo}");
        }
        
        public static void Info(string message)
        {
            Log.Info(Tag, message);
        }
    }
}