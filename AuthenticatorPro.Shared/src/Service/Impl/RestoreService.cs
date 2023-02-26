// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Backup;
using System.Threading.Tasks;

namespace AuthenticatorPro.Shared.Service.Impl
{
    public class RestoreService : IRestoreService
    {
        private readonly IAuthenticatorService _authenticatorService;
        private readonly ICategoryService _categoryService;
        private readonly IAuthenticatorCategoryService _authenticatorCategoryService;
        private readonly ICustomIconService _customIconService;

        public RestoreService(IAuthenticatorService authenticatorService, ICategoryService categoryService,
            IAuthenticatorCategoryService authenticatorCategoryService,
            ICustomIconService customIconService)
        {
            _authenticatorService = authenticatorService;
            _categoryService = categoryService;
            _authenticatorCategoryService = authenticatorCategoryService;
            _customIconService = customIconService;
        }

        public async Task<RestoreResult> RestoreAsync(Backup.Backup backup)
        {
            return new RestoreResult
            {
                AddedAuthenticatorCount = await _authenticatorService.AddManyAsync(backup.Authenticators),
                AddedCategoryCount = await _categoryService.AddManyAsync(backup.Categories),
                AddedAuthenticatorCategoryCount =
                    await _authenticatorCategoryService.AddManyAsync(backup.AuthenticatorCategories),
                AddedCustomIconCount = await _customIconService.AddManyAsync(backup.CustomIcons)
            };
        }

        public async Task<RestoreResult> RestoreAndUpdateAsync(Backup.Backup backup)
        {
            var (authsAdded, authsUpdated) = await _authenticatorService.AddOrUpdateManyAsync(backup.Authenticators);
            var result = new RestoreResult
            {
                AddedAuthenticatorCount = authsAdded, UpdatedAuthenticatorCount = authsUpdated
            };

            if (backup.Categories != null)
            {
                var (added, updated) = await _categoryService.AddOrUpdateManyAsync(backup.Categories);
                result.AddedCategoryCount = added;
                result.UpdatedCategoryCount = updated;
            }

            if (backup.AuthenticatorCategories != null)
            {
                var (added, updated) =
                    await _authenticatorCategoryService.AddOrUpdateManyAsync(backup.AuthenticatorCategories);
                result.AddedAuthenticatorCategoryCount = added;
                result.UpdatedAuthenticatorCategoryCount = updated;
            }

            if (backup.CustomIcons != null)
            {
                result.AddedCustomIconCount = await _customIconService.AddManyAsync(backup.CustomIcons);
            }

            return result;
        }
    }
}