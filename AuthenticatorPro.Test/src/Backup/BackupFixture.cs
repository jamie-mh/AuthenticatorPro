// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Newtonsoft.Json;
using System.IO;

namespace AuthenticatorPro.Test.Backup
{
    public class BackupFixture
    {
        public Core.Backup.Backup Backup { get; }

        public BackupFixture()
        {
            var contents = File.ReadAllText("test.authpro");
            Backup = JsonConvert.DeserializeObject<Core.Backup.Backup>(contents);
        }
    }
}