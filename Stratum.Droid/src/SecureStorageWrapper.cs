// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content;
using Stratum.Droid.Storage;

namespace Stratum.Droid
{
    public class SecureStorageWrapper
    {
        private const string AutoBackupPasswordKey = "autoBackupPassword";
        private const string DatabasePasswordKey = "databasePassword";

        private readonly SecureStorage _secureStorage;

        public SecureStorageWrapper(Context context)
        {
            _secureStorage = new SecureStorage(context);
        }

        public string GetAutoBackupPassword()
        {
            return _secureStorage.Get(AutoBackupPasswordKey);
        }

        public void SetAutoBackupPassword(string value)
        {
            _secureStorage.Set(AutoBackupPasswordKey, value);
        }

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