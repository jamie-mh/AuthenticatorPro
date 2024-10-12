// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.IO;

namespace Stratum.Test.Converter.Fixture
{
    public class GoogleAuthenticatorBackupFixture
    {
        public GoogleAuthenticatorBackupFixture()
        {
            var path = Path.Join("data", "googleauthenticator.txt");
            Data = File.ReadAllBytes(path);
        }

        public byte[] Data { get; }
    }
}