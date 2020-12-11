namespace AuthenticatorPro.Data.Generator
{
    public interface ICounterBasedGenerator : IGenerator
    {
        public long Counter { set; }
    }
}