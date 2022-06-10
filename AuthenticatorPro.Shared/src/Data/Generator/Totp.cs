// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Util;

namespace AuthenticatorPro.Shared.Data.Generator
{
    public class Totp : HmacOtp, IGenerator
    {
        private readonly int _period;

        public Totp(string secret, int period, HashAlgorithm algorithm, int digits) : base(secret, algorithm, digits)
        {
            _period = period;
        }

        private byte[] GetCounterBytes(long counter)
        {
            var window = counter / _period;
            return ByteUtil.GetBigEndianBytes(window);
        }

        protected virtual string Finalise(int material)
        {
            return Truncate(material);
        }

        public string Compute(long counter)
        {
            var material = base.Compute(GetCounterBytes(counter));
            return Finalise(material);
        }
    }
}