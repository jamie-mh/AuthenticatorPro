using System.Text;

namespace AuthenticatorPro.Shared.Source.Data.Generator
{
    public class SteamOtp : Totp
    {
        public const int Digits = 5;
        private const int Period = 30;
        private const Algorithm Algorithm = Generator.Algorithm.Sha1;
        private const string Alphabet = "23456789BCDFGHJKMNPQRTVWXY";

        public SteamOtp(string secret) : base(secret, Period, Algorithm, Digits)
        {
            
        }

        protected override string Finalise(int material)
        {
            var builder = new StringBuilder();

            for(var i = 0; i < Digits; i++)
            {
                builder.Append(Alphabet[material % Alphabet.Length]);
                material /= Alphabet.Length;
            }

            return builder.ToString();
        }
    }
}
