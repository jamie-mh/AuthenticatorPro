// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Threading.Tasks;
using Stratum.Core;
using Stratum.Core.Backup;
using Stratum.Core.Converter;
using Moq;
using Stratum.Test.Converter.Fixture;
using Xunit;

namespace Stratum.Test.Converter
{
    public class WinAuthBackupConverterTest : IClassFixture<WinAuthBackupFixture>
    {
        private readonly WinAuthBackupFixture _winAuthBackupFixture;
        private readonly WinAuthBackupConverter _winAuthBackupConverter;

        public WinAuthBackupConverterTest(WinAuthBackupFixture winAuthBackupFixture)
        {
            _winAuthBackupFixture = winAuthBackupFixture;

            var iconResolver = new Mock<IIconResolver>();
            iconResolver.Setup(r => r.FindServiceKeyByName(It.IsAny<string>())).Returns("icon");

            _winAuthBackupConverter = new WinAuthBackupConverter(iconResolver.Object);
        }

        [Fact]
        public async Task ConvertAsync_encrypted()
        {
            var result = await _winAuthBackupConverter.ConvertAsync(_winAuthBackupFixture.Data, "test");

            Assert.Empty(result.Failures);

            Assert.Single(result.Backup.Authenticators);
            Assert.Null(result.Backup.Categories);
            Assert.Null(result.Backup.AuthenticatorCategories);
            Assert.Null(result.Backup.CustomIcons);
        }

        [Fact]
        public async Task ConvertAsync_encrypted_wrongPassword()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _winAuthBackupConverter.ConvertAsync(_winAuthBackupFixture.Data));
            await Assert.ThrowsAsync<BackupPasswordException>(() =>
                _winAuthBackupConverter.ConvertAsync(_winAuthBackupFixture.Data, "test1"));
        }

        [Fact]
        public async Task ConvertAsync_invalid()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _winAuthBackupConverter.ConvertAsync(_winAuthBackupFixture.InvalidData, "test"));
        }
    }
}