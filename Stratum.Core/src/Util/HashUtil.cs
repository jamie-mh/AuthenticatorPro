// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Security.Cryptography;
using System.Text;

namespace Stratum.Core.Util
{
    public static class HashUtil
    {
        public static string Sha1(string input)
        {
            var hash = SHA1.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash).ToLower();
        }

        public static string Md5(string input)
        {
            var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash).ToLower();
        }
    }
}