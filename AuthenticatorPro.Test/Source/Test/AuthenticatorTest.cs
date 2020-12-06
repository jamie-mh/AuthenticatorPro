using System;
using System.Linq;
using AuthenticatorPro.Data;
using NUnit.Framework;
using OtpNet;

namespace AuthenticatorPro.Test.Test
{
    [TestFixture]
    internal class AuthenticatorTest
    {
        private static readonly string[] FromInvalidOtpAuthUriTestCases =
        {
            "definitely not valid",
            "otpauth://totp/?secret=ABCDEFG", // No issuer username 
            "otpauth://totp", // No parameters
            "otpauth://totp?issuer=test", // No secret
            "otpauth://test/issuer:username?secret=ABCDEFG", // Invalid type
            "otpauth://totp/issuer:username?secret=ABCDEFG&algorithm=something", // Invalid algorithm
            "otpauth://totp/issuer:username?secret=ABCDEFG&digits=0", // Invalid digits 1/2
            "otpauth://totp/issuer:username?secret=ABCDEFG&digits=test", // Invalid digits 2/2
            "otpauth://totp/issuer:username?secret=ABCDEFG&period=0", // Invalid period 1/2
            "otpauth://totp/issuer:username?secret=ABCDEFG&period=test", // Invalid period 2/2
            "otpauth://hotp/issuer:username?secret=ABCDEFG&counter=test", // Invalid counter 1/2
            "otpauth://hotp/issuer:username?secret=ABCDEFG&counter=-1", // Invalid counter 2/2
        };
       
        [Test]
        [TestCaseSource(nameof(FromInvalidOtpAuthUriTestCases))]
        public void FromInvalidOtpAuthUriTest(string uri)
        {
            Assert.Throws(typeof(ArgumentException), delegate
            {
                var auth = Authenticator.FromOtpAuthUri(uri);
            });
        }
        
