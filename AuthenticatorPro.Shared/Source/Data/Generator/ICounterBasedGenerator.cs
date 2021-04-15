namespace AuthenticatorPro.Shared.Source.Data.Generator
{
    public interface ICounterBasedGenerator : IGenerator
    {
        public string Compute(long counter);
    }
}