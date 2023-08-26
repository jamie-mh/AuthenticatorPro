// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Linq;
using System.Threading.Tasks;
using AuthenticatorPro.Core;
using AuthenticatorPro.Core.Converter;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Test.Converter.Fixture;
using Moq;
using Xunit;

namespace AuthenticatorPro.Test.Converter
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
            await Assert.ThrowsAsync<ArgumentException>(() =>
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