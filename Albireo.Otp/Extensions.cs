namespace Albireo.Otp
{
    using System;
    using System.Diagnostics.Contracts;

    internal static class Extensions
    {
        internal static string ToKeyUriValue(this OtpType type)
        {
            switch (type)
            {
                case OtpType.Totp:
                    return "totp";

                case OtpType.Hotp:
                    return "hotp";

                default:
                    throw new NotSupportedException();
            }
        }

        internal static string ToKeyUriValue(this HashAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case HashAlgorithm.Md5:
                    return "MD5";

                case HashAlgorithm.Sha1:
                    return "SHA1";

                case HashAlgorithm.Sha256:
                    return "SHA256";

                case HashAlgorithm.Sha512:
                    return "SHA512";

                default:
                    throw new NotSupportedException();
            }
        }

        internal static string ToAlgorithmName(this HashAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case HashAlgorithm.Md5:
                    return "System.Security.Cryptography.HMACMD5";

                case HashAlgorithm.Sha1:
                    return "System.Security.Cryptography.HMACSHA1";

                case HashAlgorithm.Sha256:
                    return "System.Security.Cryptography.HMACSHA256";

                case HashAlgorithm.Sha512:
                    return "System.Security.Cryptography.HMACSHA512";

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
