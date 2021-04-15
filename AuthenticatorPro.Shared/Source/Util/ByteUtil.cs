using System;

namespace AuthenticatorPro.Shared.Util
{
    public static class ByteUtil
    {
        public static byte[] GetBigEndianBytes(long input)
        {
            var result = BitConverter.GetBytes(input);
            
            if(BitConverter.IsLittleEndian)
                Array.Reverse(result);

            return result;
        } 
    }
}