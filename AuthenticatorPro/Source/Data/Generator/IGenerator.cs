using System;

namespace AuthenticatorPro.Data.Generator
{
    public interface IGenerator
    {
        public string Compute();
        public DateTime GetRenewTime();
    }
}