using System;

namespace AuthenticatorPro.Shared.Query
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