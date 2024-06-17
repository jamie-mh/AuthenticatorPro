// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthenticatorPro.Core;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Persistence;
using AuthenticatorPro.Core.Service;
using AuthenticatorPro.Core.Service.Impl;
using HtmlAgilityPack;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;
using ZXing;
using ZXing.Common;

namespace AuthenticatorPro.Test.Service
{
    public class BackupServiceTest
    {
        private readonly Mock<IAuthenticatorRepository> _authenticatorRepository;
        private readonly Mock<ICategoryRepository> _categoryRepository;
        private readonly Mock<IAuthenticatorCategoryRepository> _authenticatorCategoryRepository;
        private readonly Mock<ICustomIconRepository> _customIconRepository;
        private readonly Mock<IAssetProvider> _assetProvider;

        private readonly IBackupService _backupService;

        public BackupServiceTest()
        {
            _authenticatorRepository = new Mock<IAuthenticatorRepository>();
            _categoryRepository = new Mock<ICategoryRepository>();
            _authenticatorCategoryRepository = new Mock<IAuthenticatorCategoryRepository>();
            _customIconRepository = new Mock<ICustomIconRepository>();
            _assetProvider = new Mock<IAssetProvider>();

            _backupService = new BackupService(
                _authenticatorRepository.Object,
                _categoryRepository.Object,
                _authenticatorCategoryRepository.Object,
                _customIconRepository.Object,
                _assetProvider.Object);
        }

        [Fact]
        public async Task CreateBackupAsync()
        {
            var auths = new List<Authenticator>();
            var categories = new List<Category>();
            var authCategories = new List<AuthenticatorCategory>();
            var customIcons = new List<CustomIcon>();

            _authenticatorRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(auths);
            _categoryRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);
            _authenticatorCategoryRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(authCategories);
            _customIconRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(customIcons);

            var backup = await _backupService.CreateBackupAsync();

            Assert.Equal(auths, backup.Authenticators);
            Assert.Equal(categories, backup.Categories);
            Assert.Equal(authCategories, backup.AuthenticatorCategories);
            Assert.Equal(customIcons, backup.CustomIcons);
        }

        [Fact]
        public async Task CreateHtmlBackupAsync()
        {
            var authA = new Authenticator
            {
                Type = AuthenticatorType.Totp,
                Issuer = "issuer",
                Username = "username",
                Secret = "ABCDEFGH",
                Digits = 6,
                Period = 30
            };

            var authB = new Authenticator
            {
                Type = AuthenticatorType.Hotp,
                Issuer = "issuer2",
                Username = "username2",
                Secret = "ABCDEFGH123",
                Digits = 6,
                Period = 30,
                Counter = 10
            };

            var authC = new Mock<Authenticator>();
            authC.Setup(a => a.GetUri()).Throws(new NotSupportedException());

            _authenticatorRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync([authA, authB, authC.Object]);
            _assetProvider.Setup(a => a.ReadStringAsync("backup_template.html")).ReturnsAsync("%ITEMS");

            var backup = await _backupService.CreateHtmlBackupAsync();

            var document = new HtmlDocument();
            document.LoadHtml(backup.ToString());

            var rows = document.DocumentNode.SelectNodes("//tr");
            Assert.Equal(2, rows.Count);

            var barcodeReader = new ZXing.ImageSharp.BarcodeReader<Rgba32>
            {
                Options = new DecodingOptions
                {
                    PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE }
                }
            };

            void AssertRowMatches(HtmlNode node, Authenticator auth)
            {
                var tds = node.SelectNodes("td");
                Assert.Equal(auth.Issuer, tds[0].InnerText);
                Assert.Equal(auth.Username, tds[1].InnerText);
                Assert.Equal(auth.GetUri(), tds[2].InnerText);

                var img = tds[3].SelectSingleNode("img");
                var src = img.Attributes["src"].Value;
                var data = Convert.FromBase64String(src["data:image/png;base64,".Length..]);

                using var decodedImage = Image.Load<Rgba32>(data);
                var qrCodeResult = barcodeReader.Decode(decodedImage);
                Assert.Equal(auth.GetUri(), qrCodeResult.Text);
            }

            AssertRowMatches(rows[0], authA);
            AssertRowMatches(rows[1], authB);
        }

        [Fact]
        public async Task CreateUriListBackupAsync()
        {
            var authA = new Authenticator
            {
                Type = AuthenticatorType.MobileOtp,
                Issuer = "issuer",
                Username = "username",
                Secret = "ABCDEFGH",
                Digits = 6,
                Period = 30
            };

            var authB = new Authenticator
            {
                Type = AuthenticatorType.SteamOtp,
                Issuer = "steam",
                Username = "username2",
                Secret = "ABCDEFGH123",
                Digits = 5,
                Period = 30,
                Counter = 10
            };

            var authC = new Mock<Authenticator>();
            authC.Setup(a => a.GetUri()).Throws(new NotSupportedException());

            _authenticatorRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync([authA, authB, authC.Object]);

            var backup = await _backupService.CreateUriListBackupAsync();
            var lines = backup.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(2, lines.Length);
            Assert.Equal(authA.GetUri(), lines[0]);
            Assert.Equal(authB.GetUri(), lines[1]);
        }
    }
}