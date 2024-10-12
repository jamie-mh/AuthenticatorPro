// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

// Test data from KeePassXC
// Licensed under GPL 3.0
// https://github.com/keepassxreboot/keepassxc/blob/420c364bf7bb28268bdf423d22191653747a559d/tests/TestTotp.cpp

using Stratum.Core.Generator;
using Xunit;

namespace Stratum.Test.Generator
{
    public class SteamOtpTest
    {
        private const string Secret = "63BEDWCQZKTQWPESARIERL5DTTQFCJTK";

        [Theory]
        [InlineData(1511200518, "FR8RV")]
        [InlineData(1511200714, "9P3VP")]
        public void Compute(long offset, string expectedResult)
        {
            var steamOtp = new SteamOtp(Secret);
            Assert.Equal(expectedResult, steamOtp.Compute(offset));
        }
    }
}