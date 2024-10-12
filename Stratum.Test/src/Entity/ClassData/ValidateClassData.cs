// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections;
using System.Collections.Generic;
using Stratum.Core;
using Stratum.Core.Entity;

namespace Stratum.Test.Entity.ClassData
{
    public class ValidateClassData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[]
            {
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Secret = "abcdefg",
                    Issuer = "test",
                    Digits = AuthenticatorType.Totp.GetDefaultDigits(),
                    Period = AuthenticatorType.Totp.GetDefaultPeriod()
                },
                true
            }; // Valid
            yield return new object[]
            {
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Secret = "abcdefg",
                    Issuer = null,
                    Digits = AuthenticatorType.Totp.GetDefaultDigits(),
                    Period = AuthenticatorType.Totp.GetDefaultPeriod()
                },
                false
            }; // Missing issuer 1/2
            yield return new object[]
            {
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Secret = "abcdefg",
                    Issuer = "",
                    Digits = AuthenticatorType.Totp.GetDefaultDigits(),
                    Period = AuthenticatorType.Totp.GetDefaultPeriod()
                },
                false
            }; // Missing issuer 2/2
            yield return new object[]
            {
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Secret = null,
                    Issuer = "test",
                    Digits = AuthenticatorType.Totp.GetDefaultDigits(),
                    Period = AuthenticatorType.Totp.GetDefaultPeriod()
                },
                false
            }; // Missing secret 1/2
            yield return new object[]
            {
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Secret = "",
                    Issuer = "test",
                    Digits = AuthenticatorType.Totp.GetDefaultDigits(),
                    Period = AuthenticatorType.Totp.GetDefaultPeriod()
                },
                false
            }; // Missing secret 2/2
            yield return new object[]
            {
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Secret = "11111111",
                    Issuer = "test",
                    Digits = AuthenticatorType.Totp.GetDefaultDigits(),
                    Period = AuthenticatorType.Totp.GetDefaultPeriod()
                },
                false
            }; // Invalid secret
            yield return new object[]
            {
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Secret = "abcdefg",
                    Issuer = "test",
                    Digits = AuthenticatorType.Totp.GetMinDigits() - 1,
                    Period = AuthenticatorType.Totp.GetDefaultPeriod()
                },
                false
            }; // Too few digits
            yield return new object[]
            {
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Secret = "abcdefg",
                    Issuer = "test",
                    Digits = AuthenticatorType.Totp.GetMaxDigits() + 1,
                    Period = AuthenticatorType.Totp.GetDefaultPeriod()
                },
                false
            }; // Too many digits
            yield return new object[]
            {
                new Authenticator
                {
                    Type = AuthenticatorType.Totp,
                    Secret = "abcdefg",
                    Issuer = "test",
                    Digits = AuthenticatorType.Totp.GetDefaultDigits(),
                    Period = -1
                },
                false
            }; // Negative period
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}