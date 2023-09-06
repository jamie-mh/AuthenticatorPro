// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Newtonsoft.Json;
using SQLite;

namespace AuthenticatorPro.Core.Entity
{
    [Table("customicon")]
    public class CustomIcon
    {
        public const char Prefix = '@';
        public const int MaxSize = 128;

        [Column("id")]
        [PrimaryKey]
        public string Id { get; set; }

        [Column("data")]
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] Data { get; set; }
    }
}