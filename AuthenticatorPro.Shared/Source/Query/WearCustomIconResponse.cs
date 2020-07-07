using AuthenticatorPro.Shared.Data;
using Newtonsoft.Json;

namespace AuthenticatorPro.Shared.Query
{
    [Android.Runtime.Preserve(AllMembers = true)]
    public class WearCustomIconResponse
    {
        public readonly string Id;
        
        [JsonConverter(typeof(ByteArrayConverter))]
        public readonly byte[] Data;
        

        public WearCustomIconResponse(string id, byte[] data)
        {
            Id = id;
            Data = data;
        }
    }
}