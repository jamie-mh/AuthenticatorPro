// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Backup;
using AuthenticatorPro.Shared.Persistence;
using System.Threading.Tasks;

namespace AuthenticatorPro.Shared.Service.Impl
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
            return new Backup.Backup(
                await _authenticatorRepository.GetAllAsync(),
                await _categoryRepository.GetAllAsync(),
                await _authenticatorCategoryRepository.GetAllAsync(),
                await _customIconRepository.GetAllAsync()
            );
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