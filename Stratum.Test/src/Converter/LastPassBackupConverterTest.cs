// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Linq;
using System.Threading.Tasks;
using Stratum.Core;
using Stratum.Core.Converter;
using Moq;
using Stratum.Test.Converter.Fixture;
using Xunit;

namespace Stratum.Test.Converter
{
    public class LastPassBackupConverterTest : IClassFixture<LastPassBackupFixture>
    {
        private readonly LastPassBackupFixture _lastPassBackupFixture;
        private readonly LastPassBackupConverter _lastPassBackupConverter;

        public LastPassBackupConverterTest(LastPassBackupFixture lastPassBackupFixture)
        {
            _lastPassBackupFixture = lastPassBackupFixture;

            var iconResolver = new Mock<IIconResolver>();
            iconResolver.Setup(r => r.FindServiceKeyByName(It.IsAny<string>())).Returns("icon");

            _lastPassBackupConverter = new LastPassBackupConverter(iconResolver.Object);
        }

        [Fact]
        public async Task ConvertAsync()
        {
            var result = await _lastPassBackupConverter.ConvertAsync(_lastPassBackupFixture.Data);

            Assert.Empty(result.Failures);

            Assert.Equal(6, result.Backup.Authenticators.Count());
            Assert.Equal(2, result.Backup.Categories.Count());
            Assert.Equal(4, result.Backup.AuthenticatorCategories.Count());
            Assert.Null(result.Backup.CustomIcons);
        }
    }
}