// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.IO;

namespace AuthenticatorPro.Test.Converter.Fixture
{
    public class EnteBackupFixture
    {
        public EnteBackupFixture()
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