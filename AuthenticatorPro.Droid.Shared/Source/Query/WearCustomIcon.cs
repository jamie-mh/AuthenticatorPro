using AuthenticatorPro.Shared.Source.Data;
using Newtonsoft.Json;

namespace AuthenticatorPro.Droid.Shared.Query
{
    public class WearCustomIcon
    {
        public readonly string Id;
        
        [JsonConverter(typeof(ByteArrayConverter))]
        public readonly byte[] Data;
        

        public WearCustomIcon(string id, byte[] data)
        {
            Id = id;
            Data = data;
        }
    }
}