// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections;
using System.Collections.Generic;
using Stratum.Core;
using Stratum.Core.Entity;
using Stratum.Core.Generator;

namespace Stratum.Test.Entity.ClassData
{
    public class GetOtpAuthUriClassData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[]
            {
                new Authenticator
                {
                    Type = AuthenticatorType.Totp, Issuer = "issuer", Username = "username", Secret = "ABCDEFG"
                },
                "otpauth://totp/issuer%3Ausername?secret=ABCDEFG&issuer=issuer"
            }; // Standard uri
            yield return new object[]
            {
                new Authenticator
                {
                    Type = AuthenticatorType.Totp, Issuer = "issuer", Username = null, Secret = "ABCDEFG"
                },
                "otpauth://totp/issuer?secret=ABCDEFG&issuer=issuer"
            }; // No username 1/2
            yield return new object[]
            {
                new Authenticator
                {
                    Type = AuthenticatorType.Totp, Issuer = "issuer", Username = "", Secret = "ABCDEFG"
                },
                "otpauth://totp/issuer?secret=ABCDEFG&issuer=issuer"
            }; // No username 2/2
            yield return new object[]
            {
                new Authenticator
                {
                    Type = AuthenticatorType.Totp, Issuer = "Big Company", Username = null, Secret = "ABCDEFG"
                },
                "otpauth://totp/Big%20Company?secret=ABCDEFG&issuer=Big%20Company"
            }; // Issuer encoded
            yield return new object[]
            {
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Issuer = "issuer",
                    Username = "example@test",
                    Secret = "ABCDEFG"
                },
                "otpauth://totp/issuer%3Aexample%40test?secret=ABCDEFG&issuer=issuer"
            }; // Username encoded
            yield return new object[]
            {
                new Authenticator
                {
                    Type = AuthenticatorType.Hotp,
                    Issuer = "issuer",
                    Username = "username",
                    Secret = "ABCDEFG",
                    Counter = 10
                },
                "otpauth://hotp/issuer%3Ausername?secret=ABCDEFG&issuer=issuer&counter=10"
            }; // Hotp
            yield return new object[]
            {
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Issuer = "issuer",
                    Username = "username",
                    Secret = "ABCDEFG",
                    Digits = 7
                },
                "otpauth://totp/issuer%3Ausername?secret=ABCDEFG&issuer=issuer&digits=7"
            }; // Digits parameter
            yield return new object[]
            {
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Issuer = "issuer",
                    Username = "username",
                    Secret = "ABCDEFG",
                    Period = 60
                },
                "otpauth://totp/issuer%3Ausername?secret=ABCDEFG&issuer=issuer&period=60"
            }; // Period parameter
            yield return new object[]
            {
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Issuer = "issuer",
                    Username = "username",
                    Secret = "ABCDEFG",
                    Algorithm = HashAlgorithm.Sha512
                },
                "otpauth://totp/issuer%3Ausername?secret=ABCDEFG&issuer=issuer&algorithm=SHA512"
            }; // Algorithm parameter
            yield return new object[]
            {
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Issuer = "issuer",
                    Username = "username",
                    Secret = "ABCDEFG",
                    Algorithm = HashAlgorithm.Sha1
                },
                "otpauth://totp/issuer%3Ausername?secret=ABCDEFG&issuer=issuer"
            }; // Algorithm parameter default
            yield return new object[]
            {
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Issuer = "issuer",
                    Username = "username",
                    Secret = "ABCDEFG",
                    Digits = 7,
                    Period = 60,
                    Algorithm = HashAlgorithm.Sha256
                },
                "otpauth://totp/issuer%3Ausername?secret=ABCDEFG&issuer=issuer&algorithm=SHA256&digits=7&period=60"
            }; // All parameters
            yield return new object[]
            {
                new Authenticator
                {
                    Type = AuthenticatorType.SteamOtp,
                    Issuer = "Steam",
                    Username = "username",
                    Secret = "ABCDEFG",
                    Digits = 5
                },
                "otpauth://totp/Steam%3Ausername?secret=ABCDEFG&issuer=Steam"
            }; // Steam issuer
            yield return new object[]
            {
                new Authenticator
                {
                    Type = AuthenticatorType.SteamOtp,
                    Issuer = "issuer",
                    Username = "username",
                    Secret = "ABCDEFG",
                    Digits = 5
                },
                "otpauth://totp/issuer%3Ausername?secret=ABCDEFG&issuer=issuer&steam"
            }; // Steam parameter
            yield return new object[]
            {
                new Authenticator
                {
                    Type = AuthenticatorType.YandexOtp,
                    Issuer = "Yandex",
                    Username = "username",
                    Secret = "ORSXG5DJNZTXIZLTORUW4ZZRGI",
                    Pin = "1234",
                    Digits = 8
                },
                "otpauth://yaotp/Yandex%3Ausername?secret=ORSXG5DJNZTXIZLTORUW4ZZRGI&issuer=Yandex&pin_length=4"
            }; // Yandex
            yield return new object[]
            {
                new Authenticator
                {
                    Type = AuthenticatorType.MobileOtp,
                    Issuer = "motp",
                    Username = "username",
                    Secret = "30edcc8edae50a60",
                    Pin = "1234",
                    Digits = 6
                },
                "motp://motp:username?secret=30edcc8edae50a60"
            }; // mOTP
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}