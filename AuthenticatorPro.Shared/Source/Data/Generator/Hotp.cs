using System;
using AuthenticatorPro.Shared.Util;

namespace AuthenticatorPro.Shared.Data.Generator
{
    public class Hotp : HmacOtp, ICounterBasedGenerator
    {
        private const int CooldownSeconds = 10;
        private DateTimeOffset _computedAt;
        
        public Hotp(string secret, HashAlgorithm algorithm, int digits) : base(secret, algorithm, digits)
        {
            
        }

        public string Compute(long counter)
        {
            _computedAt = DateTimeOffset.UtcNow;
            var counterBytes = ByteUtil.GetBigEndianBytes(counter);
            var material = base.Compute(counterBytes);
            return Truncate(material);
        }

        public DateTimeOffset GetRenewTime()
        {
            return _computedAt.AddSeconds(CooldownSeconds);
        }
    }
}