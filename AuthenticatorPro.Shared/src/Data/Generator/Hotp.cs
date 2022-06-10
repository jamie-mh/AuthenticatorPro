// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Util;

namespace AuthenticatorPro.Shared.Data.Generator
{
    public class Hotp : HmacOtp, IGenerator
    {
        public Hotp(string secret, HashAlgorithm algorithm, int digits) : base(secret, algorithm, digits) { }

        public string Compute(long counter)
        {
            var counterBytes = ByteUtil.GetBigEndianBytes(counter);
            var material = base.Compute(counterBytes);
            return Truncate(material);
        }
    }
}