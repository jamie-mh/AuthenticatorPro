// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Backup;
using System.Threading.Tasks;

namespace AuthenticatorPro.Core.Service
{
    public interface IBackupService
    {
        public Task<Backup.Backup> CreateBackupAsync();
        public Task<HtmlBackup> CreateHtmlBackupAsync();
        public Task<UriListBackup> CreateUriListBackupAsync();
    }
}