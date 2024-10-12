// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Stratum.Core.Generator;
using Xunit;

namespace Stratum.Test.Generator
{
    public class TotpTest
    {
        private const string Sha1Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";
        private const string Sha256Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZA====";

        private const string Sha512Secret =
            "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNA=";

        [Theory]
        [InlineData(59, "94287082", HashAlgorithm.Sha1)]
        [InlineData(59, "46119246", HashAlgorithm.Sha256)]
        [InlineData(59, "90693936", HashAlgorithm.Sha512)]
        [InlineData(1111111109, "07081804", HashAlgorithm.Sha1)]
        [InlineData(1111111109, "68084774", HashAlgorithm.Sha256)]
        [InlineData(1111111109, "25091201", HashAlgorithm.Sha512)]
        [InlineData(1111111111, "14050471", HashAlgorithm.Sha1)]
        [InlineData(1111111111, "67062674", HashAlgorithm.Sha256)]
        [InlineData(1111111111, "99943326", HashAlgorithm.Sha512)]
        [InlineData(1234567890, "89005924", HashAlgorithm.Sha1)]
        [InlineData(1234567890, "91819424", HashAlgorithm.Sha256)]
        [InlineData(1234567890, "93441116", HashAlgorithm.Sha512)]
        [InlineData(2000000000, "69279037", HashAlgorithm.Sha1)]
        [InlineData(2000000000, "90698825", HashAlgorithm.Sha256)]
        [InlineData(2000000000, "38618901", HashAlgorithm.Sha512)]
        [InlineData(20000000000, "65353130", HashAlgorithm.Sha1)]
        [InlineData(20000000000, "77737706", HashAlgorithm.Sha256)]
        [InlineData(20000000000, "47863826", HashAlgorithm.Sha512)]
        public void Compute(long offset, string expectedResult, HashAlgorithm algorithm)
        {
            var secret = algorithm switch
            {
                HashAlgorithm.Sha1 => Sha1Secret,
                HashAlgorithm.Sha256 => Sha256Secret,
                HashAlgorithm.Sha512 => Sha512Secret,
                _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
            };

            var totp = new Totp(secret, 30, algorithm, 8);
            Assert.Equal(expectedResult, totp.Compute(offset));
        }
    }
}