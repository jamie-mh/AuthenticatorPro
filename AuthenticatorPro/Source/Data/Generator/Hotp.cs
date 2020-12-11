using System;
using OtpNet;

namespace AuthenticatorPro.Data.Generator
{
    public class Hotp : ICounterBasedGenerator
    {
        private const int CooldownSeconds = 10;

        public long Counter { get; set; }
        
        private readonly OtpNet.Hotp _hotp;
        private DateTime _computedAt;

        public Hotp(string secret, OtpHashMode algorithm, int digits, long counter)
        {
            var secretBytes = Base32Encoding.ToBytes(secret);
            _hotp = new OtpNet.Hotp(secretBytes, algorithm, digits);
            Counter = counter;
        }

        public string Compute()
        {
            _computedAt = DateTime.Now;
            return _hotp.ComputeHOTP(Counter);
        }

        public DateTime GetRenewTime()
        {
            return _computedAt.AddSeconds(CooldownSeconds);
        }
    }
}