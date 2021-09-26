// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Data.Backup;
using AuthenticatorPro.Shared.Data.Backup.Converter;
using System;
using System.Threading.Tasks;

namespace AuthenticatorPro.Shared.Service.Impl
{
    public class ImportService : IImportService
    {
        private readonly IRestoreService _restoreService;

        public ImportService(IRestoreService restoreService)
        {
            _restoreService = restoreService;
        }

        public async Task<RestoreResult> ImportAsync(BackupConverter converter, byte[] data, string password)
        {
            if (converter == null)
            {
                throw new ArgumentNullException(nameof(converter));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var backup = await converter.ConvertAsync(data, password);
            return await _restoreService.RestoreAsync(backup);
        }
    }
}