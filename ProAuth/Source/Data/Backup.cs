using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace ProAuth.Data
{
    internal class Backup
    {
        public List<Authenticator> Authenticators { get; set; }
        public List<Category> Categories { get; set; }
        public List<AuthenticatorCategory> AuthenticatorCategories { get; set; }
    }
}