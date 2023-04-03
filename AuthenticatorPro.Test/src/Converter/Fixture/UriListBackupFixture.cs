// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.IO;

namespace AuthenticatorPro.Test.Converter.Fixture
{
    public class UriListBackupFixture
    {
        public byte[] Data { get; }

        public UriListBackupFixture()
        {
            var path = Path.Join("data", "urilist.txt");
            Data = File.ReadAllBytes(path);
        }
    }
}