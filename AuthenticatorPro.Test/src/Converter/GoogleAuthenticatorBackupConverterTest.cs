// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Linq;
using System.Threading.Tasks;
using AuthenticatorPro.Core;
using AuthenticatorPro.Core.Backup;
using AuthenticatorPro.Core.Converter;
using AuthenticatorPro.Test.Converter.Fixture;
using Moq;
using Xunit;

namespace AuthenticatorPro.Test.Converter
{
    public class GoogleAuthenticatorBackupConverterTest : IClassFixture<GoogleAuthenticatorBackupFixture>
    {
        private readonly GoogleAuthenticatorBackupFixture _googleAuthenticatorBackupFixture;
        private readonly GoogleAuthenticatorBackupConverter _googleAuthenticatorBackupConverter;

        public GoogleAuthenticatorBackupConverterTest(GoogleAuthenticatorBackupFixture googleAuthenticatorBackupFixture)
        {
            _googleAuthenticatorBackupFixture = googleAuthenticatorBackupFixture;

            var iconResolver = new Mock<IIconResolver>();
            iconResolver.Setup(r => r.FindServiceKeyByName(It.IsAny<string>())).Returns("icon");

            _googleAuthenticatorBackupConverter = new GoogleAuthenticatorBackupConverter(iconResolver.Object);
        }

        private static void CheckResult(ConversionResult result)
        {
            Assert.Empty(result.Failures);

            Assert.Equal(6, result.Backup.Authenticators.Count());
            Assert.Null(result.Backup.Categories);
            Assert.Null(result.Backup.AuthenticatorCategories);
            Assert.Null(result.Backup.CustomIcons);
        }

        [Fact]
        public async Task ConvertAsync_ok()
        {
            var result = await _googleAuthenticatorBackupConverter.ConvertAsync(_googleAuthenticatorBackupFixture.Data);
            CheckResult(result);
        }
    }
}