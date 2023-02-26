// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Backup;
using AuthenticatorPro.Shared.Comparer;
using AuthenticatorPro.Shared.Persistence;
using AuthenticatorPro.Shared.Service;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AuthenticatorPro.Test.ServiceTest
{
    public class RestoreServiceTest : DataTest
    {
        private readonly IRestoreService _restoreService;

        private readonly Backup _backup;

        public RestoreServiceTest(IAuthenticatorRepository authenticatorRepository,
            IAuthenticatorCategoryRepository authenticatorCategoryRepository, ICategoryRepository categoryRepository,
            ICustomIconRepository customIconRepository, IRestoreService restoreService, Backup backup)
            : base(authenticatorRepository, categoryRepository, authenticatorCategoryRepository, customIconRepository)
        {
            _restoreService = restoreService;
            _backup = backup;
        }

        private async Task AssertDatabaseMatchesBackupAsync()
        {
            var authenticators = await AuthenticatorRepository.GetAllAsync();
            Assert.False(_backup.Authenticators.Except(authenticators, new AuthenticatorComparer()).Any());

            var categories = await CategoryRepository.GetAllAsync();
            Assert.False(_backup.Categories.Except(categories, new CategoryComparer()).Any());

            var authenticatorCategories = await AuthenticatorCategoryRepository.GetAllAsync();
            Assert.False(_backup.AuthenticatorCategories
                .Except(authenticatorCategories, new AuthenticatorCategoryComparer()).Any());

            var customIcons = await CustomIconRepository.GetAllAsync();
            Assert.False(_backup.CustomIcons.Except(customIcons, new CustomIconComparer()).Any());
        }

        [Fact]
        public async Task RestoreAsyncTest()
        {
            await _restoreService.RestoreAsync(_backup);
            await AssertDatabaseMatchesBackupAsync();
        }

        [Fact]
        public async Task RestoreAndUpdateAsyncTest()
        {
            await _restoreService.RestoreAndUpdateAsync(_backup);
            await AssertDatabaseMatchesBackupAsync();

            var authenticators = await AuthenticatorRepository.GetAllAsync();

            for (var i = 0; i < authenticators.Count; i++)
            {
                var authenticator = authenticators[i];
                authenticator.Issuer = "test";
                await AuthenticatorRepository.UpdateAsync(authenticator);
            }

            var categories = await CategoryRepository.GetAllAsync();

            for (var i = 0; i < categories.Count; i++)
            {
                var category = categories[i];
                category.Name = "test";
                await CategoryRepository.UpdateAsync(category);
            }

            await _restoreService.RestoreAndUpdateAsync(_backup);

            authenticators = await AuthenticatorRepository.GetAllAsync();
            Assert.True(authenticators.All(a => a.Issuer != "test"));

            categories = await CategoryRepository.GetAllAsync();
            Assert.True(categories.All(a => a.Name != "test"));
        }
    }
}