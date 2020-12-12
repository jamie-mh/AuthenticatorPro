using System;

namespace AuthenticatorPro.Shared.Data.Generator
{
    public interface IGenerator
    {
        public GenerationMethod GenerationMethod { get; }
        public string Compute();
        public DateTime GetRenewTime();
    }
}