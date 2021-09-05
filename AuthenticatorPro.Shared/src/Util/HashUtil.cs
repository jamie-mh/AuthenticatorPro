// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AuthenticatorPro.Shared.Util
{
    public static class HashUtil
    {
        public static string Sha1(string input)
        {
            var hash = new SHA1Managed().ComputeHash(Encoding.UTF8.GetBytes(input));
            return String.Join("", hash.Select(b => b.ToString("x2")).ToArray());
        }

        public static string Md5(string input)
        {
            using var md5 = MD5.Create();
            var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));

            var builder = new StringBuilder();
            foreach (var b in hashBytes)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }
    }
}