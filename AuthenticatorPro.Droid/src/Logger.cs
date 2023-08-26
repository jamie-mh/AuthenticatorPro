// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.Util;

namespace AuthenticatorPro.Droid
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

        public static void Error(string message, Exception e)
        {
            Log.Error(Tag, message + Environment.NewLine + e);
        }

        public static void Warn(string message, Exception e)
        {
            Log.Warn(Tag, message + Environment.NewLine + e);
        }

        public static void Warn(Exception e)
        {
            Log.Warn(Tag, e.ToString());
        }

        public static void Info(string message)
        {
            Log.Info(Tag, message);
        }

        public static void Debug(string message)
        {
#if DEBUG
            Log.Debug(Tag, message);
#endif
        }
    }
}