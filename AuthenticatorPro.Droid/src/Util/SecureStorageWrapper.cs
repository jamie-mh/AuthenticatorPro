// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Threading.Tasks;
using Xamarin.Essentials;

namespace AuthenticatorPro.Droid.Util
{
    internal static class SecureStorageWrapper
    {
        private const string AutoBackupPasswordKey = "autoBackupPassword";

        public static Task<string> GetAutoBackupPassword()
        {
            return Get(AutoBackupPasswordKey);
        }

        public static Task SetAutoBackupPassword(string value)
        {
            return Set(AutoBackupPasswordKey, value);
        }

        private const string DatabasePasswordKey = "databasePassword";

        public static Task<string> GetDatabasePassword()
        {
            return Get(DatabasePasswordKey);
        }

        public static Task SetDatabasePassword(string value)
        {
            return Set(DatabasePasswordKey, value);
        }

        private static async Task Set(string key, string value)
        {
            await Task.Run(async delegate
            {
                if (value == null)
                {
                    SecureStorage.Remove(key);
                }
                else
                {
                    await SecureStorage.SetAsync(key, value);
                }
            });
        }

        private static Task<string> Get(string key)
        {
            // Don't call secure storage on the ui thread
            return Task.Run(() => SecureStorage.GetAsync(key));
        }
    }
}