// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Linq;
using System.Threading.Tasks;
using Stratum.Core;
using Stratum.Core.Backup;
using Stratum.Core.Converter;
using Stratum.Core.Entity;
using Moq;
using Stratum.Test.Converter.Fixture;
using Xunit;

namespace Stratum.Test.Converter
{
    public class AegisBackupConverterTest : IClassFixture<AegisBackupFixture>
    {
        private readonly AegisBackupFixture _aegisBackupFixture;
        private readonly AegisBackupConverter _aegisBackupConverter;

        public AegisBackupConverterTest(AegisBackupFixture aegisBackupFixture)
        {
            _aegisBackupFixture = aegisBackupFixture;

            var iconResolver = new Mock<IIconResolver>();
            iconResolver.Setup(r => r.FindServiceKeyByName(It.IsAny<string>())).Returns("icon");

            var customIconDecoder = new Mock<ICustomIconDecoder>();
            customIconDecoder.Setup(d => d.DecodeAsync(It.IsAny<byte[]>(), It.IsAny<bool>()))
                .ReturnsAsync(new CustomIcon());

            _aegisBackupConverter = new AegisBackupConverter(iconResolver.Object, customIconDecoder.Object);
        }

        [Fact]
        public async Task ConvertAsync_unencrypted()
        {
            var result = await _aegisBackupConverter.ConvertAsync(_aegisBackupFixture.UnencryptedData);

            Assert.Empty(result.Failures);

            Assert.Equal(11, result.Backup.Authenticators.Count());
            Assert.Single(result.Backup.Categories);
            Assert.Equal(2, result.Backup.AuthenticatorCategories.Count());
            Assert.Single(result.Backup.CustomIcons);
        }

        [Fact]
        public async Task ConvertAsync_encrypted_wrongPassword()
        {
            await Assert.ThrowsAsync<BackupPasswordException>(() =>
                _aegisBackupConverter.ConvertAsync(_aegisBackupFixture.EncryptedData, "test1"));
        }

        [Fact]
        public async Task ConvertAsync_encrypted_ok()
        {
            var result = await _aegisBackupConverter.ConvertAsync(_aegisBackupFixture.EncryptedData, "test");

            Assert.Empty(result.Failures);

            Assert.Equal(11, result.Backup.Authenticators.Count());
            Assert.Single(result.Backup.Categories);
            Assert.Equal(2, result.Backup.AuthenticatorCategories.Count());
            Assert.Single(result.Backup.CustomIcons);
        }
    }
}