// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content.PM;
using Android.OS;

namespace AuthenticatorPro.Droid.Util
{
    public static class PackageUtil
    {
        public static string GetVersionName(PackageManager packageManager, string packageName)
        {
            PackageInfo packageInfo;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                var flags = PackageManager.PackageInfoFlags.Of(0);
                packageInfo = packageManager.GetPackageInfo(packageName, flags);
            }
            else
            {
#pragma warning disable 618
                packageInfo = packageManager.GetPackageInfo(packageName, 0);
#pragma warning restore 618
            }

            return packageInfo.VersionName;
        }
    }
}