// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.IO;

namespace Stratum.Test.Converter.Fixture
{
    public class TotpAuthenticatorBackupFixture
    {
        public TotpAuthenticatorBackupFixture()
        {
            var encryptedPath = Path.Join("data", "totpauthenticator.encrypted.bin");
            EncryptedData = File.ReadAllBytes(encryptedPath);
        }

        public byte[] EncryptedData { get; }
    }
}