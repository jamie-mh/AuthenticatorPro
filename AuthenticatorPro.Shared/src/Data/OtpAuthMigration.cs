// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AuthenticatorPro.Shared.Data
{
    [ProtoContract]
    public class OtpAuthMigration
    {
        [ProtoContract]
        public enum Algorithm
        {
            [ProtoEnum] Unknown = 0,

            [ProtoEnum] Sha1 = 1
        }

        [ProtoContract]
        public enum Type
        {
            [ProtoEnum] Unknown = 0,

            [ProtoEnum] Hotp = 1,

            [ProtoEnum] Totp = 2
        }

        [ProtoContract]
        public class Authenticator
        {
            [ProtoMember(1)] public byte[] Secret { get; set; }

            [ProtoMember(2)] public string Username { get; set; }

            [ProtoMember(3)] public string Issuer { get; set; }

            [ProtoMember(4)] public Algorithm Algorithm { get; set; }

            [ProtoMember(5)] public int Unknown1 { get; set; }

            [ProtoMember(6)] public Type Type { get; set; }

            [ProtoMember(7)] public long Counter { get; set; }
        }

        [ProtoMember(1)] public List<Authenticator> Authenticators { get; set; }

        [ProtoMember(2)] public int Unknown2 { get; set; }

        [ProtoMember(3)] public int Unknown3 { get; set; }

        [ProtoMember(4)] public int Unknown4 { get; set; }

        [ProtoMember(5)] public int Unknown5 { get; set; }

        public static OtpAuthMigration FromOtpAuthMigrationUri(string uri)
        {
            var real = Uri.UnescapeDataString(uri);
            var match = Regex.Match(real, @"^otpauth-migration:\/\/offline\?data=(.*)$");

            if (!match.Success)
            {
                throw new ArgumentException("Invalid URI");
            }

            var rawData = match.Groups[1].Value;

            if (rawData.Length % 4 != 0)
            {
                var nextFactor = (rawData.Length + 4 - 1) / 4 * 4;
                rawData = rawData.PadRight(nextFactor, '=');
            }

            ReadOnlySpan<byte> protoMessage = Convert.FromBase64String(rawData);
            var migration = Serializer.Deserialize<OtpAuthMigration>(protoMessage);

            return migration;
        }
    }
}