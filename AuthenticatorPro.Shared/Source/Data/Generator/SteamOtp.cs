using System;
using System.Text;
using OtpNet;

namespace AuthenticatorPro.Shared.Data.Generator
{
    public class SteamOtp : IGenerator
    {
        public const int Digits = SteamTotp.NumDigits;
        private readonly OtpNet.Totp _totp;

        public GenerationMethod GenerationMethod => GenerationMethod.Time;

        public SteamOtp(string secret)
        {
            var secretBytes = Base32Encoding.ToBytes(secret);
            _totp = new SteamTotp(secretBytes);
        }

        public string Compute()
        {
            return _totp.ComputeTotp();
        }

        public DateTime GetRenewTime() {
            return DateTime.UtcNow.AddSeconds(_totp.RemainingSeconds());
        }

        private class SteamTotp : OtpNet.Totp
        {
            public const int NumDigits = 5;

            private const string Alphabet = "23456789BCDFGHJKMNPQRTVWXY";

            public SteamTotp(byte[] secretKey) : base(secretKey, 30, OtpHashMode.Sha1, NumDigits)
            {
            }

            protected override string Compute(long counter, OtpHashMode mode)
            {
                // As base.Compute(long, OtpHashMode), but doesn't call Digits(long, int)
                var data = BitConverter.GetBytes(counter);
                Array.Reverse(data);
                var otp = (int) CalculateOtp(data, mode);

                var builder = new StringBuilder();

                for(var i = 0; i < NumDigits; i++) {
                    builder.Append(Alphabet[otp % Alphabet.Length]);
                    otp /= Alphabet.Length;
                }

                return builder.ToString();
            }
        }
    }
}
