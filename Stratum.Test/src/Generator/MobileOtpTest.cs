// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Stratum.Core.Generator;
using Xunit;

namespace Stratum.Test.Generator
{
    public class MobileOtpTest
    {
        [Fact]
        public void Compute()
        {
            var motp = new MobileOtp("7ac61d4736f51a2b", "5555");
            Assert.Equal("c42a26", motp.Compute(0));
            Assert.Equal("4af383", motp.Compute(1_000_000_000));
        }
    }
}