// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Newtonsoft.Json;
using ProtoBuf;
using SQLite;

namespace AuthenticatorPro.Core.Entity
{
    [ProtoContract]
    [Table("iconpackentry")]
    public class IconPackEntry
    {
        [ProtoIgnore]
        [Indexed]
        [Column("iconPackName")]
        public string IconPackName { get; set; }

        [ProtoMember(1)]
        [Indexed]
        [Column("name")]
        public string Name { get; set; }

        [ProtoMember(2)]
        [Column("data")]
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] Data { get; set; }
    }
}