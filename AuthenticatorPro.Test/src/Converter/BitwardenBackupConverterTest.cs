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
    public class BitwardenBackupConverterTest : IClassFixture<BitwardenBackupFixture>
    {
        private readonly BitwardenBackupFixture _bitwardenBackupFixture;
        private readonly BitwardenBackupConverter _bitwardenBackupConverter;

        public BitwardenBackupConverterTest(BitwardenBackupFixture bitwardenBackupFixture)
        {
            _bitwardenBackupFixture = bitwardenBackupFixture;

            var iconResolver = new Mock<IIconResolver>();
            iconResolver.Setup(r => r.FindServiceKeyByName(It.IsAny<string>())).Returns("icon");

            _bitwardenBackupConverter = new BitwardenBackupConverter(iconResolver.Object);
        }

        [Fact]
        public async Task ConvertAsync()
        {
            var result = await _bitwardenBackupConverter.ConvertAsync(_bitwardenBackupFixture.Data);

            Assert.Empty(result.Failures);

            Assert.Equal(3, result.Backup.Authenticators.Count());
            Assert.Single(result.Backup.Categories);
            Assert.Equal(2, result.Backup.AuthenticatorCategories.Count());
            Assert.Null(result.Backup.CustomIcons);
        }
    }
}