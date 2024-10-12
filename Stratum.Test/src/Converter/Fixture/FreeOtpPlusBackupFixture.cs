// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.IO;

namespace Stratum.Test.Converter.Fixture
{
    public class FreeOtpPlusBackupFixture
    {
        public FreeOtpPlusBackupFixture()
        {
            var path = Path.Join("data", "freeotpplus.json");
            Data = File.ReadAllBytes(path);
        }

        public byte[] Data { get; }
    }
}