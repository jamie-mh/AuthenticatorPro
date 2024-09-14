// Copyright (C) 2024 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Linq;
using System.Threading.Tasks;
using Stratum.Core;
using Stratum.Core.Backup;
using Stratum.Core.Converter;
using Moq;
using Stratum.Test.Converter.Fixture;
using Xunit;

namespace Stratum.Test.Converter
{
    public class EnteAuthBackupConverterTest : IClassFixture<EnteAuthBackupFixture>
    {
        private readonly EnteAuthBackupFixture _enteAuthBackupFixture;
        private readonly EnteAuthBackupConverter _enteAuthBackupConverter;

        public EnteAuthBackupConverterTest(EnteAuthBackupFixture enteAuthBackupFixture)
        {
            _enteAuthBackupFixture = enteAuthBackupFixture;

            var iconResolver = new Mock<IIconResolver>();
            iconResolver.Setup(r => r.FindServiceKeyByName(It.IsAny<string>())).Returns("icon");

            _enteAuthBackupConverter = new EnteAuthBackupConverter(iconResolver.Object);
        }

        [Fact]
        public async Task ConvertAsync_unencrypted()
        {
            var result = await _enteAuthBackupConverter.ConvertAsync(_enteAuthBackupFixture.UnencryptedData);
            
            Assert.Empty(result.Failures);
            
            Assert.Equal(7, result.Backup.Authenticators.Count());
            Assert.Null(result.Backup.Categories);
            Assert.Null(result.Backup.AuthenticatorCategories);
            Assert.Null(result.Backup.CustomIcons);
        }

        [Fact]
        public async Task ConvertAsync_encrypted_wrongPassword()
        {
            await Assert.ThrowsAsync<BackupPasswordException>(() =>
                _enteAuthBackupConverter.ConvertAsync(_enteAuthBackupFixture.EncryptedData, "test1"));
        }

        [Fact]
        public async Task ConvertAsync_encrypted_ok()
        {
            var result = await _enteAuthBackupConverter.ConvertAsync(_enteAuthBackupFixture.EncryptedData, "test");

            Assert.Empty(result.Failures);

            Assert.Equal(7, result.Backup.Authenticators.Count());
            Assert.Null(result.Backup.Categories);
            Assert.Null(result.Backup.AuthenticatorCategories);
            Assert.Null(result.Backup.CustomIcons);
        }
    }
}