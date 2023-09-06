// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Util;
using SQLite;

namespace AuthenticatorPro.Core.Entity
{
    [Table("category")]
    public class Category
    {
        private const int IdLength = 8;
        public const int NameMaxLength = 32;

        public Category()
        {
            Ranking = 0;
        }

        public Category(string name)
        {
            name = name.Trim().Truncate(NameMaxLength);
            Id = HashUtil.Sha1(name).Truncate(IdLength);
            Name = name;
            Ranking = 0;
        }

        [Column("id")]
        [MaxLength(IdLength)]
        [PrimaryKey]
        public string Id { get; set; }

        [Column("name")]
        [MaxLength(NameMaxLength)]
        public string Name { get; set; }

        [Column("ranking")]
        public int Ranking { get; set; }
    }
}