// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using SQLite;

namespace AuthenticatorPro.Core.Entity
{
    [Table("authenticatorcategory")]
    public class AuthenticatorCategory
    {
        [Column("categoryId")] [Indexed] public string CategoryId { get; set; }

        [Column("authenticatorSecret")]
        [Indexed]
        public string AuthenticatorSecret { get; set; }

        [Column("ranking")] public int Ranking { get; set; }

        public AuthenticatorCategory()
        {
            Ranking = 0;
        }

        public AuthenticatorCategory(string authenticatorSecret, string categoryId)
        {
            AuthenticatorSecret = authenticatorSecret;
            CategoryId = categoryId;
            Ranking = 0;
        }
    }
}