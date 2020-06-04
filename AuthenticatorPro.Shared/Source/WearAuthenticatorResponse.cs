namespace AuthenticatorPro.Shared
{
    public class WearAuthenticatorResponse
    {
        public readonly AuthenticatorType Type;
        public readonly string Icon;
        public readonly string Issuer;
        public readonly string Username;
        public readonly int Period;

        public WearAuthenticatorResponse(AuthenticatorType type, string icon, string issuer, string username, int period)
        {
            Type = type;
            Icon = icon;
            Issuer = issuer;
            Username = username;
            Period = period;
        }
    }
}