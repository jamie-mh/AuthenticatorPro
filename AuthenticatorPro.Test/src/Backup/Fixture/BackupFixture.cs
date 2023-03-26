// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Newtonsoft.Json;
using System.IO;

namespace AuthenticatorPro.Test.Backup.Fixture
{
    public class BackupFixture
    {
        public Core.Backup.Backup Backup { get; }
        public byte[] LegacyData { get; }
        public byte[] StrongData { get; }

        public BackupFixture()
        {
            var unencryptedPath = Path.Join("data", "backup.unencrypted.authpro");
            var contents = File.ReadAllText(unencryptedPath);
            Backup = JsonConvert.DeserializeObject<Core.Backup.Backup>(contents);

            var legacyPath = Path.Join("data", "backup.legacy.authpro");
            LegacyData = File.ReadAllBytes(legacyPath);

            var strongPath = Path.Join("data", "backup.strong.authpro");
            StrongData = File.ReadAllBytes(strongPath);
        }
    }
}