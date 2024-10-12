// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Stratum.Core.Util;
using Xunit;

namespace Stratum.Test.Util
{
    public class HashUtilTest
    {
        [Fact]
        public void Sha1_null()
        {
            Assert.Throws<ArgumentNullException>(() => HashUtil.Sha1(null));
        }

        [Fact]
        public void Sha1_empty()
        {
            Assert.Equal("da39a3ee5e6b4b0d3255bfef95601890afd80709", HashUtil.Sha1(""));
        }

        [Fact]
        public void Sha1_test()
        {
            Assert.Equal("a94a8fe5ccb19ba61c4c0873d391e987982fbbd3", HashUtil.Sha1("test"));
        }

        [Fact]
        public void Md5_null()
        {
            Assert.Throws<ArgumentNullException>(() => HashUtil.Md5(null));
        }

        [Fact]
        public void Md5_empty()
        {
            Assert.Equal("d41d8cd98f00b204e9800998ecf8427e", HashUtil.Md5(""));
        }

        [Fact]
        public void Md5_test()
        {
            Assert.Equal("098f6bcd4621d373cade4e832627b4f6", HashUtil.Md5("test"));
        }
    }
}