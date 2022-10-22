// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Entity;
using AuthenticatorPro.Test.AuthenticatorTest.ClassData;
using SimpleBase;
using System;
using Xunit;

namespace AuthenticatorPro.Test.AuthenticatorTest
{
    public class AuthenticatorTest
    {
        private readonly IIconResolver _iconResolver;

        public AuthenticatorTest(IIconResolver iconResolver)
        {
            _iconResolver = iconResolver;
        }

        [Theory]
        [InlineData("definitely not valid")] // Simple fail
        [InlineData("otpauth://totp/?secret=ABCDEFG")] // No issuer username
        [InlineData("otpauth://totp")] // No parameters
        [InlineData("otpauth://fail")] // Invalid scheme
        [InlineData("otpauth://totp?issuer=test")] // No secret
        [InlineData("otpauth://test/issuer:username?secret=ABCDEFG")] // Invalid type
        [InlineData("otpauth://totp/issuer:username?secret=ABCDEFG&algorithm=something")] // Invalid algorithm
        [InlineData("otpauth://totp/issuer:username?secret=ABCDEFG&digits=0")] // Invalid digits 1/2
        [InlineData("otpauth://totp/issuer:username?secret=ABCDEFG&digits=test")] // Invalid digits 2/2
        [InlineData("otpauth://totp/issuer:username?secret=ABCDEFG&period=0")] // Invalid period 1/2
        [InlineData("otpauth://totp/issuer:username?secret=ABCDEFG&period=test")] // Invalid period 2/2
        [InlineData("otpauth://hotp/issuer:username?secret=ABCDEFG&counter=test")] // Invalid counter 1/2
        [InlineData("otpauth://hotp/issuer:username?secret=ABCDEFG&counter=-1")] // Invalid counter 2/2
        public void FromInvalidOtpAuthUriTest(string uri)
        {
            Assert.Throws<ArgumentException>(delegate
            {
                _ = Authenticator.ParseUri(uri, _iconResolver);
            });
        }

        [Theory]
        [ClassData(typeof(FromValidOtpAuthUriClassData))]
        public void FromValidOtpAuthUriTest(string uri, Authenticator b, int pinLength)
        {
            var result = Authenticator.ParseUri(uri, _iconResolver);

            var a = result.Authenticator;
            Assert.Equal(b.Type, a.Type);
            Assert.Equal(b.Algorithm, a.Algorithm);
            Assert.Equal(b.Counter, a.Counter);
            Assert.Equal(b.Digits, a.Digits);
            Assert.Equal(b.Period, a.Period);
            Assert.Equal(b.Issuer, a.Issuer);
            Assert.Equal(b.Username, a.Username);
            Assert.Equal(b.Secret, a.Secret);

            Assert.Equal(pinLength, result.PinLength);
        }

        [Theory]
        [InlineData("otpauth://totp/issuer%3Ausername?secret=ABCDEFG&issuer=issuer")] // Standard TOTP
        [InlineData(
            "otpauth://totp/Big%20Company%3AMy%20User?secret=ABCDEFG&issuer=Big%20Company")] // Encoded username issuer pair
        [InlineData("otpauth://hotp/issuer%3Ausername?secret=ABCDEFG&issuer=issuer&counter=10")] // Standard HOTP
        [InlineData("otpauth://totp/issuer%3Ausername?secret=ABCDEFG&issuer=issuer&digits=7")] // Digits parameter
        [InlineData("otpauth://totp/issuer%3Ausername?secret=ABCDEFG&issuer=issuer&period=60")] // Period parameter
        [InlineData(
            "otpauth://totp/issuer%3Ausername?secret=ABCDEFG&issuer=issuer&algorithm=SHA512")] // Algorithm parameter
        public void FromOtpAuthUriToOtpAuthUriTest(string uri)
        {
            var auth = Authenticator.ParseUri(uri, _iconResolver).Authenticator;
            Assert.Equal(uri, auth.GetUri());
        }

