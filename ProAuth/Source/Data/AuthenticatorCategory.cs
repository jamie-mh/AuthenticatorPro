using Newtonsoft.Json;
using SQLite;

namespace ProAuth.Data
{
    [Table("authenticatorcategory")]
    internal class AuthenticatorCategory
    {
        [Column("categoryId"), Indexed]
        public string CategoryId { get; set; }

        [Column("authenticatorSecret"), Indexed]
        public string AuthenticatorSecret { get; set; }

        [Column("ranking")]
        public int Ranking { get; set; }

        public AuthenticatorCategory()
        {
            Ranking = 0;
        }

        public AuthenticatorCategory(string categoryId, string authenticatorSecret)
        {
            CategoryId = categoryId;
            AuthenticatorSecret = authenticatorSecret;
            Ranking = 0;
        }
    }
}