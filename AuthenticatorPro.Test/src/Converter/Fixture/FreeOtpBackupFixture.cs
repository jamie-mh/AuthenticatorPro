// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.IO;

namespace AuthenticatorPro.Test.Converter.Fixture
{
    public class FreeOtpBackupFixture
    {
        public byte[] Data { get; }

        public FreeOtpBackupFixture()
        {
            var path = Path.Join("data", "freeotp.bin");
            Data = File.ReadAllBytes(path);
        }
    }
}