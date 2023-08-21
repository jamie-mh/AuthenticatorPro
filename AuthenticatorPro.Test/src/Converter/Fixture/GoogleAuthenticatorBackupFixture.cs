// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.IO;

namespace AuthenticatorPro.Test.Converter.Fixture
{
    public class GoogleAuthenticatorBackupFixture
    {
        public byte[] Data { get; }

        public GoogleAuthenticatorBackupFixture()
        {
            var path = Path.Join("data", "googleauthenticator.txt");
            Data = File.ReadAllBytes(path);
        }
    }
}