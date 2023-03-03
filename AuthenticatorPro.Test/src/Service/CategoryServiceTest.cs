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
    public class CategoryServiceTest
    {
        private readonly Mock<ICategoryRepository> _categoryRepository;
        private readonly Mock<IAuthenticatorCategoryRepository> _authenticatorCategoryRepository;
        private readonly Mock<IEqualityComparer<Category>> _equalityComparer;

        private readonly ICategoryService _categoryService;

        public CategoryServiceTest()
        {
            _categoryRepository = new Mock<ICategoryRepository>();
            _authenticatorCategoryRepository = new Mock<IAuthenticatorCategoryRepository>();
            _equalityComparer = new Mock<IEqualityComparer<Category>>();

            _categoryService = new CategoryService(
                _categoryRepository.Object,
                _authenticatorCategoryRepository.Object,
                _equalityComparer.Object);
        }

        [Fact]
        public async Task TransferAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _categoryService.TransferAsync(null, new Category()));
            await Assert.ThrowsAsync<ArgumentException>(() => _categoryService.TransferAsync(new Category(), null));
        }

        [Fact]
        public async Task TransferAsync_ok()
        {
            var initial = new Category();
            var next = new Category();

            _categoryRepository.Setup(r => r.CreateAsync(next)).Verifiable();
            _categoryRepository.Setup(r => r.DeleteAsync(initial)).Verifiable();
            _authenticatorCategoryRepository.Setup(r => r.TransferAsync(initial, next)).Verifiable();

            await _categoryService.TransferAsync(initial, next);

            _categoryRepository.Verify(r => r.CreateAsync(next));
            _categoryRepository.Verify(r => r.DeleteAsync(initial));
            _authenticatorCategoryRepository.Verify(r => r.TransferAsync(initial, next));
        }

        [Fact]
        public async Task AddManyAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _categoryService.AddManyAsync(null));
        }

        [Fact]
        public async Task AddManyAsync_exists()
        {
            var category = new Category();
            _categoryRepository.Setup(r => r.CreateAsync(category)).ThrowsAsync(new EntityDuplicateException());

            var added = await _categoryService.AddManyAsync(new List<Category> { category });

            Assert.Equal(0, added);
        }

        [Fact]
        public async Task AddManyAsync_ok()
        {
            var category = new Category();
            _categoryRepository.Setup(r => r.CreateAsync(category)).Verifiable();

            var added = await _categoryService.AddManyAsync(new List<Category> { category });

            Assert.Equal(1, added);
            _categoryRepository.Verify(r => r.CreateAsync(category));
        }

        [Fact]
        public async Task UpdateManyAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _categoryService.UpdateManyAsync(null));
        }

        [Fact]
        public async Task UpdateManyAsync_doesntExist()
        {
            var category = new Category { Id = "id" };
            _categoryRepository.Setup(r => r.GetAsync("id")).ReturnsAsync((Category) null);

            var updated = await _categoryService.UpdateManyAsync(new List<Category> { category });

            Assert.Equal(0, updated);
            _categoryRepository.Verify(r => r.UpdateAsync(category), Times.Never());
        }

        [Fact]
        public async Task UpdateManyAsync_identical()
        {
            var category = new Category { Id = "id" };
            _categoryRepository.Setup(r => r.GetAsync("id")).ReturnsAsync(category);
            _equalityComparer.Setup(c => c.Equals(category, category)).Returns(true);

            var updated = await _categoryService.UpdateManyAsync(new List<Category> { category });

            Assert.Equal(0, updated);
            _categoryRepository.Verify(r => r.UpdateAsync(category), Times.Never());
        }

        [Fact]
        public async Task UpdateManyAsync_ok()
        {
            var category = new Category { Id = "id" };
            _categoryRepository.Setup(r => r.GetAsync("id")).ReturnsAsync(category);
            _equalityComparer.Setup(c => c.Equals(category, category)).Returns(false);

            _categoryRepository.Setup(r => r.GetAsync("id")).ReturnsAsync(category);

            var updated = await _categoryService.UpdateManyAsync(new List<Category> { category });

            Assert.Equal(1, updated);
            _categoryRepository.Verify(r => r.UpdateAsync(category));
        }

        [Fact]
        public async Task AddOrUpdateManyAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _categoryService.AddOrUpdateManyAsync(null));
        }

        [Fact]
        public async Task AddOrUpdateManyAsync_ok()
        {
            var category = new Category { Id = "id" };

            _categoryRepository.Setup(r => r.CreateAsync(category)).Verifiable();
            _categoryRepository.Setup(r => r.GetAsync("id")).ReturnsAsync(category);
            _equalityComparer.Setup(c => c.Equals(category, category)).Returns(false);

            var (added, updated) = await _categoryService.AddOrUpdateManyAsync(new List<Category> { category });

            Assert.Equal(1, added);
            Assert.Equal(1, updated);
        }

        [Fact]
        public async Task DeleteWithCategoryBindingsAsync_null()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _categoryService.DeleteWithCategoryBindingsASync(null));
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
    }
}