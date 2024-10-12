// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.IO;

namespace Stratum.Test.Converter.Fixture
{
    public class AndOtpBackupFixture
    {
        public AndOtpBackupFixture()
        {
            var unencryptedPath = Path.Join("data", "andotp.unencrypted.json");
            UnencryptedData = File.ReadAllBytes(unencryptedPath);

            var encryptedPath = Path.Join("data", "andotp.encrypted.bin");
            EncryptedData = File.ReadAllBytes(encryptedPath);
        }

        public byte[] UnencryptedData { get; }
        public byte[] EncryptedData { get; }
    }
}