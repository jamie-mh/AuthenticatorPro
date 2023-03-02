// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Persistence;
using AuthenticatorPro.Core.Persistence.Exception;
using AuthenticatorPro.Core.Service;
using AuthenticatorPro.Core.Service.Impl;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace AuthenticatorPro.Test.Service
{
    public class AuthenticatorCategoryServiceTest
    {
        private readonly Mock<IAuthenticatorCategoryRepository> _authenticatorCategoryRepository;
        private readonly Mock<IEqualityComparer<AuthenticatorCategory>> _equalityComparer;

        private readonly IAuthenticatorCategoryService _authenticatorCategoryService;

        public AuthenticatorCategoryServiceTest()
        {
            _authenticatorCategoryRepository = new Mock<IAuthenticatorCategoryRepository>();
            _equalityComparer = new Mock<IEqualityComparer<AuthenticatorCategory>>();

            _authenticatorCategoryService = new AuthenticatorCategoryService(
                _authenticatorCategoryRepository.Object,
                _equalityComparer.Object);
        }

        [Fact]
        public async Task AddManyAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _authenticatorCategoryService.AddManyAsync(null));
        }

        [Fact]
        public async Task AddManyAsync_exists()
        {
            var ac = new AuthenticatorCategory();
            _authenticatorCategoryRepository.Setup(r => r.CreateAsync(ac)).ThrowsAsync(new EntityDuplicateException());

            var added = await _authenticatorCategoryService.AddManyAsync(new List<AuthenticatorCategory> { ac });

            Assert.Equal(0, added);
        }

        [Fact]
        public async Task AddManyAsync_ok()
        {
            var ac = new AuthenticatorCategory();

            _authenticatorCategoryRepository.Setup(r => r.CreateAsync(ac)).Verifiable();

            var added = await _authenticatorCategoryService.AddManyAsync(new List<AuthenticatorCategory> { ac });

            Assert.Equal(1, added);
            _authenticatorCategoryRepository.Verify(r => r.CreateAsync(ac));
        }

        [Fact]
        public async Task UpdateManyAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _authenticatorCategoryService.UpdateManyAsync(null));
        }

        [Fact]
        public async Task UpdateManyAsync_doesntExist()
        {
            var ac = new AuthenticatorCategory { AuthenticatorSecret = "authenticator", CategoryId = "category" };
            var id = new ValueTuple<string, string>("authenticator", "category");

            _authenticatorCategoryRepository.Setup(r => r.GetAsync(id)).ReturnsAsync((AuthenticatorCategory) null);

            var updated = await _authenticatorCategoryService.UpdateManyAsync(new List<AuthenticatorCategory> { ac });

            Assert.Equal(0, updated);
            _authenticatorCategoryRepository.Verify(r => r.UpdateAsync(ac), Times.Never());
        }

        [Fact]
        public async Task UpdateManyAsync_identical()
        {
            var ac = new AuthenticatorCategory { AuthenticatorSecret = "authenticator", CategoryId = "category" };
            var id = new ValueTuple<string, string>("authenticator", "category");

            _authenticatorCategoryRepository.Setup(r => r.GetAsync(id)).ReturnsAsync(ac);
            _equalityComparer.Setup(c => c.Equals(ac, ac)).Returns(true);

            var updated = await _authenticatorCategoryService.UpdateManyAsync(new List<AuthenticatorCategory> { ac });

            Assert.Equal(0, updated);
            _authenticatorCategoryRepository.Verify(r => r.UpdateAsync(ac), Times.Never());
        }

        [Fact]
        public async Task UpdateManyAsync_ok()
        {
            var ac = new AuthenticatorCategory { AuthenticatorSecret = "authenticator", CategoryId = "category" };
            var id = new ValueTuple<string, string>("authenticator", "category");

            _authenticatorCategoryRepository.Setup(r => r.GetAsync(id)).ReturnsAsync(ac);
            _equalityComparer.Setup(c => c.Equals(ac, ac)).Returns(false);

            var updated = await _authenticatorCategoryService.UpdateManyAsync(new List<AuthenticatorCategory> { ac });

            Assert.Equal(1, updated);
            _authenticatorCategoryRepository.Verify(r => r.UpdateAsync(ac));
        }

        [Fact]
        public async Task AddOrUpdateManyAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _authenticatorCategoryService.AddOrUpdateManyAsync(null));
        }

        [Fact]
        public async Task AddOrUpdateManyAsync_ok()
        {
            var ac = new AuthenticatorCategory { AuthenticatorSecret = "authenticator", CategoryId = "category" };
            var id = new ValueTuple<string, string>("authenticator", "category");

            _authenticatorCategoryRepository.Setup(r => r.CreateAsync(ac)).Verifiable();
            _authenticatorCategoryRepository.Setup(r => r.GetAsync(id)).ReturnsAsync(ac);
            _equalityComparer.Setup(c => c.Equals(ac, ac)).Returns(false);

            var (added, updated) = await _authenticatorCategoryService.AddOrUpdateManyAsync(new List<AuthenticatorCategory> { ac });

            Assert.Equal(1, added);
            Assert.Equal(1, updated);
        }

        [Fact]
        public async Task AddAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _authenticatorCategoryService.AddAsync(null, new Category()));
            await Assert.ThrowsAsync<ArgumentException>(() => _authenticatorCategoryService.AddAsync(new Authenticator(), null));
        }

        [Fact]
        public async Task AddAsync_ok()
        {
            var match = new CaptureMatch<AuthenticatorCategory>(ac =>
            {
                Assert.Equal("authenticator", ac.AuthenticatorSecret);
                Assert.Equal("category", ac.CategoryId);
            });

            _authenticatorCategoryRepository.Setup(r => r.CreateAsync(Capture.With(match)));

            var auth = new Authenticator { Secret = "authenticator" };
            var category = new Category { Id = "category" };

            await _authenticatorCategoryService.AddAsync(auth, category);

            _authenticatorCategoryRepository.Verify(r => r.CreateAsync(It.IsAny<AuthenticatorCategory>()));
        }

        [Fact]
        public async Task RemoveAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _authenticatorCategoryService.RemoveAsync(null, new Category()));
            await Assert.ThrowsAsync<ArgumentException>(() => _authenticatorCategoryService.RemoveAsync(new Authenticator(), null));
        }

        [Fact]
        public async Task RemoveAsync_ok()
        {
            var match = new CaptureMatch<AuthenticatorCategory>(ac =>
            {
                Assert.Equal("authenticator", ac.AuthenticatorSecret);
                Assert.Equal("category", ac.CategoryId);
            });

            _authenticatorCategoryRepository.Setup(r => r.DeleteAsync(Capture.With(match)));

            var auth = new Authenticator { Secret = "authenticator" };
            var category = new Category { Id = "category" };

            await _authenticatorCategoryService.RemoveAsync(auth, category);

            _authenticatorCategoryRepository.Verify(r => r.DeleteAsync(It.IsAny<AuthenticatorCategory>()));
        }
    }
}