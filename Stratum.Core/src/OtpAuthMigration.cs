// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using ProtoBuf;

namespace Stratum.Core
{
    [ProtoContract]
    public class OtpAuthMigration
    {
        [ProtoContract]
        public enum Algorithm
        {
            [ProtoEnum] Unknown = 0,

            [ProtoEnum] Sha1 = 1,

            [ProtoEnum] Sha256 = 2,

            [ProtoEnum] Sha512 = 3
        }

        [ProtoContract]
        public enum Type
        {
            [ProtoEnum] Unknown = 0,

            [ProtoEnum] Hotp = 1,

            [ProtoEnum] Totp = 2
        }

        [ProtoMember(1)]
        public List<MigrationAuthenticator> Authenticators { get; set; }

        [ProtoContract]
        public class MigrationAuthenticator
        {
            [ProtoMember(1)]
            public byte[] Secret { get; set; }

            [ProtoMember(2)]
            public string Username { get; set; }

            [ProtoMember(3)]
            public string Issuer { get; set; }

            [ProtoMember(4)]
            public Algorithm Algorithm { get; set; }

            [ProtoMember(6)]
            public Type Type { get; set; }

            [ProtoMember(7)]
            public long Counter { get; set; }
        }
    }
}