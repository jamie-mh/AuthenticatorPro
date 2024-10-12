// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Threading.Tasks;
using Stratum.Core.Backup;
using Stratum.Core.Persistence;

namespace Stratum.Core.Service.Impl
{
    public class BackupService : IBackupService
    {
        private readonly IAuthenticatorRepository _authenticatorRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IAuthenticatorCategoryRepository _authenticatorCategoryRepository;
        private readonly ICustomIconRepository _customIconRepository;
        private readonly IAssetProvider _assetProvider;

        public BackupService(IAuthenticatorRepository authenticatorRepository, ICategoryRepository categoryRepository,
            IAuthenticatorCategoryRepository authenticatorCategoryRepository,
            ICustomIconRepository customIconRepository, IAssetProvider assetProvider)
        {
            _authenticatorRepository = authenticatorRepository;
            _categoryRepository = categoryRepository;
            _authenticatorCategoryRepository = authenticatorCategoryRepository;
            _customIconRepository = customIconRepository;
            _assetProvider = assetProvider;
        }

        public async Task<Backup.Backup> CreateBackupAsync()
        {
            return new Backup.Backup
            {
                Authenticators = await _authenticatorRepository.GetAllAsync(),
                Categories = await _categoryRepository.GetAllAsync(),
                AuthenticatorCategories = await _authenticatorCategoryRepository.GetAllAsync(),
                CustomIcons = await _customIconRepository.GetAllAsync()
            };
        }

        public async Task<HtmlBackup> CreateHtmlBackupAsync()
        {
            var auths = await _authenticatorRepository.GetAllAsync();
            return await HtmlBackup.FromAuthenticators(_assetProvider, auths);
        }

        public async Task<UriListBackup> CreateUriListBackupAsync()
        {
            var auths = await _authenticatorRepository.GetAllAsync();
            return UriListBackup.FromAuthenticators(auths);
        }
    }
}