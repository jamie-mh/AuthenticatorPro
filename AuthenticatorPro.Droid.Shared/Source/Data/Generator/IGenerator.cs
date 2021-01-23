using System;

namespace AuthenticatorPro.Droid.Shared.Data.Generator
{
    public interface IGenerator
    {
        public string Compute();
        public DateTime GetRenewTime();
    }
}