// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;

namespace AuthenticatorPro.Core.Backup.Encryption
{
    public class NoBackupEncryption : IBackupEncryption
    {
        public Task<byte[]> EncryptAsync(Backup backup, string password)
        {
            var json = JsonConvert.SerializeObject(backup);
            return Task.FromResult(Encoding.UTF8.GetBytes(json));
        }

        public Task<Backup> DecryptAsync(byte[] data, string password)
        {
            var json = Encoding.UTF8.GetString(data);
            return Task.FromResult(JsonConvert.DeserializeObject<Backup>(json));
        }
    }
}