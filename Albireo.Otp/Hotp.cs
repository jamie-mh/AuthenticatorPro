namespace Albireo.Otp
{
    using System;
    using System.Diagnostics.Contracts;

    /// <summary>HMAC-based one-time password algorithm implementation.</summary>
    public static class Hotp
    {
        /// <summary>Compute the one-time code for the given parameters.</summary>
        /// <param name="algorithm">The hashing algorithm for the HMAC computation.</param>
        /// <param name="secret">The ASCII-encoded base32-encoded shared secret.</param>
        /// <param name="counter">The counter for which the one-time code must be computed.</param>
        /// <param name="digits">The number of digits of the one-time code.</param>
        /// <returns>The one-time code for the given counter.</returns>
        public static int GetCode(
            HashAlgorithm algorithm,
            string secret,
            long counter,
            int digits = Otp.DefaultDigits)
        {
            return Otp.GetCode(algorithm, secret, counter, digits);
        }

        /// <summary>Build a URI for secret key provisioning.</summary>
        /// <param name="algorithm">The hashing algorithm for the HMAC computation.</param>
        /// <param name="issuer">The name of the entity issuing and maintaining the key.</param>
        /// <param name="account">The account name for which the one-time codes will work.</param>
        /// <param name="secret">The ASCII-encoded base32-encoded shared secret.</param>
        /// <param name="counter">The initial value for the counter.</param>
        /// <param name="digits">The number of digits of the one-time codes.</param>
        /// <returns>The provisioning URI.</returns>
        public static string GetKeyUri(
            HashAlgorithm algorithm,
            string issuer,
            string account,
            byte[] secret,
            long counter,
            int digits = Otp.DefaultDigits)
        {
            return
                Otp.GetKeyUri(
                    OtpType.Hotp,
                    issuer,
                    account,
                    secret,
                    algorithm,
                    digits,
                    counter,
                    0);
        }
    }
}
