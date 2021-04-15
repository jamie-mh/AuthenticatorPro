using System;

namespace AuthenticatorPro.Shared.Source.Data.Generator
{
    public interface IGenerator
    {
        public DateTimeOffset GetRenewTime();
    }
}