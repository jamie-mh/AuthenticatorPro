using SQLite;

namespace AuthenticatorPro.Shared.Data
{
    [Table("authenticatorcategory")]
    public class AuthenticatorCategory
    {
        [Column("categoryId")]
        [Indexed]
        public string CategoryId { get; set; }

        [Column("authenticatorSecret")]
        [Indexed]
        public string AuthenticatorSecret { get; set; }

        [Column("ranking")]
        public int Ranking { get; set; }


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