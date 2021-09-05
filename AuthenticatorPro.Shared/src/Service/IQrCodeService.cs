// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Entity;
using System.Threading.Tasks;

namespace AuthenticatorPro.Shared.Service
{
    public interface IQrCodeService
    {
        public Task<Authenticator> ParseOtpAuthUri(string uri);
        public Task<int> ParseOtpMigrationUri(string uri);
    }
}