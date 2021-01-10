using System;

namespace AuthenticatorPro.Shared.Data.Generator
{
    public interface IGenerator
    {
        public string Compute();
        public DateTime GetRenewTime();
    }
}