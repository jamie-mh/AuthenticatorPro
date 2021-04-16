using System;
using AuthenticatorPro.Shared.Util;

namespace AuthenticatorPro.Shared.Data.Generator
{
    public class Totp : HmacOtp, ITimeBasedGenerator
    {
        private readonly int _period;
        private DateTimeOffset _computedAt;

        public Totp(string secret, int period, HashAlgorithm algorithm, int digits) : base(secret, algorithm, digits)
        {
            _period = period;
        }

        private byte[] GetCounter()
        {
            var window = _computedAt.ToUnixTimeSeconds() / _period;
            return ByteUtil.GetBigEndianBytes(window);
        }

        protected virtual string Finalise(int material)
        {
            return Truncate(material);
        }

        public string Compute()
        {
            return Compute(DateTimeOffset.UtcNow);
        }

        public string Compute(DateTimeOffset offset)
        {
            _computedAt = offset;
            var material = base.Compute(GetCounter());
            return Finalise(material);
        }

        public DateTimeOffset GetRenewTime()
        {
            var secondsRemaining = _period - (int) _computedAt.ToUnixTimeSeconds() % _period;
            return DateTimeOffset.UtcNow.AddSeconds(secondsRemaining);
        }
    }
}