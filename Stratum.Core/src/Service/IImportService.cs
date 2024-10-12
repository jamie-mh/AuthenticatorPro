// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Threading.Tasks;
using Stratum.Core.Backup;
using Stratum.Core.Converter;

namespace Stratum.Core.Service
{
    public interface IImportService
    {
        public Task<ValueTuple<ConversionResult, RestoreResult>> ImportAsync(BackupConverter converter, byte[] data,
            string password);
    }
}