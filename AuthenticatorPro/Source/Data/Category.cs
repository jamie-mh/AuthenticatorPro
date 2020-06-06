using AuthenticatorPro.Util;
using SQLite;

namespace AuthenticatorPro.Data
{
    [Table("category")]
    internal class Category
    {
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

        [Column("id")] [PrimaryKey] public string Id { get; set; }

        [Column("name")] public string Name { get; set; }

        [Column("ranking")] public int Ranking { get; set; }
    }
}