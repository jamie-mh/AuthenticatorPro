// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core;
using AuthenticatorPro.Core.Converter;
using AuthenticatorPro.Test.Converter.Fixture;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AuthenticatorPro.Test.Converter
{
    public class TwoFasBackupConverterTest : IClassFixture<TwoFasBackupFixture>
    {
        private readonly TwoFasBackupFixture _twoFasBackupFixture;
        private readonly TwoFasBackupConverter _twoFasBackupConverter;

        public TwoFasBackupConverterTest(TwoFasBackupFixture twoFasBackupFixture)
        {
            _twoFasBackupFixture = twoFasBackupFixture;

            var iconResolver = new Mock<IIconResolver>();
            iconResolver.Setup(r => r.FindServiceKeyByName(It.IsAny<string>())).Returns("icon");

            _twoFasBackupConverter = new TwoFasBackupConverter(iconResolver.Object);
        }

        [Fact]
        public async Task ConvertAsync_unencrypted()
        {
            var result = await _twoFasBackupConverter.ConvertAsync(_twoFasBackupFixture.UnencryptedData);

            Assert.Empty(result.Failures);

            Assert.Equal(6, result.Backup.Authenticators.Count());
            Assert.Single(result.Backup.Categories);
            Assert.Equal(2, result.Backup.AuthenticatorCategories.Count());
            Assert.Null(result.Backup.CustomIcons);
        }

        [Fact]
        public async Task ConvertAsync_encrypted_wrongPassword()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _twoFasBackupConverter.ConvertAsync(_twoFasBackupFixture.EncryptedData, "test1"));
        }

        [Fact]
        public async Task ConvertAsync_encrypted_ok()
        {
            var result = await _twoFasBackupConverter.ConvertAsync(_twoFasBackupFixture.EncryptedData, "test");

            Assert.Empty(result.Failures);

            Assert.Equal(6, result.Backup.Authenticators.Count());
            Assert.Single(result.Backup.Categories);
            Assert.Equal(2, result.Backup.AuthenticatorCategories.Count());
            Assert.Null(result.Backup.CustomIcons);
        }
    }
}