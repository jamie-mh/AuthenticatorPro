namespace AuthenticatorPro.Shared.Data.Generator
{
    public interface IGenerator
    {
        public string Compute(long counter);
    }
}