using AuthenticatorPro.Shared.Data;

namespace AuthenticatorPro.Test
{
    internal class MockIconResolver : IIconResolver
    {
        public string FindServiceKeyByName(string name)
        {
            return "default";
        }
    }
}