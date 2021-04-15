using System.Collections.Generic;
using AuthenticatorPro.Shared.Source.Data;
using AuthenticatorPro.Shared.Source.Data.Generator;

namespace AuthenticatorPro.Droid.Shared.Query
{
    public class WearAuthenticator
    {
        public readonly AuthenticatorType Type;
        public readonly string Secret;
        public readonly string Icon;
        public readonly string Issuer;
        public readonly string Username;
        public readonly int Period;
        public readonly int Digits;
        public readonly Algorithm Algorithm;
        public readonly int Ranking;
        public readonly List<WearAuthenticatorCategory> Categories;


        public WearAuthenticator(AuthenticatorType type, string secret, string icon, string issuer, string username, int period, int digits, Algorithm algorithm, int ranking, List<WearAuthenticatorCategory> categories)
        {
            Type = type;
            Secret = secret;
            Icon = icon;
            Issuer = issuer;
            Username = username;
            Period = period;
            Digits = digits;
            Algorithm = algorithm;
            Ranking = ranking;
            Categories = categories;
        }
    }
}