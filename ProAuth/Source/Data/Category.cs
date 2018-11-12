using System;
using Newtonsoft.Json;
using ProAuth.Utilities;
using SQLite;

namespace ProAuth.Data
{
    [Table("category")]
    internal class Category
    {
        [Column("id"), PrimaryKey]
        public string Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("ranking")]
        public int Ranking { get; set; }

        public Category()
        {
            Ranking = 1;
        }

        public Category(string name)
        {
            name = name.Trim();
            Id = Hash.SHA1(name).Truncate(8);
            Name = name;
            Ranking = 1;
        }
    }
}