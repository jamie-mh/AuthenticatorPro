// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.IO;

namespace Stratum.Test.Converter.Fixture
{
    public class WinAuthBackupFixture
    {
        public WinAuthBackupFixture()
        {
            Data = File.ReadAllBytes(Path.Join("data", "winauth.zip"));
            InvalidData = File.ReadAllBytes(Path.Join("data", "winauth.invalid.zip"));
        }

        public byte[] Data { get; }
        public byte[] InvalidData { get; }
    }
}