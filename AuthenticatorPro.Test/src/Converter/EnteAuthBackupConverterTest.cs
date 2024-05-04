// Copyright (C) 2024 jmh
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
    public class EnteAuthBackupConverterTest : IClassFixture<EnteBackupFixture>
    {
        private readonly EnteBackupFixture _enteBackupFixture;
        private readonly EnteAuthBackupConverter _enteAuthBackupConverter;

        public EnteAuthBackupConverterTest(EnteBackupFixture enteBackupFixture)
        {
            _enteBackupFixture = enteBackupFixture;

            var iconResolver = new Mock<IIconResolver>();
            iconResolver.Setup(r => r.FindServiceKeyByName(It.IsAny<string>())).Returns("icon");

            _enteAuthBackupConverter = new EnteAuthBackupConverter(iconResolver.Object);
        }

        [Fact]
        public async Task ConvertAsync_unencrypted()
        {
            var result = await _enteAuthBackupConverter.ConvertAsync(_enteBackupFixture.UnencryptedData);
            
            Assert.Empty(result.Failures);
            
            Assert.Equal(7, result.Backup.Authenticators.Count());
            Assert.Null(result.Backup.Categories);
            Assert.Null(result.Backup.AuthenticatorCategories);
            Assert.Null(result.Backup.CustomIcons);
        }

        [Fact]
        public async Task ConvertAsync_encrypted_wrongPassword()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _enteAuthBackupConverter.ConvertAsync(_enteBackupFixture.EncryptedData, "test1"));
        }

        [Fact]
        public async Task ConvertAsync_encrypted_ok()
        {
            var result = await _enteAuthBackupConverter.ConvertAsync(_enteBackupFixture.EncryptedData, "test");

            Assert.Empty(result.Failures);

            Assert.Equal(7, result.Backup.Authenticators.Count());
            Assert.Null(result.Backup.Categories);
            Assert.Null(result.Backup.AuthenticatorCategories);
            Assert.Null(result.Backup.CustomIcons);
        }
    }
}