using AuthenticatorPro.Data.Generator;

namespace AuthenticatorPro.Data
{
    public enum AuthenticatorType
    {
        Hotp = 1, Totp = 2, MobileOtp = 3
    }

    public static class AuthenticatorTypeSpecification
    {
        public static GenerationMethod GetGenerationMethod(this AuthenticatorType type)
        {
            return type switch
            {
                AuthenticatorType.Hotp => GenerationMethod.Counter,
                AuthenticatorType.Totp => GenerationMethod.Time,
                AuthenticatorType.MobileOtp => GenerationMethod.Time
            };
        }

        public static bool IsHmacBased(this AuthenticatorType type)
        {
            return type switch
            {
                AuthenticatorType.Hotp => true,
                AuthenticatorType.Totp => true,
                AuthenticatorType.MobileOtp => false
            };
        }
    }
}