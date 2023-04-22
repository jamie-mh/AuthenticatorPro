// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Service;
using AuthenticatorPro.Core.Service.Impl;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace AuthenticatorPro.Test.Service
{
    public class RestoreServiceTest
    {
        private readonly Mock<IAuthenticatorService> _authenticatorService;
        private readonly Mock<ICategoryService> _categoryService;
        private readonly Mock<IAuthenticatorCategoryService> _authenticatorCategoryService;
        private readonly Mock<ICustomIconService> _customIconService;

        private readonly IRestoreService _restoreService;

        public RestoreServiceTest()
        {
            _authenticatorService = new Mock<IAuthenticatorService>();
            _categoryService = new Mock<ICategoryService>();
            _authenticatorCategoryService = new Mock<IAuthenticatorCategoryService>();
            _customIconService = new Mock<ICustomIconService>();

            _restoreService = new RestoreService(
                _authenticatorService.Object,
                _categoryService.Object,
                _authenticatorCategoryService.Object,
                _customIconService.Object);
        }

        [Fact]
        public async Task RestoreAsync_nulls()
        {
            var backup = new Core.Backup.Backup();

            _authenticatorService.Setup(s => s.AddManyAsync(backup.Authenticators)).ReturnsAsync(1);

            var result = await _restoreService.RestoreAsync(backup);

            Assert.Equal(1, result.AddedAuthenticatorCount);
            Assert.Equal(0, result.UpdatedAuthenticatorCount);

            Assert.Equal(0, result.AddedCategoryCount);
            Assert.Equal(0, result.UpdatedCategoryCount);

            Assert.Equal(0, result.AddedAuthenticatorCategoryCount);
            Assert.Equal(0, result.UpdatedAuthenticatorCategoryCount);

            Assert.Equal(0, result.AddedCustomIconCount);

            _categoryService.Verify(s => s.AddManyAsync(It.IsAny<List<Category>>()), Times.Never());
            _authenticatorCategoryService.Verify(s => s.AddManyAsync(It.IsAny<List<AuthenticatorCategory>>()), Times.Never());
            _customIconService.Verify(s => s.AddManyAsync(It.IsAny<List<CustomIcon>>()), Times.Never());
        }

        [Fact]
        public async Task RestoreAsync_full()
        {
            var backup = new Core.Backup.Backup
            {
                Authenticators = new List<Authenticator>(), 
                Categories = new List<Category>(),
                AuthenticatorCategories = new List<AuthenticatorCategory>(),
                CustomIcons = new List<CustomIcon>()
            };

            _authenticatorService.Setup(s => s.AddManyAsync(backup.Authenticators)).ReturnsAsync(1);
            _categoryService.Setup(s => s.AddManyAsync(backup.Categories)).ReturnsAsync(1);
            _authenticatorCategoryService.Setup(s => s.AddManyAsync(backup.AuthenticatorCategories)).ReturnsAsync(1);
            _customIconService.Setup(s => s.AddManyAsync(backup.CustomIcons)).ReturnsAsync(1);

            var result = await _restoreService.RestoreAsync(backup);

            Assert.Equal(1, result.AddedAuthenticatorCount);
            Assert.Equal(0, result.UpdatedAuthenticatorCount);

            Assert.Equal(1, result.AddedCategoryCount);
            Assert.Equal(0, result.UpdatedCategoryCount);

            Assert.Equal(1, result.AddedAuthenticatorCategoryCount);
            Assert.Equal(0, result.UpdatedAuthenticatorCategoryCount);

            Assert.Equal(1, result.AddedCustomIconCount);
        }

        [Fact]
        public async Task RestoreAndUpdateAsync_nulls()
        {
            var backup = new Core.Backup.Backup();

            _authenticatorService.Setup(s => s.AddOrUpdateManyAsync(backup.Authenticators))
                .ReturnsAsync(new ValueTuple<int, int>(1, 2));

            var result = await _restoreService.RestoreAndUpdateAsync(backup);

            Assert.Equal(1, result.AddedAuthenticatorCount);
            Assert.Equal(2, result.UpdatedAuthenticatorCount);

            Assert.Equal(0, result.AddedCategoryCount);
            Assert.Equal(0, result.UpdatedCategoryCount);

            Assert.Equal(0, result.AddedAuthenticatorCategoryCount);
            Assert.Equal(0, result.UpdatedAuthenticatorCategoryCount);

            Assert.Equal(0, result.AddedCustomIconCount);

            _categoryService.Verify(s => s.AddOrUpdateManyAsync(It.IsAny<List<Category>>()), Times.Never());
            _authenticatorCategoryService.Verify(s => s.AddOrUpdateManyAsync(It.IsAny<List<AuthenticatorCategory>>()), Times.Never());
            _customIconService.Verify(s => s.AddManyAsync(It.IsAny<List<CustomIcon>>()), Times.Never());
        }

        [Fact]
        public async Task RestoreAndUpdateAsync_full()
        {
            var backup = new Core.Backup.Backup
            {
                Authenticators = new List<Authenticator>(), 
                Categories = new List<Category>(),
                AuthenticatorCategories = new List<AuthenticatorCategory>(),
                CustomIcons = new List<CustomIcon>()
            };

            _authenticatorService.Setup(s => s.AddOrUpdateManyAsync(backup.Authenticators))
                .ReturnsAsync(new ValueTuple<int, int>(1, 2));
            _categoryService.Setup(s => s.AddOrUpdateManyAsync(backup.Categories))
                .ReturnsAsync(new ValueTuple<int, int>(3, 4));
            _authenticatorCategoryService.Setup(s => s.AddOrUpdateManyAsync(backup.AuthenticatorCategories))
                .ReturnsAsync(new ValueTuple<int, int>(5, 6));
            _customIconService.Setup(s => s.AddManyAsync(backup.CustomIcons))
                .ReturnsAsync(7);

            var result = await _restoreService.RestoreAndUpdateAsync(backup);

            Assert.Equal(1, result.AddedAuthenticatorCount);
            Assert.Equal(2, result.UpdatedAuthenticatorCount);

            Assert.Equal(3, result.AddedCategoryCount);
            Assert.Equal(4, result.UpdatedCategoryCount);

            Assert.Equal(5, result.AddedAuthenticatorCategoryCount);
            Assert.Equal(6, result.UpdatedAuthenticatorCategoryCount);

            Assert.Equal(7, result.AddedCustomIconCount);
        }
    }
}