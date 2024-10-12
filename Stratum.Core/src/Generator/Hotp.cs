// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Buffers.Binary;

namespace Stratum.Core.Generator
{
    public class Hotp : HmacOtp, IGenerator
    {
        public Hotp(string secret, HashAlgorithm algorithm, int digits) : base(secret, algorithm, digits)
        {
        }

        public string Compute(long counter)
        {
            var counterBytes = new byte[sizeof(long)];
            BinaryPrimitives.WriteInt64BigEndian(counterBytes, counter);
            var material = base.Compute(counterBytes);
            return Truncate(material);
        }
    }
}