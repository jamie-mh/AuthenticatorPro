namespace AuthenticatorPro.Shared.Source.Data.Generator
{
    public interface ITimeBasedGenerator : IGenerator
    {
        public string Compute();
    }
}