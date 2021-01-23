using System.Threading.Tasks;

namespace AuthenticatorPro.Shared.Source.Data
{
    public interface ICustomIconDecoder
    {
        public Task<CustomIcon> Decode(byte[] data);
    }
}