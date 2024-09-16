// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.Util;

namespace Stratum.WearOS
{
    internal static class Logger
    {
        private const string Tag = "STRATUMWEAR";

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

        public static void Debug(string message)
        {
#if DEBUG
            Log.Debug(Tag, message);
#endif
        }
    }
}