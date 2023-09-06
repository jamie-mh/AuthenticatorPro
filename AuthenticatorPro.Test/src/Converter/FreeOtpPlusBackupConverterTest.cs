// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Linq;
using System.Threading.Tasks;
using AuthenticatorPro.Core;
using AuthenticatorPro.Core.Converter;
using AuthenticatorPro.Test.Converter.Fixture;
using Moq;
using Xunit;

namespace AuthenticatorPro.Test.Converter
{
    public class FreeOtpPlusBackupConverterTest : IClassFixture<FreeOtpPlusBackupFixture>
    {
        private readonly FreeOtpPlusBackupFixture _freeOtpPlusBackupFixture;
        private readonly FreeOtpPlusBackupConverter _freeOtpPlusBackupConverter;

        public FreeOtpPlusBackupConverterTest(FreeOtpPlusBackupFixture freeOtpPlusBackupFixture)
        {
            _freeOtpPlusBackupFixture = freeOtpPlusBackupFixture;

            var iconResolver = new Mock<IIconResolver>();
            iconResolver.Setup(r => r.FindServiceKeyByName(It.IsAny<string>())).Returns("icon");

            _freeOtpPlusBackupConverter = new FreeOtpPlusBackupConverter(iconResolver.Object);
        }

        [Fact]
        public async Task ConvertAsync()
        {
            var result = await _freeOtpPlusBackupConverter.ConvertAsync(_freeOtpPlusBackupFixture.Data);

            Assert.Empty(result.Failures);

            Assert.Equal(7, result.Backup.Authenticators.Count());
            Assert.Null(result.Backup.Categories);
            Assert.Null(result.Backup.AuthenticatorCategories);
            Assert.Null(result.Backup.CustomIcons);
        }
    }
}