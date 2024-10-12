// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Stratum.Core;
using Stratum.Core.Entity;
using Stratum.Core.Generator;
using Stratum.Test.Entity.ClassData;
using Xunit;

namespace Stratum.Test.Entity
{
    public class AuthenticatorTest
    {
        [Theory]
        [ClassData(typeof(GetOtpAuthUriClassData))]
        public void GetOtpAuthUri(Authenticator auth, string uri)
        {
            Assert.Equal(uri, auth.GetUri());
        }

        [Theory]
        [ClassData(typeof(ValidateClassData))]
        public void Validate(Authenticator auth, bool isValid)
        {
            if (!isValid)
            {
                Assert.Throws<ArgumentException>(auth.Validate);
            }
        }

        [Theory]
        [InlineData(AuthenticatorType.Totp)]
        [InlineData(AuthenticatorType.MobileOtp)]
        [InlineData(AuthenticatorType.SteamOtp)]
        [InlineData(AuthenticatorType.YandexOtp)]
        public void GetCodeWithOffset(AuthenticatorType type)
        {
            var auth = new Authenticator
            {
                Type = type,
                Secret = "NBSWY3DPO5XXE3DEORSXG5BRGI",
                Pin = "1234",
                Algorithm = HashAlgorithm.Sha1,
                Digits = 6,
                Period = 30
            };

            IGenerator generator = type switch
            {
                AuthenticatorType.Totp => new Totp(auth.Secret, auth.Period, auth.Algorithm, auth.Digits),
                AuthenticatorType.MobileOtp => new MobileOtp(auth.Secret, auth.Pin),
                AuthenticatorType.SteamOtp => new SteamOtp(auth.Secret),
                AuthenticatorType.YandexOtp => new YandexOtp(auth.Secret, auth.Pin),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            var code = auth.GetCode(1000);
            Assert.Equal(generator.Compute(1000), code);
        }

        [Fact]
        public void GetCodeWithCounter()
        {
            var auth = new Authenticator
            {
                Type = AuthenticatorType.Hotp,
                Secret = "NBSWY3DPO5XXE3DEORSXG5BRGI",
                Pin = "1234",
                Algorithm = HashAlgorithm.Sha1,
                Digits = 6,
                Period = 30,
                Counter = 1000
            };

            var generator = new Hotp(auth.Secret, auth.Algorithm, auth.Digits);

            var codeA = auth.GetCode(1234);
            Assert.Equal(generator.Compute(1000), codeA);

            var codeB = auth.GetCode(4321);
            Assert.Equal(generator.Compute(1000), codeB);

            auth.Counter++;
            var codeC = auth.GetCode(1111);
            Assert.Equal(generator.Compute(1001), codeC);
        }
    }
}