// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content;
using AuthenticatorPro.Droid.Storage;

namespace AuthenticatorPro.Droid
{
    internal class SecureStorageWrapper
    {
        private readonly SecureStorage _secureStorage;

        public SecureStorageWrapper(Context context)
        {
            _secureStorage = new SecureStorage(context);
        }
        
        private const string AutoBackupPasswordKey = "autoBackupPassword";

        public string GetAutoBackupPassword()
        {
            return _secureStorage.Get(AutoBackupPasswordKey);
        }

        public void SetAutoBackupPassword(string value)
        {
            _secureStorage.Set(AutoBackupPasswordKey, value);
        }

        private const string DatabasePasswordKey = "databasePassword";

        public string GetDatabasePassword()
        {
            return _secureStorage.Get(DatabasePasswordKey);
        }

        public void SetDatabasePassword(string value)
        {
            _secureStorage.Set(DatabasePasswordKey, value);
        }
    }
}