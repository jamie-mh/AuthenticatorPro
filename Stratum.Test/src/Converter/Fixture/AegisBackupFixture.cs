// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.IO;

namespace Stratum.Test.Converter.Fixture
{
    public class AegisBackupFixture
    {
        public AegisBackupFixture()
        {
            var unencryptedPath = Path.Join("data", "aegis.unencrypted.json");
            UnencryptedData = File.ReadAllBytes(unencryptedPath);

            var encryptedPath = Path.Join("data", "aegis.encrypted.json");
            EncryptedData = File.ReadAllBytes(encryptedPath);
        }

        public byte[] UnencryptedData { get; }
        public byte[] EncryptedData { get; }
    }
}