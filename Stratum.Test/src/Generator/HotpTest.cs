// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Stratum.Core.Generator;
using Xunit;

namespace Stratum.Test.Generator
{
    public class HotpTest
    {
        private readonly Hotp _computeTestHotp;

        public HotpTest()
        {
            _computeTestHotp = new Hotp("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ", HashAlgorithm.Sha1, 6);
        }

        [Theory]
        [InlineData(0, "755224")]
        [InlineData(1, "287082")]
        [InlineData(2, "359152")]
        [InlineData(3, "969429")]
        [InlineData(4, "338314")]
        [InlineData(5, "254676")]
        [InlineData(6, "287922")]
        [InlineData(7, "162583")]
        [InlineData(8, "399871")]
        [InlineData(9, "520489")]
        public void Compute(long counter, string expected)
        {
            Assert.Equal(expected, _computeTestHotp.Compute(counter));
        }
    }
}