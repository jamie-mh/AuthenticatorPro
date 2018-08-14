namespace Albireo.Otp
{
    using System;
    using System.Diagnostics.Contracts;

    /// <summary>Time-based one-time password algorithm implementation.</summary>
    public static class Totp
    {
        private const int DefaultPeriod = 30;

        /// <summary>Compute the one-time code for the given parameters.</summary>
        /// <param name="algorithm">The hashing algorithm for the HMAC computation.</param>
        /// <param name="secret">The ASCII-encoded base32-encoded shared secret.</param>
        /// <param name="datetime">The date with time for which the one-time code must be computed.</param>
        /// <param name="digits">The number of digits of the one-time codes.</param>
        /// <param name="period">The period step used for the HMAC counter computation.</param>
        /// <returns>The one-time code for the given date.</returns>
        public static int GetCode(
            HashAlgorithm algorithm,
            string secret,
            DateTime datetime,
            int digits = Otp.DefaultDigits,
            int period = Totp.DefaultPeriod)
        {
            datetime = datetime.Kind == DateTimeKind.Utc ? datetime : datetime.ToUniversalTime();

            var unixTime = datetime.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            var counter = (long) (unixTime * 1000) / (period * 1000);

            return Otp.GetCode(algorithm, secret, counter, digits);
        }

        /// <summary>Build a URI for secret key provisioning.</summary>
        /// <param name="algorithm">The hashing algorithm for the HMAC computation.</param>
        /// <param name="issuer">The name of the entity issuing and maintaining the key.</param>
        /// <param name="account">The account name for which the one-time codes will work.</param>
        /// <param name="secret">The ASCII-encoded base32-encoded shared secret.</param>
        /// <param name="period">The period step for the HMAC counter computation.</param>
        /// <param name="digits">The number of digits of the one-time codes.</param>
        /// <returns>The provisioning URI.</returns>
        public static string GetKeyUri(
            HashAlgorithm algorithm,
            string issuer,
            string account,
            byte[] secret,
            int digits = Otp.DefaultDigits,
            int period = Totp.DefaultPeriod)
        {
            return
                Otp.GetKeyUri(
                    OtpType.Totp,
                    issuer,
                    account,
                    secret,
                    algorithm,
                    digits,
                    0,
                    period);
        }
    }
}
