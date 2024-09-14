// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Text;
using Stratum.Core.Util;

namespace Stratum.Core.Generator
{
    public class MobileOtp : IGenerator
    {
        public const int SecretMinLength = 16;
        public const int PinLength = 4;
        public const int Digits = 6;

        private readonly string _secret;
        private readonly string _pin;

        public MobileOtp(string secret, string pin)
        {
            _secret = secret;
            _pin = pin;
        }

        public string Compute(long counter)
        {
            var timestamp = counter / 10;

            var builder = new StringBuilder();
            builder.Append(timestamp);
            builder.Append(_secret);
            builder.Append(_pin);

            return HashUtil.Md5(builder.ToString()).Truncate(Digits);
        }
    }
}