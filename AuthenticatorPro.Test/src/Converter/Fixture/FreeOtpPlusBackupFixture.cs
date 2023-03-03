// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.IO;

namespace AuthenticatorPro.Test.Converter.Fixture
{
    public class FreeOtpPlusBackupFixture
    {
        public byte[] Data { get; }

        public FreeOtpPlusBackupFixture()
        {
            var path = Path.Join("data", "freeotpplus.json");
            Data = File.ReadAllBytes(path);
        }
    }
}