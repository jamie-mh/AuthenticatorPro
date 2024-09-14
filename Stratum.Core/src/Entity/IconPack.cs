// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using ProtoBuf;
using SQLite;

namespace Stratum.Core.Entity
{
    [ProtoContract]
    [Table("iconpack")]
    public class IconPack
    {
        [PrimaryKey]
        [Column("name")]
        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        [Column("description")]
        public string Description { get; set; }

        [ProtoMember(3)]
        [Column("url")]
        public string Url { get; set; }

        [ProtoMember(4)]
        [Ignore]
        public List<IconPackEntry> Icons { get; set; }
    }
}