// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Newtonsoft.Json;
using System.IO;

namespace AuthenticatorPro.Test.Backup.Fixture
{
    public class BackupFixture
    {
        public Core.Backup.Backup Backup { get; }
        public byte[] EncryptedData { get; }

        public BackupFixture()
        {
            var path = Path.Join("data", "backup.unencrypted.authpro");
            var contents = File.ReadAllText(path);
            Backup = JsonConvert.DeserializeObject<Core.Backup.Backup>(contents);

            var encryptedPath = Path.Join("data", "backup.encrypted.authpro");
            EncryptedData = File.ReadAllBytes(encryptedPath);
        }
    }
}