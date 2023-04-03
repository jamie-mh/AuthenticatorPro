// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.IO;

namespace AuthenticatorPro.Test.Converter.Fixture
{
    public class AuthenticatorPlusBackupFixture
    {
        public byte[] Data { get; }

        public AuthenticatorPlusBackupFixture()
        {
            var path = Path.Join("data", "authplus.db");
            Data = File.ReadAllBytes(path);
        }
    }
}