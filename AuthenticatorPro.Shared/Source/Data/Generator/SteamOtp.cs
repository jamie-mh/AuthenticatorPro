using System;
using System.Text;
using OtpNet;

namespace AuthenticatorPro.Shared.Data.Generator
{
    public class SteamOtp : IGenerator
    {
        public const string Alphabet = "23456789BCDFGHJKMNPQRTVWXY";
        public const int Digits = 5;

        private readonly OtpNet.Totp _totp;

        public GenerationMethod GenerationMethod => GenerationMethod.Time;

        public SteamOtp(string secret, int digits)
        {
            var secretBytes = Base32Encoding.ToBytes(secret);
            _totp = new SteamTotp(secretBytes, digits);
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
            private readonly int _digits;

            public SteamTotp(byte[] secretKey, int digits = 5) : base(secretKey, 30, OtpHashMode.Sha1, digits)
            {
                _digits = digits;
            }

            protected override string Compute(long counter, OtpHashMode mode)
            {
                // As base.Compute(long, OtpHashMode), but doesn't call Digits(long, int)
                var data = BitConverter.GetBytes(counter);
                Array.Reverse(data);
                var otp = (int) CalculateOtp(data, mode);

                var builder = new StringBuilder();

                for (int i = 0; i < _digits; i++) {
                    builder.Append(Alphabet[otp % Alphabet.Length]);
                    otp /= Alphabet.Length;
                }

                return builder.ToString();
            }
        }
    }
}
