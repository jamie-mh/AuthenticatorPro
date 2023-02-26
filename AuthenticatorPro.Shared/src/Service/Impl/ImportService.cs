// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Backup;
using AuthenticatorPro.Shared.Backup.Converter;
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

        public async Task<ValueTuple<ConversionResult, RestoreResult>> ImportAsync(BackupConverter converter, byte[] data, string password)
        {
            if (converter == null)
            {
                throw new ArgumentNullException(nameof(converter));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var conversionResult = await converter.ConvertAsync(data, password);
            var restoreResult = await _restoreService.RestoreAsync(conversionResult.Backup);

            return new ValueTuple<ConversionResult, RestoreResult>(conversionResult, restoreResult);
        }
    }
}