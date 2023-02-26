// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Backup;
using System.Threading.Tasks;

namespace AuthenticatorPro.Core.Converter
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
        public abstract Task<ConversionResult> ConvertAsync(byte[] data, string password = null);
    }
}