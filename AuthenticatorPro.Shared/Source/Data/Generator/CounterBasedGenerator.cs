using System;

namespace AuthenticatorPro.Shared.Source.Data.Generator
{
    public abstract class CounterBasedGenerator : IGenerator
    {
        public abstract long Counter { set; }
        public abstract string Compute();
        public abstract DateTime GetRenewTime();
    }
}