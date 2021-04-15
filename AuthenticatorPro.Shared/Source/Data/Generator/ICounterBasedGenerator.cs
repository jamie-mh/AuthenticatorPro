namespace AuthenticatorPro.Shared.Data.Generator
{
    public interface ICounterBasedGenerator : IGenerator
    {
        public string Compute(long counter);
    }
}