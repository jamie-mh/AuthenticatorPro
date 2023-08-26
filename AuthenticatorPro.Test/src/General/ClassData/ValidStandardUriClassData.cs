// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections;
using System.Collections.Generic;
using AuthenticatorPro.Core;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Generator;

namespace AuthenticatorPro.Test.General.ClassData
{
    public class ValidStandardUriClassData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[]
            {
                "otpauth://totp/issuer:username?secret=ABCDEFG",
                new Authenticator
                {
                    Type = AuthenticatorType.Totp, Issuer = "issuer", Username = "username", Secret = "ABCDEFG"
                },
                0
            }; // Username issuer pair
            yield return new object[]
            {
                "otpauth://totp/Big%20Company:username?secret=ABCDEFG",
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Issuer = "Big Company",
                    Username = "username",
                    Secret = "ABCDEFG"
                },
                0
            }; // Username issuer pair (encoded) 1/3
            yield return new object[]
            {
                "otpauth://totp/Big%20Company%3Ausername@test?secret=ABCDEFG",
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Issuer = "Big Company",
                    Username = "username@test",
                    Secret = "ABCDEFG"
                },
                0
            }; // Username issuer pair (encoded) 2/3
            yield return new object[]
            {
                "otpauth://totp/Big%20Company%3Ausername%40test?secret=ABCDEFG",
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Issuer = "Big Company",
                    Username = "username@test",
                    Secret = "ABCDEFG"
                },
                0
            }; // Username issuer pair (encoded) 3/3
            yield return new object[]
            {
                "otpauth://totp/issuer:username?secret=ABCDEFG&issuer=something",
                new Authenticator
                {
                    Type = AuthenticatorType.Totp, Issuer = "issuer", Username = "username", Secret = "ABCDEFG"
                },
                0
            }; // Redundant issuer parameter
            yield return new object[]
            {
                "otpauth://totp/username?secret=ABCDEFG&issuer=issuer",
                new Authenticator
                {
                    Type = AuthenticatorType.Totp, Issuer = "issuer", Username = "username", Secret = "ABCDEFG"
                },
                0
            }; // Issuer parameter
            yield return new object[]
            {
                "otpauth://totp/username?secret=ABCDEFG&issuer=Big%20Company",
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Issuer = "Big Company",
                    Username = "username",
                    Secret = "ABCDEFG"
                },
                0
            }; // Issuer parameter (encoded)
            yield return new object[]
            {
                "otpauth://totp/issuer?secret=ABCDEFG",
                new Authenticator
                {
                    Type = AuthenticatorType.Totp, Issuer = "issuer", Username = null, Secret = "ABCDEFG"
                },
                0
            }; // No username
            yield return new object[]
            {
                "otpauth://hotp/issuer?secret=ABCDEFG",
                new Authenticator
                {
                    Type = AuthenticatorType.Hotp,
                    Issuer = "issuer",
                    Username = null,
                    Secret = "ABCDEFG",
                    Counter = 0
                },
                0
            }; // HOTP
            yield return new object[]
            {
                "otpauth://hotp/issuer?secret=ABCDEFG&counter=10",
                new Authenticator
                {
                    Type = AuthenticatorType.Hotp,
                    Issuer = "issuer",
                    Username = null,
                    Secret = "ABCDEFG",
                    Counter = 10
                },
                0
            }; // HOTP with counter
            yield return new object[]
            {
                "otpauth://totp/issuer?secret=ABCDEFG&counter=10",
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Issuer = "issuer",
                    Username = null,
                    Secret = "ABCDEFG",
                    Counter = 0
                },
                0
            }; // TOTP with counter
            yield return new object[]
            {
                "otpauth://totp/issuer?secret=ABCDEFG&digits=7",
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Issuer = "issuer",
                    Username = null,
                    Secret = "ABCDEFG",
                    Digits = 7
                },
                0
            }; // Digits parameter
            yield return new object[]
            {
                "otpauth://totp/issuer?secret=ABCDEFG&period=60",
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Issuer = "issuer",
                    Username = null,
                    Secret = "ABCDEFG",
                    Period = 60
                },
                0
            }; // Period parameter
            yield return new object[]
            {
                "otpauth://totp/issuer?secret=ABCDEFG",
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Issuer = "issuer",
                    Username = null,
                    Secret = "ABCDEFG",
                    Algorithm = Authenticator.DefaultAlgorithm
                },
                0
            }; // Algorithm parameter 1/4
            yield return new object[]
            {
                "otpauth://totp/issuer?secret=ABCDEFG&algorithm=SHA1",
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Issuer = "issuer",
                    Username = null,
                    Secret = "ABCDEFG",
                    Algorithm = HashAlgorithm.Sha1
                },
                0
            }; // Algorithm parameter 2/4
            yield return new object[]
            {
                "otpauth://totp/issuer?secret=ABCDEFG&algorithm=SHA256",
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Issuer = "issuer",
                    Username = null,
                    Secret = "ABCDEFG",
                    Algorithm = HashAlgorithm.Sha256
                },
                0
            }; // Algorithm parameter 3/4
            yield return new object[]
            {
                "otpauth://totp/issuer?secret=ABCDEFG&algorithm=SHA512",
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Issuer = "issuer",
                    Username = null,
                    Secret = "ABCDEFG",
                    Algorithm = HashAlgorithm.Sha512
                },
                0
            }; // Algorithm parameter 4/4
            yield return new object[]
            {
                "otpauth://totp/issuer:username?secret=ABCDEFG&icon=myicon",
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Issuer = "issuer",
                    Username = "username",
                    Secret = "ABCDEFG",
                    Icon = "myicon"
                },
                0
            }; // Icon parameter
            yield return new object[]
            {
                $"otpauth://totp/{new string('a', Authenticator.IssuerMaxLength + 1)}?secret=ABCDEFG",
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Issuer = new string('a', Authenticator.IssuerMaxLength),
                    Username = null,
                    Secret = "ABCDEFG"
                },
                0
            }; // Truncate issuer
            yield return new object[]
            {
                $"otpauth://totp/issuer:{new string('a', Authenticator.UsernameMaxLength + 1)}?secret=ABCDEFG",
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Issuer = "issuer",
                    Username = new string('a', Authenticator.UsernameMaxLength),
                    Secret = "ABCDEFG"
                },
                0
            }; // Truncate username
            yield return new object[]
            {
                "otpauth://totp/:username?secret=ABCDEFG",
                new Authenticator
                {
                    Type = AuthenticatorType.Totp, Issuer = "username", Username = null, Secret = "ABCDEFG"
                },
                0
            }; // Blank issuer
            yield return new object[]
            {
                "otpauth://totp/%F0%9F%98%80%3Ausername?secret=ABCDEFG",
                new Authenticator
                {
                    Type = AuthenticatorType.Totp, Issuer = "😀", Username = "username", Secret = "ABCDEFG"
                },
                0
            }; // Multibyte characters 1/2
            yield return new object[]
            {
                "otpauth://totp/%E4%BD%A0%E5%A5%BD%E4%B8%96%E7%95%8C%3Ausername?secret=ABCDEFG",
                new Authenticator
                {
                    Type = AuthenticatorType.Totp, Issuer = "你好世界", Username = "username", Secret = "ABCDEFG"
                },
                0
            }; // Multibyte characters 2/2
            yield return new object[]
            {
                "otpauth://totp/Steam?secret=ABCDEFG",
                new Authenticator
                {
                    Type = AuthenticatorType.SteamOtp,
                    Digits = 5,
                    Issuer = "Steam",
                    Username = null,
                    Secret = "ABCDEFG"
                },
                0
            }; // Steam issuer no username
            yield return new object[]
            {
                "otpauth://totp/Steam:username?secret=ABCDEFG",
                new Authenticator
                {
                    Type = AuthenticatorType.SteamOtp,
                    Digits = 5,
                    Issuer = "Steam",
                    Username = "username",
                    Secret = "ABCDEFG"
                },
                0
            }; // Steam issuer and username
            yield return new object[]
            {
                "otpauth://totp/issuer:username?secret=ABCDEFG&steam",
                new Authenticator
                {
                    Type = AuthenticatorType.SteamOtp,
                    Digits = 5,
                    Issuer = "issuer",
                    Username = "username",
                    Secret = "ABCDEFG"
                },
                0
            }; // Steam parameter
            yield return new object[]
            {
                "otpauth://yaotp/username?secret=ORSXG5DJNZTXIZLTORUW4ZZRGI&pin_length=4",
                new Authenticator
                {
                    Type = AuthenticatorType.YandexOtp,
                    Digits = 8,
                    Issuer = "Yandex",
                    Username = "username",
                    Secret = "ORSXG5DJNZTXIZLTORUW4ZZRGI"
                },
                4
            }; // Yandex with pin length
            yield return new object[]
            {
                "motp://motp:username?secret=30edcc8edae50a60",
                new Authenticator
                {
                    Type = AuthenticatorType.MobileOtp,
                    Digits = 6,
                    Issuer = "motp",
                    Username = "username",
                    Secret = "30edcc8edae50a60"
                },
                4
            }; // mOTP
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}