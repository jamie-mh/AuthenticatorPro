using System.Threading.Tasks;

namespace AuthenticatorPro.Shared.Data
{
    public interface ICustomIconDecoder
    {
        public Task<CustomIcon> Decode(byte[] data);
    }
}