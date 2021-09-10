// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Data.Backup;
using System.Threading.Tasks;

namespace AuthenticatorPro.Shared.Service
{
    public interface IRestoreService
    {
        public Task<RestoreResult> RestoreAsync(Backup backup);
        public Task<RestoreResult> RestoreAndUpdateAsync(Backup backup);
    }
}