// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

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

        public bool CanBeDecrypted(byte[] data)
        {
            Backup backup;

            try
            {
                var json = Encoding.UTF8.GetString(data);
                backup = JsonConvert.DeserializeObject<Backup>(json);
            }
            catch (Exception)
            {
                return false;
            }

            return backup?.Authenticators != null;
        }
    }
}