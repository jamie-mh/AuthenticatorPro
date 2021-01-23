using System;

namespace AuthenticatorPro.Shared.Source.Data.Generator
{
    public interface IGenerator
    {
        public string Compute();
        public DateTime GetRenewTime();
    }
}