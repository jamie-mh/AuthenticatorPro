// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.IO;

namespace AuthenticatorPro.Test.Converter.Fixture
{
    public class BitwardenBackupFixture
    {
        public byte[] Data { get; }

        public BitwardenBackupFixture()
        {
            var path = Path.Join("data", "bitwarden.unencrypted.json");
            Data = File.ReadAllBytes(path);
        }
    }
}