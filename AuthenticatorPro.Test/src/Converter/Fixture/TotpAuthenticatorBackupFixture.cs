// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.IO;

namespace AuthenticatorPro.Test.Converter.Fixture
{
    public class TotpAuthenticatorBackupFixture
    {
        public byte[] EncryptedData { get; }

        public TotpAuthenticatorBackupFixture()
        {
            var encryptedPath = Path.Join("data", "totpauthenticator.encrypted.bin");
            EncryptedData = File.ReadAllBytes(encryptedPath);
        }
    }
}