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
    public class TotpAuthenticatorBackupConverterTest : IClassFixture<TotpAuthenticatorBackupFixture>
    {
        private readonly TotpAuthenticatorBackupFixture _totpAuthenticatorBackupFixture;
        private readonly TotpAuthenticatorBackupConverter _totpAuthenticatorBackupConverter;

        public TotpAuthenticatorBackupConverterTest(TotpAuthenticatorBackupFixture totpAuthenticatorBackupFixture)
        {
            _totpAuthenticatorBackupFixture = totpAuthenticatorBackupFixture;

            var iconResolver = new Mock<IIconResolver>();
            iconResolver.Setup(r => r.FindServiceKeyByName(It.IsAny<string>())).Returns("icon");

            _totpAuthenticatorBackupConverter = new TotpAuthenticatorBackupConverter(iconResolver.Object);
        }

        [Fact]
        public async Task ConvertAsync_wrongPassword()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _totpAuthenticatorBackupConverter.ConvertAsync(_totpAuthenticatorBackupFixture.EncryptedData, "test"));
        }

        [Fact]
        public async Task ConvertAsync_ok()
        {
            var result =
                await _totpAuthenticatorBackupConverter.ConvertAsync(_totpAuthenticatorBackupFixture.EncryptedData,
                    "Testtest1");

            Assert.Empty(result.Failures);

            Assert.Equal(2, result.Backup.Authenticators.Count());
            Assert.Null(result.Backup.Categories);
            Assert.Null(result.Backup.AuthenticatorCategories);
            Assert.Null(result.Backup.CustomIcons);
        }
    }
}