        private static readonly object[] FromValidOtpAuthUriTestCases =
        {
            new object[] { "otpauth://totp/issuer:username?secret=ABCDEFG", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = "username", Secret = "ABCDEFG" }}, // Username issuer pair
            new object[] { "otpauth://totp/Big%20Company:username?secret=ABCDEFG", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "Big Company", Username = "username", Secret = "ABCDEFG" }}, // Username issuer pair (encoded) 1/3
            new object[] { "otpauth://totp/Big%20Company%3Ausername@test?secret=ABCDEFG", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "Big Company", Username = "username@test", Secret = "ABCDEFG" }}, // Username issuer pair (encoded) 2/3
            new object[] { "otpauth://totp/Big%20Company%3Ausername%40test?secret=ABCDEFG", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "Big Company", Username = "username@test", Secret = "ABCDEFG" }}, // Username issuer pair (encoded) 3/3
            new object[] { "otpauth://totp/issuer:username?secret=ABCDEFG&issuer=something", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = "username", Secret = "ABCDEFG" }}, // Redundant issuer parameter
            new object[] { "otpauth://totp/username?secret=ABCDEFG&issuer=issuer", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = "username", Secret = "ABCDEFG" }}, // Issuer parameter
            new object[] { "otpauth://totp/username?secret=ABCDEFG&issuer=Big%20Company", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "Big Company", Username = "username", Secret = "ABCDEFG" }}, // Issuer parameter (encoded)
            new object[] { "otpauth://totp/issuer?secret=ABCDEFG", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = null, Secret = "ABCDEFG" }}, // No username
            new object[] { "otpauth://hotp/issuer?secret=ABCDEFG", new Authenticator { Type = AuthenticatorType.Hotp, Issuer = "issuer", Username = null, Secret = "ABCDEFG", Counter = 0}}, // HOTP
            new object[] { "otpauth://hotp/issuer?secret=ABCDEFG&counter=10", new Authenticator { Type = AuthenticatorType.Hotp, Issuer = "issuer", Username = null, Secret = "ABCDEFG", Counter = 10 }}, // HOTP with counter
            new object[] { "otpauth://totp/issuer?secret=ABCDEFG&counter=10", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = null, Secret = "ABCDEFG", Counter = 0 }}, // TOTP with counter
            new object[] { "otpauth://totp/issuer?secret=ABCDEFG&digits=7", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = null, Secret = "ABCDEFG", Digits = 7 }}, // Digits parameter 
            new object[] { "otpauth://totp/issuer?secret=ABCDEFG&period=60", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = null, Secret = "ABCDEFG", Period = 60 }}, // Period parameter
            new object[] { "otpauth://totp/issuer?secret=ABCDEFG", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = null, Secret = "ABCDEFG", Algorithm = Authenticator.DefaultAlgorithm }}, // Algorithm parameter 1/4
            new object[] { "otpauth://totp/issuer?secret=ABCDEFG&algorithm=SHA1", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = null, Secret = "ABCDEFG", Algorithm = OtpHashMode.Sha1 }}, // Algorithm parameter 2/4
            new object[] { "otpauth://totp/issuer?secret=ABCDEFG&algorithm=SHA256", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = null, Secret = "ABCDEFG", Algorithm = OtpHashMode.Sha256 }}, // Algorithm parameter 3/4
            new object[] { "otpauth://totp/issuer?secret=ABCDEFG&algorithm=SHA512", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = null, Secret = "ABCDEFG", Algorithm = OtpHashMode.Sha512 }}, // Algorithm parameter 4/4
            new object[] { $"otpauth://totp/{new string('a', Authenticator.IssuerMaxLength + 1)}?secret=ABCDEFG", new Authenticator { Type = AuthenticatorType.Totp, Issuer = new string('a', Authenticator.IssuerMaxLength), Username = null, Secret = "ABCDEFG" }}, // Truncate issuer
            new object[] { $"otpauth://totp/issuer:{new string('a', Authenticator.UsernameMaxLength + 1)}?secret=ABCDEFG", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = new string('a', Authenticator.UsernameMaxLength), Secret = "ABCDEFG" }}, // Truncate username
            new object[] { "otpauth://totp/%F0%9F%98%80%3Ausername?secret=ABCDEFG", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "😀", Username = "username", Secret = "ABCDEFG" }}, // Multibyte characters 1/2
            new object[] { "otpauth://totp/%E4%BD%A0%E5%A5%BD%E4%B8%96%E7%95%8C%3Ausername?secret=ABCDEFG", new Authenticator { Type = AuthenticatorType.Totp, Issuer = "你好世界", Username = "username", Secret = "ABCDEFG" }} // Multibyte characters 2/2
        };
       
        [Test]
        [TestCaseSource(nameof(FromValidOtpAuthUriTestCases))]
        public void FromValidOtpAuthUriTest(string uri, Authenticator b)
        {
            var a = Authenticator.FromOtpAuthUri(uri);
            
            Assert.That(a.Type == b.Type);
            Assert.That(a.Algorithm == b.Algorithm);
            Assert.That(a.Counter == b.Counter);
            Assert.That(a.Digits == b.Digits);
            Assert.That(a.Period == b.Period);
            Assert.That(a.Issuer == b.Issuer);
            Assert.That(a.Username == b.Username);
            Assert.That(Base32Encoding.ToBytes(a.Secret).SequenceEqual(Base32Encoding.ToBytes(b.Secret)));
        }

        private static readonly string[] FromOtpAuthUriToOtpAuthUriTestCases =
        {
            "otpauth://totp/issuer%3Ausername?secret=ABCDEFG&issuer=issuer", // Standard TOTP
            "otpauth://totp/Big%20Company%3AMy%20User?secret=ABCDEFG&issuer=Big%20Company", // Encoded username issuer pair 
            "otpauth://hotp/issuer%3Ausername?secret=ABCDEFG&issuer=issuer&counter=10", // Standard HOTP
            "otpauth://totp/issuer%3Ausername?secret=ABCDEFG&issuer=issuer&digits=7", // Digits parameter
            "otpauth://totp/issuer%3Ausername?secret=ABCDEFG&issuer=issuer&period=60", // Period parameter
            "otpauth://totp/issuer%3Ausername?secret=ABCDEFG&issuer=issuer&algorithm=SHA512" // Algorithm parameter
        };

        [Test]
        [TestCaseSource(nameof(FromOtpAuthUriToOtpAuthUriTestCases))]
        public void FromOtpAuthUriToOtpAuthUriTest(string uri)
        {
            var auth = Authenticator.FromOtpAuthUri(uri);
            Assert.That(auth.GetOtpAuthUri() == uri);
        }

        private static readonly object[] FromOtpAuthMigrationAuthenticatorTestCases =
        {
            new object[]
            {
                new OtpAuthMigration.Authenticator { Type = OtpAuthMigration.Type.Totp, Secret = Base32Encoding.ToBytes("ABCDEFG"), Issuer = "issuer", Username = "username", Algorithm = OtpAuthMigration.Algorithm.Sha1 }, 
                new Authenticator { Type = AuthenticatorType.Totp, Secret = "ABCDEFG", Issuer = "issuer", Username = "username", Algorithm = OtpHashMode.Sha1 }
            }, // Standard Totp
            new object[]
            {
                new OtpAuthMigration.Authenticator { Type = OtpAuthMigration.Type.Hotp, Secret = Base32Encoding.ToBytes("ABCDEFG"), Issuer = "issuer", Username = "username", Algorithm = OtpAuthMigration.Algorithm.Sha1, Counter = 10 }, 
                new Authenticator { Type = AuthenticatorType.Hotp, Secret = "ABCDEFG", Issuer = "issuer", Username = "username", Algorithm = OtpHashMode.Sha1, Counter = 10 }
            }, // Standard Hotp
            new object[]
            {
                new OtpAuthMigration.Authenticator { Type = OtpAuthMigration.Type.Totp, Secret = Base32Encoding.ToBytes("ABCDEFG"), Issuer = "", Username = "username", Algorithm = OtpAuthMigration.Algorithm.Sha1 }, 
                new Authenticator { Type = AuthenticatorType.Totp, Secret = "ABCDEFG", Issuer = "username", Username = null, Algorithm = OtpHashMode.Sha1 }
            }, // No issuer
            new object[]
            {
                new OtpAuthMigration.Authenticator { Type = OtpAuthMigration.Type.Totp, Secret = Base32Encoding.ToBytes("ABCDEFG"), Issuer = "issuer", Username = "issuer: username", Algorithm = OtpAuthMigration.Algorithm.Sha1 }, 
                new Authenticator { Type = AuthenticatorType.Totp, Secret = "ABCDEFG", Issuer = "issuer", Username = "username", Algorithm = OtpHashMode.Sha1 }
            }, // Username issuer pair
        };
        
        [Test]
        [TestCaseSource(nameof(FromOtpAuthMigrationAuthenticatorTestCases))]
        public void FromOtpAuthMigrationAuthenticatorTest(OtpAuthMigration.Authenticator migration, Authenticator b)
        {
            var a = Authenticator.FromOtpAuthMigrationAuthenticator(migration);
            
            Assert.That(a.Type == b.Type);
            Assert.That(a.Algorithm == b.Algorithm);
            Assert.That(a.Counter == b.Counter);
            Assert.That(a.Digits == b.Digits);
            Assert.That(a.Period == b.Period);
            Assert.That(a.Issuer == b.Issuer);
            Assert.That(a.Username == b.Username);
            Assert.That(Base32Encoding.ToBytes(a.Secret).SequenceEqual(Base32Encoding.ToBytes(b.Secret)));
        }

        private static readonly object[] GetOtpAuthUriTestCases =
        {
            new object[] { new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = "username", Secret = "ABCDEFG" }, "otpauth://totp/issuer%3Ausername?secret=ABCDEFG&issuer=issuer" }, // Standard uri
            new object[] { new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = null, Secret = "ABCDEFG" }, "otpauth://totp/issuer?secret=ABCDEFG&issuer=issuer" }, // No username 1/2
            new object[] { new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = "", Secret = "ABCDEFG" }, "otpauth://totp/issuer?secret=ABCDEFG&issuer=issuer" }, // No username 2/2
            new object[] { new Authenticator { Type = AuthenticatorType.Totp, Issuer = "Big Company", Username = null, Secret = "ABCDEFG" }, "otpauth://totp/Big%20Company?secret=ABCDEFG&issuer=Big%20Company" }, // Issuer encoded
            new object[] { new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = "example@test", Secret = "ABCDEFG" }, "otpauth://totp/issuer%3Aexample%40test?secret=ABCDEFG&issuer=issuer" }, // Username encoded
            new object[] { new Authenticator { Type = AuthenticatorType.Hotp, Issuer = "issuer", Username = "username", Secret = "ABCDEFG", Counter = 10 }, "otpauth://hotp/issuer%3Ausername?secret=ABCDEFG&issuer=issuer&counter=10" }, // Hotp
            new object[] { new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = "username", Secret = "ABCDEFG", Digits = 7 }, "otpauth://totp/issuer%3Ausername?secret=ABCDEFG&issuer=issuer&digits=7" }, // Digits parameter
            new object[] { new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = "username", Secret = "ABCDEFG", Period = 60 }, "otpauth://totp/issuer%3Ausername?secret=ABCDEFG&issuer=issuer&period=60" }, // Period parameter
            new object[] { new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = "username", Secret = "ABCDEFG", Algorithm = OtpHashMode.Sha512 }, "otpauth://totp/issuer%3Ausername?secret=ABCDEFG&issuer=issuer&algorithm=SHA512" }, // Algorithm parameter
            new object[] { new Authenticator { Type = AuthenticatorType.Totp, Issuer = "issuer", Username = "username", Secret = "ABCDEFG", Digits = 7, Period = 60, Algorithm = OtpHashMode.Sha512 }, "otpauth://totp/issuer%3Ausername?secret=ABCDEFG&issuer=issuer&algorithm=SHA512&digits=7&period=60" }, // All parameters
        };

        [Test]
        [TestCaseSource(nameof(GetOtpAuthUriTestCases))]
        public void GetOtpAuthUriTest(Authenticator auth, string uri)
        {
            Assert.That(auth.GetOtpAuthUri() == uri); 
        }
        
        private static readonly object[] CleanSecretTestCases =
        {
            new object[] { "abcdefg", "ABCDEFG" }, // Make uppercase
            new object[] { "ABCD EFG", "ABCDEFG" }, // Remove spaces
            new object[] { "ABCD-EFG", "ABCDEFG" } // Remove hyphens 
        };
       
        [Test]
        [TestCaseSource(nameof(CleanSecretTestCases))]
        public void CleanSecretTest(string input, string output)
        {
            Assert.That(Authenticator.CleanSecret(input) == output);
        }
        
        private static readonly object[] IsValidSecretTestCases =
        {
            new object[] { "ABCDEFG", true }, // Valid (uppercase)
            new object[] { "abcdefg", true }, // Valid (lowercase)
            new object[] { "abcdefg==", true }, // Valid (padding)
            new object[] { null, false }, // Missing 1/2 
            new object[] { "", false }, // Missing 2/2
            new object[] { "a", false } // Too few bytes
        };
        
        [Test]
        [TestCaseSource(nameof(IsValidSecretTestCases))]
        public void IsValidSecretTest(string secret, bool expectedResult)
        {
            Assert.That(Authenticator.IsValidSecret(secret) == expectedResult); 
        }
        
        private static readonly object[] IsValidTestCases =
        {
            new object[] { new Authenticator { Secret = "abcdefg", Issuer = "test", Digits = Authenticator.DefaultDigits, Period = Authenticator.DefaultPeriod }, true }, // Valid
            new object[] { new Authenticator { Secret = "abcdefg", Issuer = null, Digits = Authenticator.DefaultDigits, Period = Authenticator.DefaultPeriod }, false }, // Missing issuer 1/2
            new object[] { new Authenticator { Secret = "abcdefg", Issuer = "", Digits = Authenticator.DefaultDigits, Period = Authenticator.DefaultPeriod }, false }, // Missing issuer 2/2
            new object[] { new Authenticator { Secret = null, Issuer = "test", Digits = Authenticator.DefaultDigits, Period = Authenticator.DefaultPeriod }, false }, // Missing secret 1/2
            new object[] { new Authenticator { Secret = "", Issuer = "test", Digits = Authenticator.DefaultDigits, Period = Authenticator.DefaultPeriod }, false }, // Missing secret 2/2
            new object[] { new Authenticator { Secret = "11111111", Issuer = "test", Digits = Authenticator.DefaultDigits, Period = Authenticator.DefaultPeriod }, false }, // Invalid secret 
            new object[] { new Authenticator { Secret = "abcdefg", Issuer = "test", Digits = Authenticator.MinDigits - 1, Period = Authenticator.DefaultPeriod }, false }, // Too few digits
            new object[] { new Authenticator { Secret = "abcdefg", Issuer = "test", Digits = Authenticator.MaxDigits + 1, Period = Authenticator.DefaultPeriod }, false }, // Too many digits
            new object[] { new Authenticator { Secret = "abcdefg", Issuer = "test", Digits = Authenticator.DefaultDigits, Period = -1 }, false } // Negative period
        };

        [Test]
        [TestCaseSource(nameof(IsValidTestCases))]
        public void IsValidTest(Authenticator auth, bool expectedResult)
        {
            Assert.That(auth.IsValid() == expectedResult);
        }
    }
}