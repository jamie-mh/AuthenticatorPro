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
    public class CategoryServiceTest
    {
        private readonly Mock<ICategoryRepository> _categoryRepository;
        private readonly Mock<IAuthenticatorCategoryRepository> _authenticatorCategoryRepository;
        private readonly Mock<IEqualityComparer<Category>> _categoryComparer;
        private readonly Mock<IEqualityComparer<AuthenticatorCategory>> _authenticatorCategoryComparer;

        private readonly ICategoryService _categoryService;

        public CategoryServiceTest()
        {
            _categoryRepository = new Mock<ICategoryRepository>();
            _authenticatorCategoryRepository = new Mock<IAuthenticatorCategoryRepository>();
            _categoryComparer = new Mock<IEqualityComparer<Category>>();
            _authenticatorCategoryComparer = new Mock<IEqualityComparer<AuthenticatorCategory>>();

            _categoryService = new CategoryService(
                _categoryRepository.Object,
                _authenticatorCategoryRepository.Object,
                _categoryComparer.Object,
                _authenticatorCategoryComparer.Object);
        }

        [Fact]
        public async Task GetCategoryByIdAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _categoryService.GetCategoryByIdAsync(null));
        }

        [Fact]
        public async Task GetCategoryByIdAsync_ok()
        {
            var category = new Category();
            _categoryRepository.Setup(r => r.GetAsync("id")).ReturnsAsync(category);
            Assert.Equal(category, await _categoryService.GetCategoryByIdAsync("id"));
        }

        [Fact]
        public async Task TransferAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _categoryService.TransferAsync(null, new Category()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _categoryService.TransferAsync(new Category(), null));
        }

        [Fact]
        public async Task TransferAsync_ok()
        {
            var initial = new Category();
            var next = new Category();

            _categoryRepository.Setup(r => r.CreateAsync(next)).Verifiable();
            _categoryRepository.Setup(r => r.DeleteAsync(initial)).Verifiable();
            _authenticatorCategoryRepository.Setup(r => r.TransferCategoryAsync(initial, next)).Verifiable();

            await _categoryService.TransferAsync(initial, next);

            _categoryRepository.Verify(r => r.CreateAsync(next));
            _categoryRepository.Verify(r => r.DeleteAsync(initial));
            _authenticatorCategoryRepository.Verify(r => r.TransferCategoryAsync(initial, next));
        }

        [Fact]
        public async Task AddCategoryAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _categoryService.AddCategoryAsync(null));
        }

        [Fact]
        public async Task AddCategoryAsync_ok()
        {
            var category = new Category();
            _categoryRepository.Setup(r => r.CreateAsync(category)).Verifiable();

            await _categoryService.AddCategoryAsync(category);

            _categoryRepository.Verify(r => r.CreateAsync(category));
        }

        [Fact]
        public async Task AddManyCategoriesAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _categoryService.AddManyCategoriesAsync(null));
        }

        [Fact]
        public async Task AddManyCategoriesAsync_exists()
        {
            var category = new Category();
            _categoryRepository.Setup(r => r.CreateAsync(category)).ThrowsAsync(new EntityDuplicateException());

            var added = await _categoryService.AddManyCategoriesAsync(new List<Category> { category });

            Assert.Equal(0, added);
        }

        [Fact]
        public async Task AddManyCategoriesAsync_ok()
        {
            var category = new Category();
            _categoryRepository.Setup(r => r.CreateAsync(category)).Verifiable();

            var added = await _categoryService.AddManyCategoriesAsync(new List<Category> { category });

            Assert.Equal(1, added);
            _categoryRepository.Verify(r => r.CreateAsync(category));
        }

        [Fact]
        public async Task UpdateManyCategoriesAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _categoryService.UpdateManyCategoriesAsync(null));
        }

        [Fact]
        public async Task UpdateManyCategoriesAsync_doesntExist()
        {
            var category = new Category { Id = "id" };
            _categoryRepository.Setup(r => r.GetAsync("id")).ReturnsAsync((Category) null);

            var updated = await _categoryService.UpdateManyCategoriesAsync(new List<Category> { category });

            Assert.Equal(0, updated);
            _categoryRepository.Verify(r => r.UpdateAsync(category), Times.Never());
        }

        [Fact]
        public async Task UpdateManyCategoriesAsync_identical()
        {
            var category = new Category { Id = "id" };
            _categoryRepository.Setup(r => r.GetAsync("id")).ReturnsAsync(category);
            _categoryComparer.Setup(c => c.Equals(category, category)).Returns(true);

            var updated = await _categoryService.UpdateManyCategoriesAsync(new List<Category> { category });

            Assert.Equal(0, updated);
            _categoryRepository.Verify(r => r.UpdateAsync(category), Times.Never());
        }

        [Fact]
        public async Task UpdateManyAsync_ok()
        {
            var category = new Category { Id = "id" };
            _categoryRepository.Setup(r => r.GetAsync("id")).ReturnsAsync(category);
            _categoryComparer.Setup(c => c.Equals(category, category)).Returns(false);

            _categoryRepository.Setup(r => r.GetAsync("id")).ReturnsAsync(category);

            var updated = await _categoryService.UpdateManyCategoriesAsync(new List<Category> { category });

            Assert.Equal(1, updated);
            _categoryRepository.Verify(r => r.UpdateAsync(category));
        }

        [Fact]
        public async Task AddOrUpdateManyCategoriesAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _categoryService.AddOrUpdateManyCategoriesAsync(null));
        }

        [Fact]
        public async Task AddOrUpdateManyCategoriesAsync_ok()
        {
            var category = new Category { Id = "id" };

            _categoryRepository.Setup(r => r.CreateAsync(category)).Verifiable();
            _categoryRepository.Setup(r => r.GetAsync("id")).ReturnsAsync(category);
            _categoryComparer.Setup(c => c.Equals(category, category)).Returns(false);

            var (added, updated) =
                await _categoryService.AddOrUpdateManyCategoriesAsync(new List<Category> { category });

            Assert.Equal(1, added);
            Assert.Equal(1, updated);
        }

        [Fact]
        public async Task DeleteWithCategoryBindingsAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _categoryService.DeleteWithCategoryBindingsASync(null));
        }

        [Fact]
        public async Task DeleteWithCategoryBindingsAsync_ok()
        {
            var category = new Category();

            _categoryRepository.Setup(r => r.DeleteAsync(category)).Verifiable();
            _authenticatorCategoryRepository.Setup(r => r.DeleteAllForCategoryAsync(category)).Verifiable();

            await _categoryService.DeleteWithCategoryBindingsASync(category);

            _categoryRepository.Verify(r => r.DeleteAsync(category));
            _authenticatorCategoryRepository.Verify(r => r.DeleteAllForCategoryAsync(category));
        }

        [Fact]
        public async Task AddManyBindingsAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _categoryService.AddManyBindingsAsync(null));
        }

        [Fact]
        public async Task AddManyBindingsAsync_exists()
        {
            var ac = new AuthenticatorCategory();
            _authenticatorCategoryRepository.Setup(r => r.CreateAsync(ac)).ThrowsAsync(new EntityDuplicateException());

            var added = await _categoryService.AddManyBindingsAsync(new List<AuthenticatorCategory> { ac });

            Assert.Equal(0, added);
        }

        [Fact]
        public async Task AddManyBindingsAsync_ok()
        {
            var ac = new AuthenticatorCategory();

            _authenticatorCategoryRepository.Setup(r => r.CreateAsync(ac)).Verifiable();

            var added = await _categoryService.AddManyBindingsAsync(new List<AuthenticatorCategory> { ac });

            Assert.Equal(1, added);
            _authenticatorCategoryRepository.Verify(r => r.CreateAsync(ac));
        }

        [Fact]
        public async Task UpdateManyBindingsAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _categoryService.UpdateManyBindingsAsync(null));
        }

        [Fact]
        public async Task UpdateManyBindingsAsync_doesntExist()
        {
            var ac = new AuthenticatorCategory { AuthenticatorSecret = "authenticator", CategoryId = "category" };
            var id = new ValueTuple<string, string>("authenticator", "category");

            _authenticatorCategoryRepository.Setup(r => r.GetAsync(id)).ReturnsAsync((AuthenticatorCategory) null);

            var updated = await _categoryService.UpdateManyBindingsAsync(new List<AuthenticatorCategory> { ac });

            Assert.Equal(0, updated);
            _authenticatorCategoryRepository.Verify(r => r.UpdateAsync(ac), Times.Never());
        }

        [Fact]
        public async Task UpdateManyBindingsAsync_identical()
        {
            var ac = new AuthenticatorCategory { AuthenticatorSecret = "authenticator", CategoryId = "category" };
            var id = new ValueTuple<string, string>("authenticator", "category");

            _authenticatorCategoryRepository.Setup(r => r.GetAsync(id)).ReturnsAsync(ac);
            _authenticatorCategoryComparer.Setup(c => c.Equals(ac, ac)).Returns(true);

            var updated = await _categoryService.UpdateManyBindingsAsync(new List<AuthenticatorCategory> { ac });

            Assert.Equal(0, updated);
            _authenticatorCategoryRepository.Verify(r => r.UpdateAsync(ac), Times.Never());
        }

        [Fact]
        public async Task UpdateManyBindingsAsync_ok()
        {
            var ac = new AuthenticatorCategory { AuthenticatorSecret = "authenticator", CategoryId = "category" };
            var id = new ValueTuple<string, string>("authenticator", "category");

            _authenticatorCategoryRepository.Setup(r => r.GetAsync(id)).ReturnsAsync(ac);
            _authenticatorCategoryComparer.Setup(c => c.Equals(ac, ac)).Returns(false);

            var updated = await _categoryService.UpdateManyBindingsAsync(new List<AuthenticatorCategory> { ac });

            Assert.Equal(1, updated);
            _authenticatorCategoryRepository.Verify(r => r.UpdateAsync(ac));
        }

        [Fact]
        public async Task AddOrUpdateManyBindingsAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _categoryService.AddOrUpdateManyBindingsAsync(null));
        }

        [Fact]
        public async Task AddOrUpdateManyBindingsAsync_ok()
        {
            var ac = new AuthenticatorCategory { AuthenticatorSecret = "authenticator", CategoryId = "category" };
            var id = new ValueTuple<string, string>("authenticator", "category");

            _authenticatorCategoryRepository.Setup(r => r.CreateAsync(ac)).Verifiable();
            _authenticatorCategoryRepository.Setup(r => r.GetAsync(id)).ReturnsAsync(ac);
            _authenticatorCategoryComparer.Setup(c => c.Equals(ac, ac)).Returns(false);

            var (added, updated) =
                await _categoryService.AddOrUpdateManyBindingsAsync(new List<AuthenticatorCategory> { ac });

            Assert.Equal(1, added);
            Assert.Equal(1, updated);
        }

        [Fact]
        public async Task AddBindingAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _categoryService.AddBindingAsync(null, new Category()));
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _categoryService.AddBindingAsync(new Authenticator(), null));
        }

        [Fact]
        public async Task AddBindingAsync_ok()
        {
            var match = new CaptureMatch<AuthenticatorCategory>(ac =>
            {
                Assert.Equal("authenticator", ac.AuthenticatorSecret);
                Assert.Equal("category", ac.CategoryId);
            });

            _authenticatorCategoryRepository.Setup(r => r.CreateAsync(Capture.With(match)));

            var auth = new Authenticator { Secret = "authenticator" };
            var category = new Category { Id = "category" };

            await _categoryService.AddBindingAsync(auth, category);

            _authenticatorCategoryRepository.Verify(r => r.CreateAsync(It.IsAny<AuthenticatorCategory>()));
        }

        [Fact]
        public async Task RemoveBindingAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _categoryService.RemoveBindingAsync(null, new Category()));
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _categoryService.RemoveBindingAsync(new Authenticator(), null));
        }

        [Fact]
        public async Task RemoveBindingAsync_ok()
        {
            var match = new CaptureMatch<AuthenticatorCategory>(ac =>
            {
                Assert.Equal("authenticator", ac.AuthenticatorSecret);
                Assert.Equal("category", ac.CategoryId);
            });

            _authenticatorCategoryRepository.Setup(r => r.DeleteAsync(Capture.With(match)));

            var auth = new Authenticator { Secret = "authenticator" };
            var category = new Category { Id = "category" };

            await _categoryService.RemoveBindingAsync(auth, category);

            _authenticatorCategoryRepository.Verify(r => r.DeleteAsync(It.IsAny<AuthenticatorCategory>()));
        }

        [Fact]
        public async Task GetBindingsForAuthenticatorAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _categoryService.GetBindingsForAuthenticatorAsync(null));
        }

        [Fact]
        public async Task GetBindingsForAuthenticatorAsync_ok()
        {
            var bindings = new List<AuthenticatorCategory> { new() };
            var auth = new Authenticator();
            _authenticatorCategoryRepository.Setup(r => r.GetAllForAuthenticatorAsync(auth)).ReturnsAsync(bindings);
            Assert.Equal(bindings, await _categoryService.GetBindingsForAuthenticatorAsync(auth));
        }

        [Fact]
        public async Task GetBindingsForCategoryAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _categoryService.GetBindingsForCategoryAsync(null));
        }

        [Fact]
        public async Task GetBindingsForCategoryAsync_ok()
        {
            var bindings = new List<AuthenticatorCategory> { new() };
            var category = new Category();
            _authenticatorCategoryRepository.Setup(r => r.GetAllForCategoryAsync(category)).ReturnsAsync(bindings);
            Assert.Equal(bindings, await _categoryService.GetBindingsForCategoryAsync(category));
        }

        [Fact]
        public async Task GetAllCategoriesAsync()
        {
            var categories = new List<Category> { new() };
            _categoryRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);
            Assert.Equal(categories, await _categoryService.GetAllCategoriesAsync());
        }

        [Fact]
        public async Task GetAllBindingsAsync()
        {
            var bindings = new List<AuthenticatorCategory> { new() };
            _authenticatorCategoryRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(bindings);
            Assert.Equal(bindings, await _categoryService.GetAllBindingsAsync());
        }
    }
}