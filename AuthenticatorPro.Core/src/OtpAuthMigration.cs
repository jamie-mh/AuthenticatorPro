// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using ProtoBuf;
using System.Collections.Generic;

namespace AuthenticatorPro.Core
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
        public class MigrationAuthenticator
        {
            [ProtoMember(1)] public byte[] Secret { get; set; }

            [ProtoMember(2)] public string Username { get; set; }

            [ProtoMember(3)] public string Issuer { get; set; }

            [ProtoMember(4)] public Algorithm Algorithm { get; set; }

            [ProtoMember(5)] public int Unknown1 { get; set; }

            [ProtoMember(6)] public Type Type { get; set; }

            [ProtoMember(7)] public long Counter { get; set; }
        }

        [ProtoMember(1)] public List<MigrationAuthenticator> Authenticators { get; set; }

        [ProtoMember(2)] public int Unknown2 { get; set; }

        [ProtoMember(3)] public int Unknown3 { get; set; }

        [ProtoMember(4)] public int Unknown4 { get; set; }

        [ProtoMember(5)] public int Unknown5 { get; set; }
    }
}