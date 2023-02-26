// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Test.Entity.ClassData;
using System;
using Xunit;

namespace AuthenticatorPro.Test.Entity
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
    }
}