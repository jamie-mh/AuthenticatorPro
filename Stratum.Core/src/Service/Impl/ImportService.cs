// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Threading.Tasks;
using Stratum.Core.Backup;
using Stratum.Core.Converter;

namespace Stratum.Core.Service.Impl
{
    public class ImportService : IImportService
    {
        private readonly IRestoreService _restoreService;

        public ImportService(IRestoreService restoreService)
        {
            _restoreService = restoreService;
        }

        public async Task<ValueTuple<ConversionResult, RestoreResult>> ImportAsync(BackupConverter converter,
            byte[] data, string password)
        {
            ArgumentNullException.ThrowIfNull(converter);
            ArgumentNullException.ThrowIfNull(data);

            var conversionResult = await converter.ConvertAsync(data, password);
            var restoreResult = await _restoreService.RestoreAsync(conversionResult.Backup);

            return new ValueTuple<ConversionResult, RestoreResult>(conversionResult, restoreResult);
        }
    }
}