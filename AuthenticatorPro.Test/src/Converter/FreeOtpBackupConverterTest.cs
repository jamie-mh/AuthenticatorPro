// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Linq;
using System.Threading.Tasks;
using AuthenticatorPro.Core;
using AuthenticatorPro.Core.Converter;
using AuthenticatorPro.Test.Converter.Fixture;
using Moq;
using Xunit;

namespace AuthenticatorPro.Test.Converter
{
    public class FreeOtpBackupConverterTest : IClassFixture<FreeOtpBackupFixture>
    {
        private readonly FreeOtpBackupFixture _freeOtpBackupFixture;
        private readonly FreeOtpBackupConverter _freeOtpBackupConverter;

        public FreeOtpBackupConverterTest(FreeOtpBackupFixture freeOtpBackupFixture)
        {
            _freeOtpBackupFixture = freeOtpBackupFixture;

            var iconResolver = new Mock<IIconResolver>();
            iconResolver.Setup(r => r.FindServiceKeyByName(It.IsAny<string>())).Returns("icon");

            _freeOtpBackupConverter = new FreeOtpBackupConverter(iconResolver.Object);
        }

        [Fact]
        public async Task ConvertAsync_noPassword()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _freeOtpBackupConverter.ConvertAsync(_freeOtpBackupFixture.Data));
        }

        [Fact]
        public async Task ConvertAsync_wrongPassword()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _freeOtpBackupConverter.ConvertAsync(_freeOtpBackupFixture.Data, "test1"));
        }

        [Fact]
        public async Task ConvertAsync_ok()
        {
            var result = await _freeOtpBackupConverter.ConvertAsync(_freeOtpBackupFixture.Data, "test");

            Assert.Empty(result.Failures);

            Assert.Equal(7, result.Backup.Authenticators.Count());
            Assert.Null(result.Backup.Categories);
            Assert.Null(result.Backup.AuthenticatorCategories);
            Assert.Null(result.Backup.CustomIcons);
        }
    }
}