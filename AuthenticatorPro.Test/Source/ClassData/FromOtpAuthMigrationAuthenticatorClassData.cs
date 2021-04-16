using System.Collections;
using System.Collections.Generic;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Data.Generator;
using SimpleBase;

namespace AuthenticatorPro.Test.ClassData
{
    internal class FromOtpAuthMigrationAuthenticatorClassData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        { 
            // Standard Totp
            yield return new object[]
            {
                new OtpAuthMigration.Authenticator { Type = OtpAuthMigration.Type.Totp, Secret = Base32.Rfc4648.Decode("ABCDEFG").ToArray(), Issuer = "issuer", Username = "username", Algorithm = OtpAuthMigration.Algorithm.Sha1 }, 
                new Authenticator { Type = AuthenticatorType.Totp, Secret = "ABCDEFG", Issuer = "issuer", Username = "username", Algorithm = HashAlgorithm.Sha1 }
            };
            
            // Standard Hotp
            yield return new object[]
            {
                new OtpAuthMigration.Authenticator { Type = OtpAuthMigration.Type.Hotp, Secret = Base32.Rfc4648.Decode("ABCDEFG").ToArray(), Issuer = "issuer", Username = "username", Algorithm = OtpAuthMigration.Algorithm.Sha1, Counter = 10 }, 
                new Authenticator { Type = AuthenticatorType.Hotp, Secret = "ABCDEFG", Issuer = "issuer", Username = "username", Algorithm = HashAlgorithm.Sha1, Counter = 10 }
            };
            
            // No issuer
            yield return new object[]
            {
                new OtpAuthMigration.Authenticator { Type = OtpAuthMigration.Type.Totp, Secret = Base32.Rfc4648.Decode("ABCDEFG").ToArray(), Issuer = "", Username = "username", Algorithm = OtpAuthMigration.Algorithm.Sha1 }, 
                new Authenticator { Type = AuthenticatorType.Totp, Secret = "ABCDEFG", Issuer = "username", Username = null, Algorithm = HashAlgorithm.Sha1 }
            };
            
            // Username issuer pair
            yield return new object[]
            {
                new OtpAuthMigration.Authenticator { Type = OtpAuthMigration.Type.Totp, Secret = Base32.Rfc4648.Decode("ABCDEFG").ToArray(), Issuer = "issuer", Username = "issuer: username", Algorithm = OtpAuthMigration.Algorithm.Sha1 }, 
                new Authenticator { Type = AuthenticatorType.Totp, Secret = "ABCDEFG", Issuer = "issuer", Username = "username", Algorithm = HashAlgorithm.Sha1 }
            };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}