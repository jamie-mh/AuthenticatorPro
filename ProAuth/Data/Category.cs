using System;
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

        public Category()
        {

        }

        public Category(string name)
        {
            Id = Hash.SHA1(name).Truncate(8);
            Name = name;
        }
    }
}