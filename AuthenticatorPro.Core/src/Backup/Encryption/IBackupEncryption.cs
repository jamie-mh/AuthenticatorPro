// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Threading.Tasks;

namespace AuthenticatorPro.Core.Backup.Encryption
{
    public interface IBackupEncryption
    {
        public Task<byte[]> EncryptAsync(Backup backup, string password);
        public Task<Backup> DecryptAsync(byte[] data, string password);
        public bool CanBeDecrypted(byte[] data);
    }
}