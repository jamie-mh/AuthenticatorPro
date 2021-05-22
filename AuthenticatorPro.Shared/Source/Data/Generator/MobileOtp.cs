// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Util;

namespace AuthenticatorPro.Shared.Data.Generator
{
    public class MobileOtp : IGenerator
    {
        public const int SecretMinLength = 16;
        public const int PinLength = 4;
        
        private readonly string _secret;
        private readonly int _digits;

        public MobileOtp(string secret, int digits)
        {
            _secret = secret;
            _digits = digits;
        }

        public string Compute(long counter)
        {
            var timestamp = counter / 10;
            var material = timestamp + _secret;
            return HashUtil.Md5(material).Truncate(_digits);
        }
    }
}