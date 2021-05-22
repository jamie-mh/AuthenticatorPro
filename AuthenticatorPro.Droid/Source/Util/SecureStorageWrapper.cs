// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Threading.Tasks;
using Xamarin.Essentials;

namespace AuthenticatorPro.Droid.Util
{
    internal static class SecureStorageWrapper
    {
        private const string AutoBackupPasswordKey = "autoBackupPassword";
        
        public static async Task<string> GetAutoBackupPassword()
        {
            return await Get(AutoBackupPasswordKey);
        }
        
        public static async Task SetAutoBackupPassword(string value)
        {
            await Set(AutoBackupPasswordKey, value);
        }
        
        private const string DatabasePasswordKey = "databasePassword";
        
        public static async Task<string> GetDatabasePassword()
        {
            return await Get(DatabasePasswordKey);
        }
        
        public static async Task SetDatabasePassword(string value)
        {
            await Set(DatabasePasswordKey, value);
        }
        
        private static async Task Set(string key, string value)
        {
            await Task.Run(async delegate
            {
                if(value == null)
                    SecureStorage.Remove(key);
                else 
                    await SecureStorage.SetAsync(key, value);
            });
        }
        
        private static async Task<string> Get(string key)
        {
            // Don't call secure storage on the ui thread
            return await Task.Run(async () => await SecureStorage.GetAsync(key));
        }
    }
}