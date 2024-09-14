// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Threading.Tasks;
using Stratum.Core.Backup;

namespace Stratum.Core.Converter
{
    public abstract class BackupConverter
    {
        public enum BackupPasswordPolicy
        {
            Never,
            Always,
            Maybe
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