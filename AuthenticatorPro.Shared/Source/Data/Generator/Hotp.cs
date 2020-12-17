using System;
using OtpNet;

namespace AuthenticatorPro.Shared.Data.Generator
{
    public class Hotp : CounterBasedGenerator
    {
        public const int CooldownSeconds = 10;

        private readonly OtpNet.Hotp _hotp;
        private DateTime _computedAt;
        private long _counter;
        public override long Counter
        {
            set => _counter = value;
        }
        
        public Hotp(string secret, OtpHashMode algorithm, int digits, long counter)
        {
            var secretBytes = Base32Encoding.ToBytes(secret);
            _hotp = new OtpNet.Hotp(secretBytes, algorithm, digits);
            _counter = counter;
        }

        public override string Compute()
        {
            _computedAt = DateTime.UtcNow;
            return _hotp.ComputeHOTP(_counter);
        }

        public override DateTime GetRenewTime()
        {
            return _computedAt.AddSeconds(CooldownSeconds);
        }
    }
}