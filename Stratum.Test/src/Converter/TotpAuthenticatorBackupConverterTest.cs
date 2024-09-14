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
            await Assert.ThrowsAsync<BackupPasswordException>(() =>
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