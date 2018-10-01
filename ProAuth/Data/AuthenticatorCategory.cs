using SQLite;

namespace ProAuth.Data
{
    [Table("authenticatorcategory")]
    class AuthenticatorCategory
    {
        [Column("authenticatorId"), PrimaryKey]
        public int AuthenticatorId { get; set; }

        [Column("categoryId"), PrimaryKey]
        public int CategoryId { get; set; }
    }
}