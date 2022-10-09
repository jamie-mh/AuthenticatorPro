// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.App;

namespace AuthenticatorPro.Droid.Util
{
    public static class LifecycleUtil
    {
        public static bool IsApplicationInForeground()
        {
            var processInfo = new ActivityManager.RunningAppProcessInfo();
            ActivityManager.GetMyMemoryState(processInfo);
            return (processInfo.Importance == Importance.Foreground || processInfo.Importance == Importance.Visible);
        }
    }
}