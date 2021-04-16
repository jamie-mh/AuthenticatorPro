using System;
using AuthenticatorPro.Shared.Data.Generator;
using Xunit;

namespace AuthenticatorPro.Test
{
    public class TotpTest
    {
        private const string Sha1Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";
        private const string Sha256Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZA====";
        private const string Sha512Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNA=";

        [Theory]
        [InlineData(59, "94287082", Algorithm.Sha1)]
        [InlineData(59, "46119246", Algorithm.Sha256)]               
        [InlineData(59, "90693936", Algorithm.Sha512)]          
        [InlineData(1111111109, "07081804", Algorithm.Sha1)]             
        [InlineData(1111111109, "68084774", Algorithm.Sha256)]             
        [InlineData(1111111109, "25091201", Algorithm.Sha512)]                
        [InlineData(1111111111, "14050471", Algorithm.Sha1)]             
        [InlineData(1111111111, "67062674", Algorithm.Sha256)]            
        [InlineData(1111111111, "99943326", Algorithm.Sha512)]             
        [InlineData(1234567890, "89005924", Algorithm.Sha1)]              
        [InlineData(1234567890, "91819424", Algorithm.Sha256)]              
        [InlineData(1234567890, "93441116", Algorithm.Sha512)]              
        [InlineData(2000000000, "69279037", Algorithm.Sha1)]                
        [InlineData(2000000000, "90698825", Algorithm.Sha256)]               
        [InlineData(2000000000, "38618901", Algorithm.Sha512)]                  
        [InlineData(20000000000, "65353130", Algorithm.Sha1)]                   
        [InlineData(20000000000, "77737706", Algorithm.Sha256)]
        [InlineData(20000000000, "47863826", Algorithm.Sha512)]
        public void ComputeTest(long time, string expectedResult, Algorithm algorithm)
        {
            var offset = DateTimeOffset.FromUnixTimeSeconds(time);

            var secret = algorithm switch
            {
                Algorithm.Sha1 => Sha1Secret,
                Algorithm.Sha256 => Sha256Secret,
                Algorithm.Sha512 => Sha512Secret
            };
            
            var totp = new Totp(secret, 30, algorithm, 8);
            Assert.Equal(totp.Compute(offset), expectedResult);
        }
    }
}