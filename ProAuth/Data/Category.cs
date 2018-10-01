using SQLite;

namespace ProAuth.Data
{
    [Table("category")]
    class Category
    {
        [Column("id"), PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Column("name"), MaxLength(32)]
        public string Name { get; set; }
    }
}