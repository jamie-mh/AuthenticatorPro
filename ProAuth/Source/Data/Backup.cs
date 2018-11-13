using System.Collections.Generic;

namespace ProAuth.Data
{
    internal class Backup
    {
        public List<Authenticator> Authenticators { get; set; }
        public List<Category> Categories { get; set; }
        public List<AuthenticatorCategory> AuthenticatorCategories { get; set; }
    }
}