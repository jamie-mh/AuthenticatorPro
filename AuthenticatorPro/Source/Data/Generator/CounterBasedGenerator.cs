using System;

namespace AuthenticatorPro.Data.Generator
{
    public abstract class CounterBasedGenerator : IGenerator
    {
        public GenerationMethod GenerationMethod => GenerationMethod.Counter;
        public abstract long Counter { set; }
        public abstract string Compute();
        public abstract DateTime GetRenewTime();
    }
}