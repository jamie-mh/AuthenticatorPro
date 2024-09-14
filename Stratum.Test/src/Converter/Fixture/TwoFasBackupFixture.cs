// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.IO;

namespace Stratum.Test.Converter.Fixture
{
    public class TwoFasBackupFixture
    {
        public TwoFasBackupFixture()
        {
            var unencryptedPath = Path.Join("data", "twofas.unencrypted.2fas");
            UnencryptedData = File.ReadAllBytes(unencryptedPath);

            var encryptedPath = Path.Join("data", "twofas.encrypted.2fas");
            EncryptedData = File.ReadAllBytes(encryptedPath);
        }

        public byte[] UnencryptedData { get; }
        public byte[] EncryptedData { get; }
    }
}