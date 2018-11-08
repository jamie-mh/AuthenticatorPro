using System;
using ProAuth.Utilities;
using SQLite;

namespace ProAuth.Data
{
    [Table("category")]
    internal class Category
    {
        [Column("id")]
        public string Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        public Category()
        {

        }

        public Category(string name)
        {
            Id = name.GetSlug();
            Name = name;
        }
    }
}