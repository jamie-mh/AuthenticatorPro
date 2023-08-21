// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.IO;

namespace AuthenticatorPro.Test.Converter.Fixture
{
    public class LastPassBackupFixture
    {
        public byte[] Data { get; }

        public LastPassBackupFixture()
        {
            var path = Path.Join("data", "lastpass.json");
            Data = File.ReadAllBytes(path);
        }
    }
}