using System;

namespace AuthenticatorPro.Shared.Data.Generator
{
    public interface IGenerator
    {
        public DateTimeOffset GetRenewTime();
    }
}