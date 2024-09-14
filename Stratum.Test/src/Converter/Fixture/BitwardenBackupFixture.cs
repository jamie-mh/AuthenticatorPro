// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.IO;

namespace Stratum.Test.Converter.Fixture
{
    public class BitwardenBackupFixture
    {
        public BitwardenBackupFixture()
        {
            Data = File.ReadAllBytes(Path.Join("data", "bitwarden.unencrypted.json"));
            EncryptedPbkdf2Data = File.ReadAllBytes(Path.Join("data", "bitwarden.encrypted.pbkdf2.json"));
            EncryptedArgon2IdData = File.ReadAllBytes(Path.Join("data", "bitwarden.encrypted.argon2id.json"));
            EncryptedAccountRestrictedData =
                File.ReadAllBytes(Path.Join("data", "bitwarden.encrypted.accountrestricted.json"));
            AuthenticatorData = File.ReadAllBytes(Path.Join("data", "bitwardenauthenticator.json"));
        }

        public byte[] Data { get; }
        public byte[] EncryptedPbkdf2Data { get; }
        public byte[] EncryptedArgon2IdData { get; }
        public byte[] EncryptedAccountRestrictedData { get; }
        public byte[] AuthenticatorData { get; }
    }
}