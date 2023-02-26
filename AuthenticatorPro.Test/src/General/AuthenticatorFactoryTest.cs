// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Test.General.ClassData;
using Moq;
using SimpleBase;
using System;
using System.Linq;
using Xunit;

namespace AuthenticatorPro.Test.General
{
    public class AuthenticatorFactoryTest
    {
        private readonly Mock<IIconResolver> _iconResolver;

        public AuthenticatorFactoryTest()
        {
            _iconResolver = new Mock<IIconResolver>();
            _iconResolver.Setup(r => r.FindServiceKeyByName(It.IsAny<string>())).Returns("default");
        }

        [Theory]
        [InlineData("definitely not valid")] // Simple fail
        [InlineData("otpauth://totp/?secret=ABCDEFG")] // No issuer username
        [InlineData("otpauth://totp")] // No parameters
        [InlineData("otpauth://fail")] // Invalid scheme
        [InlineData("otpauth://totp/test:username?issuer=test")] // No secret
        [InlineData("otpauth://test/issuer:username?secret=ABCDEFG")] // Invalid type
        [InlineData("otpauth://totp/issuer:username?secret=ABCDEFG&algorithm=something")] // Invalid algorithm
        [InlineData("otpauth://totp/issuer:username?secret=ABCDEFG&digits=0")] // Invalid digits 1/2
        [InlineData("otpauth://totp/issuer:username?secret=ABCDEFG&digits=test")] // Invalid digits 2/2
        [InlineData("otpauth://totp/issuer:username?secret=ABCDEFG&period=0")] // Invalid period 1/2
        [InlineData("otpauth://totp/issuer:username?secret=ABCDEFG&period=test")] // Invalid period 2/2
        [InlineData("otpauth://hotp/issuer:username?secret=ABCDEFG&counter=test")] // Invalid counter 1/2
        [InlineData("otpauth://hotp/issuer:username?secret=ABCDEFG&counter=-1")] // Invalid counter 2/2
        [InlineData("motp://fail")] // Invalid mOTP
        [InlineData("otpauth://yaotp/issuer:username?secret=ABCDEFG&pin_length=test")] // Invalid Yandex pin length
        public void FromInvalidOtpAuthUri(string uri)
        {
            Assert.Throws<ArgumentException>(delegate
            {
                _ = AuthenticatorFactory.ParseUri(uri, _iconResolver.Object);
            });
        }

        [Theory]
        [ClassData(typeof(FromValidOtpAuthUriClassData))]
        public void FromValidOtpAuthUri(string uri, Authenticator b, int pinLength)
        {
            var result = AuthenticatorFactory.ParseUri(uri, _iconResolver.Object);

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
        public void FromOtpAuthUriToOtpAuthUri(string uri)
        {
            var auth = AuthenticatorFactory.ParseUri(uri, _iconResolver.Object).Authenticator;
            Assert.Equal(uri, auth.GetUri());
        }

        [Theory]
        [ClassData(typeof(FromOtpAuthMigrationAuthenticatorClassData))]
        public void FromOtpAuthMigrationAuthenticator(OtpAuthMigration.Authenticator migration, Authenticator b)
        {
            var a = AuthenticatorFactory.FromOtpAuthMigrationAuthenticator(migration, _iconResolver.Object);

            Assert.Equal(b.Type, a.Type);
            Assert.Equal(b.Algorithm, a.Algorithm);
            Assert.Equal(b.Counter, a.Counter);
            Assert.Equal(b.Digits, a.Digits);
            Assert.Equal(b.Period, a.Period);
            Assert.Equal(b.Issuer, a.Issuer);
            Assert.Equal(b.Username, a.Username);
            Assert.True(Base32.Rfc4648.Decode(b.Secret).SequenceEqual(Base32.Rfc4648.Decode(a.Secret)));
        }
    }
}