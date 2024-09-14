// Copyright (C) 2024 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.IO;

namespace Stratum.Test.Converter.Fixture
{
    public class EnteAuthBackupFixture
    {
        public EnteAuthBackupFixture()
        {
            var unencryptedPath = Path.Join("data", "ente.unencrypted.txt");
            UnencryptedData = File.ReadAllBytes(unencryptedPath);

            var encryptedPath = Path.Join("data", "ente.encrypted.json");
            EncryptedData = File.ReadAllBytes(encryptedPath);
        }

        public byte[] UnencryptedData { get; }
        public byte[] EncryptedData { get; }
    }
}