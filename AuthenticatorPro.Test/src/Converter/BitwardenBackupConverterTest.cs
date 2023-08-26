// Copyright (C) 2023 jmh
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
        public async Task ConvertAsync_unencrypted()
        {
            var result = await _bitwardenBackupConverter.ConvertAsync(_bitwardenBackupFixture.Data);

            Assert.Empty(result.Failures);

            Assert.Equal(8, result.Backup.Authenticators.Count());
            Assert.Single(result.Backup.Categories);
            Assert.Equal(3, result.Backup.AuthenticatorCategories.Count());
            Assert.Null(result.Backup.CustomIcons);
        }

        [Fact]
        public async Task ConvertAsync_encrypted_pbkdf2()
        {
            var result =
                await _bitwardenBackupConverter.ConvertAsync(_bitwardenBackupFixture.EncryptedPbkdf2Data,
                    "rud9^5S6$^Ewmr%d");

            Assert.Empty(result.Failures);

            Assert.Equal(8, result.Backup.Authenticators.Count());
            Assert.Single(result.Backup.Categories);
            Assert.Equal(3, result.Backup.AuthenticatorCategories.Count());
            Assert.Null(result.Backup.CustomIcons);
        }

        [Fact]
        public async Task ConvertAsync_encrypted_argon2id()
        {
            var result = await _bitwardenBackupConverter.ConvertAsync(_bitwardenBackupFixture.EncryptedArgon2IdData,
                "rud9^5S6$^Ewmr%d");

            Assert.Empty(result.Failures);

            Assert.Equal(8, result.Backup.Authenticators.Count());
            Assert.Single(result.Backup.Categories);
            Assert.Equal(3, result.Backup.AuthenticatorCategories.Count());
            Assert.Null(result.Backup.CustomIcons);
        }

        [Fact]
        public async Task ConvertAsync_encrypted_accountRestricted()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _bitwardenBackupConverter.ConvertAsync(_bitwardenBackupFixture.EncryptedAccountRestrictedData,
                    "password"));
        }

        [Fact]
        public async Task ConvertAsync_encrypted_noPassword()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _bitwardenBackupConverter.ConvertAsync(_bitwardenBackupFixture.EncryptedPbkdf2Data));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _bitwardenBackupConverter.ConvertAsync(_bitwardenBackupFixture.EncryptedPbkdf2Data, ""));
        }

        [Fact]
        public async Task ConvertAsync_encrypted_wrongPassword()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _bitwardenBackupConverter.ConvertAsync(_bitwardenBackupFixture.EncryptedPbkdf2Data, "wrong password"));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _bitwardenBackupConverter.ConvertAsync(_bitwardenBackupFixture.EncryptedArgon2IdData,
                    "wrong password"));
        }
    }
}