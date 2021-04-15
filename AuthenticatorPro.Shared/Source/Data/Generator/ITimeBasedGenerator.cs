namespace AuthenticatorPro.Shared.Data.Generator
{
    public interface ITimeBasedGenerator : IGenerator
    {
        public string Compute();
    }
}