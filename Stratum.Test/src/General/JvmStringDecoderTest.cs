// Copyright (C) 2023 jmh
// SPDX-License-Identifier:GPL-3.0-only

using Stratum.Core;
using Xunit;

namespace Stratum.Test.General
{
    public class JvmStringDecoderTest
    {
        private readonly JvmStringDecoder _decoder = new();

        [Theory]
        [InlineData(new byte[] { 0x41, 0x42, 0x43 }, "ABC")]
        [InlineData(new byte[] { 0x31, 0x32, 0x33, 0x20, 0xC0, 0x80 }, "123 \0")]
        [InlineData(new byte[] { 0xED, 0xA0, 0x81, 0xED, 0xB0, 0x80 }, "\ud801\udc00")]
        [InlineData(new byte[] { 0xED, 0xA0, 0xB4, 0xED, 0xB5, 0xA0 }, "\ud834\udd60")]
        [InlineData(new byte[] { 0xE2, 0x82, 0xAC, 0xC2, 0xA3 }, "\u20ac\u00a3")]
        [InlineData(new byte[] { 0xE4, 0xBD, 0xA0, 0xE5, 0xA5, 0xBD }, "你好")]
        [InlineData(new byte[] { 0x30, 0xE4, 0xBA, 0xBA, 0xED, 0xA0, 0xBD, 0xED, 0xB8, 0x80 }, "0人\ud83d\ude00")]
        public void GetString(byte[] input, string expected)
        {
            Assert.Equal(expected, _decoder.GetString(input)); 
        }
    }
}