// Copyright (C) 2023 jmh
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
    public class AndOtpBackupConverterTest : IClassFixture<AndOtpBackupFixture>
    {
        private readonly AndOtpBackupFixture _andOtpBackupFixture;
        private readonly AndOtpBackupConverter _andOtpBackupConverter;

        public AndOtpBackupConverterTest(AndOtpBackupFixture andOtpBackupFixture)
        {
            _andOtpBackupFixture = andOtpBackupFixture;

            var iconResolver = new Mock<IIconResolver>();
            iconResolver.Setup(r => r.FindServiceKeyByName(It.IsAny<string>())).Returns("icon");

            _andOtpBackupConverter = new AndOtpBackupConverter(iconResolver.Object);
        }

        [Fact]
        public async Task ConvertAsync_unencrypted()
        {
            var result = await _andOtpBackupConverter.ConvertAsync(_andOtpBackupFixture.UnencryptedData);

            Assert.Single(result.Failures);

            Assert.Equal(8, result.Backup.Authenticators.Count());
            Assert.Single(result.Backup.Categories);
            Assert.Equal(2, result.Backup.AuthenticatorCategories.Count());
            Assert.Null(result.Backup.CustomIcons);
        }

        [Fact]
        public async Task ConvertAsync_encrypted_wrongPassword()
        {
            await Assert.ThrowsAsync<BackupPasswordException>(() =>
                _andOtpBackupConverter.ConvertAsync(_andOtpBackupFixture.EncryptedData, "test"));
        }

        [Fact]
        public async Task ConvertAsync_encrypted_ok()
        {
            var result = await _andOtpBackupConverter.ConvertAsync(_andOtpBackupFixture.EncryptedData, "testtest");

            Assert.Single(result.Failures);

            Assert.Equal(8, result.Backup.Authenticators.Count());
            Assert.Single(result.Backup.Categories);
            Assert.Equal(2, result.Backup.AuthenticatorCategories.Count());
            Assert.Null(result.Backup.CustomIcons);
        }
    }
}