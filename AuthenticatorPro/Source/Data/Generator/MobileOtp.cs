using System;
using AuthenticatorPro.Util;

namespace AuthenticatorPro.Data.Generator
{
    public class MobileOtp : IGenerator
    {
        public const int SecretMinLength = 16;
        public const int PinLength = 4;
        
        private readonly string _secret;
        private readonly int _digits;
        private readonly int _period;

        private DateTime _computedAt;

        public GenerationMethod GenerationMethod => GenerationMethod.Time;
        
        public MobileOtp(string secret, int digits, int period)
        {
            _secret = secret;
            _digits = digits;
            _period = period;
        }

        public string Compute()
        {
            _computedAt = DateTime.UtcNow;
            var timestamp = ((DateTimeOffset) _computedAt).ToUnixTimeSeconds() / 10;
            var material = timestamp + _secret;
            return Hash.Md5(material).Truncate(_digits);
        }

        public DateTime GetRenewTime()
        {
            var secondsRemaining = _period - (int) ((DateTimeOffset) _computedAt).ToUnixTimeSeconds() % _period;
            return DateTime.UtcNow.AddSeconds(secondsRemaining);
        }
    }
}