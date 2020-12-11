using System;
using OtpNet;

namespace AuthenticatorPro.Data.Generator
{
    public class Totp : IGenerator
    {
        private readonly OtpNet.Totp _totp;

        public GenerationMethod GenerationMethod => GenerationMethod.Time;

        public Totp(string secret, int period, OtpHashMode algorithm, int digits)
        {
            var secretBytes = Base32Encoding.ToBytes(secret);
            _totp = new OtpNet.Totp(secretBytes, period, algorithm, digits); 
        }

        public string Compute()
        {
            return _totp.ComputeTotp();
        }

        public DateTime GetRenewTime()
        {
            return DateTime.UtcNow.AddSeconds(_totp.RemainingSeconds());
        }
    }
}