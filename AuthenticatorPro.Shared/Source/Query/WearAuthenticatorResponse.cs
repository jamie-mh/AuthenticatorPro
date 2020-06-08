using AuthenticatorPro.Shared.Data;

namespace AuthenticatorPro.Shared.Query
{
    [Android.Runtime.Preserve(AllMembers = true)]
    public class WearAuthenticatorResponse
    {
        public readonly AuthenticatorType Type;
        public readonly string Icon;
        public readonly string Issuer;
        public readonly string Username;
        public readonly int Period;
        public readonly int Digits;


        public WearAuthenticatorResponse(AuthenticatorType type, string icon, string issuer, string username, int period, int digits)
        {
            Type = type;
            Icon = icon;
            Issuer = issuer;
            Username = username;
            Period = period;
            Digits = digits;
        }
    }
}