using System;
using AuthenticatorPro.Shared.Data.Generator;

namespace AuthenticatorPro.Shared.Data
{
    public enum AuthenticatorType
    {
        Hotp = 1, Totp = 2, MobileOtp = 3, SteamOtp = 4
    }

    public static class AuthenticatorTypeSpecification
    {
        public static GenerationMethod GetGenerationMethod(this AuthenticatorType type)
        {
            return type switch
            {
                AuthenticatorType.Hotp => GenerationMethod.Counter,
                AuthenticatorType.Totp => GenerationMethod.Time,
                AuthenticatorType.MobileOtp => GenerationMethod.Time,
                AuthenticatorType.SteamOtp => GenerationMethod.Time,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        public static bool IsHmacBased(this AuthenticatorType type)
        {
            return type switch
            {
                AuthenticatorType.Hotp => true,
                AuthenticatorType.Totp => true,
                AuthenticatorType.MobileOtp => false,
                AuthenticatorType.SteamOtp => true,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }
    }
}