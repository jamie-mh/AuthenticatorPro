// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stratum.Core.Entity;
using Stratum.Core.Persistence;
using Stratum.Core.Persistence.Exception;
using Stratum.Core.Service;
using Stratum.Core.Service.Impl;
using Moq;
using Xunit;

namespace Stratum.Test.Service
{
    public class CustomIconServiceTest
    {
        private readonly Mock<ICustomIconRepository> _customIconRepository;
        private readonly Mock<IAuthenticatorRepository> _authenticatorRepository;
        private readonly ICustomIconService _customIconService;

        public CustomIconServiceTest()
        {
            _customIconRepository = new Mock<ICustomIconRepository>();
            _authenticatorRepository = new Mock<IAuthenticatorRepository>();
            _customIconService = new CustomIconService(
                _customIconRepository.Object, _authenticatorRepository.Object);
        }

        [Fact]
        public async Task AddIfNotExistsAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _customIconService.AddIfNotExistsAsync(null));
        }

        [Fact]
        public async Task AddIfNotExistsAsync_exists()
        {
            var icon = new CustomIcon { Id = "id" };

            _customIconRepository.Setup(r => r.GetAsync("id")).ReturnsAsync(icon);
            _customIconRepository.Setup(r => r.CreateAsync(icon)).Verifiable();

            await _customIconService.AddIfNotExistsAsync(icon);

            _customIconRepository.Verify(r => r.CreateAsync(icon), Times.Never());
        }

        [Fact]
        public async Task AddIfNotExistsAsync_ok()
        {
            var icon = new CustomIcon { Id = "id" };

            _customIconRepository.Setup(r => r.GetAsync("id")).ReturnsAsync((CustomIcon) null);
            _customIconRepository.Setup(r => r.CreateAsync(icon)).Verifiable();

            await _customIconService.AddIfNotExistsAsync(icon);

            _customIconRepository.Verify(r => r.CreateAsync(icon));
        }

        [Fact]
        public async Task AddManyAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _customIconService.AddManyAsync(null));
        }

        [Fact]
        public async Task AddManyAsync_exists()
        {
            var icon = new CustomIcon();
            _customIconRepository.Setup(r => r.CreateAsync(icon)).ThrowsAsync(new EntityDuplicateException());

            var added = await _customIconService.AddManyAsync(new List<CustomIcon> { icon });

            Assert.Equal(0, added);
        }

        [Fact]
        public async Task AddManyAsync_ok()
        {
            var icon = new CustomIcon();
            _customIconRepository.Setup(r => r.CreateAsync(icon)).Verifiable();

            var added = await _customIconService.AddManyAsync(new List<CustomIcon> { icon });

            Assert.Equal(1, added);
            _customIconRepository.Verify(r => r.CreateAsync(icon));
        }

        [Fact]
        public async Task GetAllAsync()
        {
            var icons = new List<CustomIcon> { new() };
            _customIconRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(icons);
            Assert.Equal(icons, await _customIconService.GetAllAsync());
        }

        [Fact]
        public async Task CullUnusedAsync()
        {
            var authA = new Authenticator { Icon = $"{CustomIcon.Prefix}id" };
            var authB = new Authenticator { Icon = $"{CustomIcon.Prefix}id" };
            var authC = new Authenticator { Icon = "icon" };

            var iconUsed = new CustomIcon { Id = "id" };
            var iconUnused = new CustomIcon { Id = "id2" };

            _authenticatorRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync([authA, authB, authC]);
            _customIconRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync([iconUsed, iconUnused]);

            _customIconRepository.Setup(r => r.DeleteAsync(iconUsed)).Verifiable();
            _customIconRepository.Setup(r => r.DeleteAsync(iconUnused)).Verifiable();

            await _customIconService.CullUnusedAsync();

            _customIconRepository.Verify(r => r.DeleteAsync(iconUsed), Times.Never());
            _customIconRepository.Verify(r => r.DeleteAsync(iconUnused), Times.Once());
        }
    }
}