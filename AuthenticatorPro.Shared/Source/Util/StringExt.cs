using System;

namespace AuthenticatorPro.Shared.Source.Util
{
    public static class StringExt
    {
        public static string Truncate(this string value, int maxLength)
        {
            if(String.IsNullOrEmpty(value))
                return value;

            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
        
        public static byte[] ToHexBytes(this string data)
        {
            var len = data.Length;
            var output = new byte[len / 2];
            
            for(var i = 0; i < len; i += 2)
                output[i / 2] = Convert.ToByte(data.Substring(i, 2), 16);
            
            return output;
        }
    }
}