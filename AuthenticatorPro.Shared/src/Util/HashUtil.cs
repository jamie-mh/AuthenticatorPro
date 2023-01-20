// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AuthenticatorPro.Shared.Util
{
    public static class HashUtil
    {
        private static string BytesToHex(IEnumerable<byte> data)
        {
            return String.Join("", data.Select(b => b.ToString("x2")).ToArray());
        }

        public static string Sha1(string input)
        {
            var hash = SHA1.HashData(Encoding.UTF8.GetBytes(input));
            return BytesToHex(hash);
        }

        public static string Md5(string input)
        {
            var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
            return BytesToHex(hash);
        }
    }
}