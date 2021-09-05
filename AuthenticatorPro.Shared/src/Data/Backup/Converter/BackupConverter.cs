// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Threading.Tasks;

namespace AuthenticatorPro.Shared.Data.Backup.Converter
{
    public abstract class BackupConverter
    {
        public enum BackupPasswordPolicy
        {
            Never, Always, Maybe
        }

        protected readonly IIconResolver IconResolver;

        protected BackupConverter(IIconResolver iconResolver)
        {
            IconResolver = iconResolver;
        }

        public abstract BackupPasswordPolicy PasswordPolicy { get; }
        public abstract Task<Backup> ConvertAsync(byte[] data, string password = null);
    }
}