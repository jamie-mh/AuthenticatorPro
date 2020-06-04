using System;

namespace AuthenticatorPro.Shared
{
    public class WearAuthenticatorCodeResponse
    {
        public readonly string Code;
        public readonly DateTime TimeRenew;

        public WearAuthenticatorCodeResponse(string code, DateTime timeRenew)
        {
            Code = code;
            TimeRenew = timeRenew;
        }
    }
}