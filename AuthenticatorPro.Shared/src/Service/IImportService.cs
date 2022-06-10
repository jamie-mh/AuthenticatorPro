// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Data.Backup;
using AuthenticatorPro.Shared.Data.Backup.Converter;
using System.Threading.Tasks;

namespace AuthenticatorPro.Shared.Service
{
    public interface IImportService
    {
        public Task<RestoreResult> ImportAsync(BackupConverter converter, byte[] data, string password);
    }
}