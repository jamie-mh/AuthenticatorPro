// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Util;
using Serilog.Core;
using Serilog.Events;
using System;

namespace AuthenticatorPro.Droid
{
    public class LogcatSink : ILogEventSink
    {
        private const string Tag = "AUTHPRO";

        public void Emit(LogEvent logEvent)
        {
            var message = logEvent.RenderMessage();

            switch (logEvent.Level)
            {
                case LogEventLevel.Verbose:
                    Log.Verbose(Tag, message);
                    break;

                case LogEventLevel.Debug:
                    Log.Debug(Tag, message);
                    break;

                case LogEventLevel.Information:
                    Log.Info(Tag, message);
                    break;

                case LogEventLevel.Warning:
                    if (logEvent.Exception != null)
                    {
                        Log.Warn(Tag, message + Environment.NewLine + logEvent.Exception);
                    }
                    else
                    {
                        Log.Warn(Tag, message);
                    }
                    break;

                case LogEventLevel.Error:
                case LogEventLevel.Fatal:
                    if (logEvent.Exception != null)
                    {
                        Log.Error(Tag, message + Environment.NewLine + logEvent.Exception);
                    }
                    else
                    {
                        Log.Error(Tag, message);
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(logEvent), "Unknown log level");
            }
        }
    }
}