        [Theory]
        [ClassData(typeof(FromOtpAuthMigrationAuthenticatorClassData))]
        public void FromOtpAuthMigrationAuthenticatorTest(OtpAuthMigration.Authenticator migration, Authenticator b)
        {
            var a = Authenticator.FromOtpAuthMigrationAuthenticator(migration, _iconResolver);

            Assert.Equal(b.Type, a.Type);
            Assert.Equal(b.Algorithm, a.Algorithm);
            Assert.Equal(b.Counter, a.Counter);
            Assert.Equal(b.Digits, a.Digits);
            Assert.Equal(b.Period, a.Period);
            Assert.Equal(b.Issuer, a.Issuer);
            Assert.Equal(b.Username, a.Username);
            Assert.True(Base32.Rfc4648.Decode(b.Secret).SequenceEqual(Base32.Rfc4648.Decode(a.Secret)));
        }

        [Theory]
        [ClassData(typeof(GetOtpAuthUriClassData))]
        public void GetOtpAuthUriTest(Authenticator auth, string uri)
        {
            Assert.Equal(uri, auth.GetUri());
        }

        [Theory]
        [InlineData("abcdefg", "ABCDEFG", AuthenticatorType.Totp, AuthenticatorType.Hotp,
            AuthenticatorType.SteamOtp)] // Make uppercase
        [InlineData("ABCD EFG", "ABCDEFG", AuthenticatorType.Totp, AuthenticatorType.Hotp, AuthenticatorType.SteamOtp,
            AuthenticatorType.MobileOtp)] // Remove spaces
        [InlineData("ABCD-EFG", "ABCDEFG", AuthenticatorType.Totp, AuthenticatorType.Hotp, AuthenticatorType.SteamOtp,
            AuthenticatorType.MobileOtp)] // Remove hyphens
        [InlineData("abcdefg", "abcdefg", AuthenticatorType.MobileOtp)] // Preserve case 1/2
        [InlineData("ABCDEFG", "ABCDEFG", AuthenticatorType.MobileOtp)] // Preserve case 2/2
        public void CleanSecretTest(string input, string output, params AuthenticatorType[] types)
        {
            foreach (var type in types)
            {
                Assert.Equal(output, Authenticator.CleanSecret(input, type));
            }
        }

        [Theory]
        [InlineData(null, false, AuthenticatorType.Totp, AuthenticatorType.Hotp, AuthenticatorType.SteamOtp,
            AuthenticatorType.MobileOtp)] // Missing 1/2
        [InlineData("", false, AuthenticatorType.Totp, AuthenticatorType.Hotp, AuthenticatorType.SteamOtp,
            AuthenticatorType.MobileOtp)] // Missing 2/2
        [InlineData("ABCDEFG", true, AuthenticatorType.Totp, AuthenticatorType.Hotp,
            AuthenticatorType.SteamOtp)] // Valid base32 (uppercase)
        [InlineData("abcdefg", true, AuthenticatorType.Totp, AuthenticatorType.Hotp,
            AuthenticatorType.SteamOtp)] // Valid base32 (lowercase)
        [InlineData("abcdefg==", true, AuthenticatorType.Totp, AuthenticatorType.Hotp,
            AuthenticatorType.SteamOtp)] // Valid base32 (padding)
        [InlineData("a", false, AuthenticatorType.Totp, AuthenticatorType.Hotp,
            AuthenticatorType.SteamOtp)] // Too few bytes base32
        [InlineData("AAAAAAAAAAAAAAAA", true, AuthenticatorType.MobileOtp)] // Valid (uppercase)
        [InlineData("aaaaaaaaaaaaaaaa", true, AuthenticatorType.MobileOtp)] // Valid (lowercase)
        [InlineData("aaaaaaaaaaaaaaa", false, AuthenticatorType.MobileOtp)] // Too few characters
        public void IsValidSecretTest(string secret, bool expectedResult, params AuthenticatorType[] types)
        {
            foreach (var type in types)
            {
                Assert.Equal(expectedResult, Authenticator.IsValidSecret(secret, type));
            }
        }

        [Theory]
        [ClassData(typeof(IsValidClassData))]
        public void IsValidTest(Authenticator auth, bool expectedResult)
        {
            Assert.Equal(expectedResult, auth.IsValid());
        }
    }
}