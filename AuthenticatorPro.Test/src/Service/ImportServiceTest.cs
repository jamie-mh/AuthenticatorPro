// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthenticatorPro.Core;
using AuthenticatorPro.Core.Backup;
using AuthenticatorPro.Core.Converter;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Service;
using AuthenticatorPro.Core.Service.Impl;
using Moq;
using Xunit;

namespace AuthenticatorPro.Test.Service
{
    public class ImportServiceTest
    {
        private readonly Mock<IRestoreService> _restoreService;
        private readonly IImportService _importService;

        public ImportServiceTest()
        {
            _restoreService = new Mock<IRestoreService>();
            _importService = new ImportService(_restoreService.Object);
        }

        [Fact]
        public async Task ImportAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _importService.ImportAsync(null, new byte[] { 0 }, null));

            var converter = new Mock<BackupConverter>(new Mock<IIconResolver>().Object);
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _importService.ImportAsync(converter.Object, null, null));
        }

        [Fact]
        public async Task ImportAsync_ok()
        {
            var data = new byte[] { 1, 2, 3 };
            const string password = "password";

            var conversionResult = new ConversionResult
            {
                Backup = new Core.Backup.Backup { Authenticators = new List<Authenticator>() },
                Failures = new List<ConversionFailure>()
            };

            var converter = new Mock<BackupConverter>(new Mock<IIconResolver>().Object);
            converter.Setup(c => c.ConvertAsync(data, password)).ReturnsAsync(conversionResult);

            var restoreResult = new RestoreResult();
            _restoreService.Setup(s => s.RestoreAsync(conversionResult.Backup)).ReturnsAsync(restoreResult);

            var result = await _importService.ImportAsync(converter.Object, data, password);

            Assert.Equal(conversionResult, result.Item1);
            Assert.Equal(restoreResult, result.Item2);

            _restoreService.Verify(s => s.RestoreAsync(conversionResult.Backup));
        }
    }
}