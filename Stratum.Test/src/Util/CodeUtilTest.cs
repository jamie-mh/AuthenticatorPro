// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Stratum.Core.Util;
using Xunit;

namespace Stratum.Test.Util
{
    public class CodeUtilTest
    {
        [Theory]
        [InlineData(null, "––– –––", 6, 3)]
        [InlineData("123456", "123456", 6, 0)]
        [InlineData("123456", "123456", 6, -1)]
        [InlineData("123456", "123456", 0, 0)]
        [InlineData("123456", "123 456", 6, 3)]
        [InlineData("123456789", "123 456 789", 9, 3)]
        [InlineData("123456", "12 34 56", 6, 2)]
        [InlineData("123456", "1234 56", 6, 4)]
        public void PadCode(string input, string expected, int digits, int groupSize)
        {
            var padded = CodeUtil.PadCode(input, digits, groupSize);
            Assert.Equal(expected, padded);
        }
    }
}