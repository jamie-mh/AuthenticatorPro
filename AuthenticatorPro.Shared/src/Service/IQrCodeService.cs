// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Threading.Tasks;

namespace AuthenticatorPro.Shared.Service
{
    public interface IQrCodeService
    {
        public Task<int> ParseOtpMigrationUri(string uri);
    }
}