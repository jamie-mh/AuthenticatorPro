// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core;
using AuthenticatorPro.Core.Converter;
using AuthenticatorPro.Test.Converter.Fixture;
using Moq;
using System.Linq;
using System.Threading.Tasks;
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

        [Fact]
        public async Task ConvertAsync()
        {
            var result = await _googleAuthenticatorBackupConverter.ConvertAsync(_googleAuthenticatorBackupFixture.Data);

            Assert.Empty(result.Failures);

            Assert.Equal(7, result.Backup.Authenticators.Count());
            Assert.Null(result.Backup.Categories);
            Assert.Null(result.Backup.AuthenticatorCategories);
            Assert.Null(result.Backup.CustomIcons);
        }
    }
}