// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Generator;
using AuthenticatorPro.Core.Persistence;
using AuthenticatorPro.Core.Persistence.Exception;

namespace AuthenticatorPro.Core.Service.Impl
{
    public class AuthenticatorService : IAuthenticatorService
    {
        private readonly IAuthenticatorRepository _authenticatorRepository;
        private readonly IAuthenticatorCategoryRepository _authenticatorCategoryRepository;
        private readonly ICustomIconService _customIconService;
        private readonly IEqualityComparer<Authenticator> _equalityComparer;

        public AuthenticatorService(IAuthenticatorRepository authenticatorRepository,
            IAuthenticatorCategoryRepository authenticatorCategoryRepository,
            ICustomIconService customIconService, IEqualityComparer<Authenticator> equalityComparer)
        {
            _authenticatorRepository = authenticatorRepository;
            _authenticatorCategoryRepository = authenticatorCategoryRepository;
            _customIconService = customIconService;
            _equalityComparer = equalityComparer;
        }

        public async Task AddAsync(Authenticator auth)
        {
            if (auth == null)
            {
                throw new ArgumentNullException(nameof(auth));
            }

            auth.Validate();
            await _authenticatorRepository.CreateAsync(auth);
        }

        public async Task UpdateAsync(Authenticator auth)
        {
            if (auth == null)
            {
                throw new ArgumentNullException(nameof(auth));
            }

            auth.Validate();
            await _authenticatorRepository.UpdateAsync(auth);
        }

        public async Task<int> UpdateManyAsync(IEnumerable<Authenticator> auths)
        {
            if (auths == null)
            {
                throw new ArgumentNullException(nameof(auths));
            }

            var updated = 0;

            foreach (var auth in auths)
            {
                auth.Validate();
                var original = await _authenticatorRepository.GetAsync(auth.Secret);

                if (original == null || _equalityComparer.Equals(original, auth))
                {
                    continue;
                }

                await _authenticatorRepository.UpdateAsync(auth);
                updated++;
            }

            return updated;
        }

        public async Task ChangeSecretAsync(Authenticator auth, string newSecret)
        {
            if (auth == null)
            {
                throw new ArgumentNullException(nameof(auth));
            }

            if (string.IsNullOrEmpty(newSecret))
            {
                throw new ArgumentException("Old secret cannot be null or empty");
            }

            await _authenticatorRepository.ChangeSecretAsync(auth.Secret, newSecret);

            var next = new Authenticator { Secret = newSecret };
            await _authenticatorCategoryRepository.TransferAuthenticatorAsync(auth, next);
        }

        public async Task SetIconAsync(Authenticator auth, string icon)
        {
            if (auth == null)
            {
                throw new ArgumentNullException(nameof(auth));
            }

            if (string.IsNullOrEmpty(icon))
            {
                throw new ArgumentException("Invalid icon");
            }

            auth.Icon = icon;
            await _authenticatorRepository.UpdateAsync(auth);
            await _customIconService.CullUnusedAsync();
        }

        public async Task SetCustomIconAsync(Authenticator auth, CustomIcon icon)
        {
            if (auth == null)
            {
                throw new ArgumentNullException(nameof(auth));
            }

            if (icon == null)
            {
                throw new ArgumentNullException(nameof(icon));
            }

            var iconId = CustomIcon.Prefix + icon.Id;

            if (auth.Icon == iconId)
            {
                return;
            }

            await _customIconService.AddIfNotExistsAsync(icon);
            auth.Icon = iconId;

            try
            {
                await _authenticatorRepository.UpdateAsync(auth);
            }
            finally
            {
                await _customIconService.CullUnusedAsync();
            }
        }

        public async Task<int> AddManyAsync(IEnumerable<Authenticator> auths)
        {
            if (auths == null)
            {
                throw new ArgumentNullException(nameof(auths));
            }

            var added = 0;

            foreach (var auth in auths)
            {
                auth.Validate();

                try
                {
                    await _authenticatorRepository.CreateAsync(auth);
                }
                catch (EntityDuplicateException)
                {
                    continue;
                }

                added++;
            }

            return added;
        }

        public async Task<ValueTuple<int, int>> AddOrUpdateManyAsync(IEnumerable<Authenticator> auths)
        {
            if (auths == null)
            {
                throw new ArgumentNullException(nameof(auths));
            }

            var list = auths.ToList();
            var added = await AddManyAsync(list);
            var updated = await UpdateManyAsync(list);

            return new ValueTuple<int, int>(added, updated);
        }

        public async Task DeleteWithCategoryBindingsAsync(Authenticator auth)
        {
            if (auth == null)
            {
                throw new ArgumentNullException(nameof(auth));
            }

            await _authenticatorRepository.DeleteAsync(auth);
            await _authenticatorCategoryRepository.DeleteAllForAuthenticatorAsync(auth);
        }

        public async Task IncrementCounterAsync(Authenticator auth)
        {
            if (auth == null)
            {
                throw new ArgumentNullException(nameof(auth));
            }

            if (auth.Type.GetGenerationMethod() != GenerationMethod.Counter)
            {
                throw new ArgumentException("Not a counter based authenticator");
            }

            auth.Counter++;
            await _authenticatorRepository.UpdateAsync(auth);
        }

        public async Task IncrementCopyCountAsync(Authenticator auth)
        {
            if (auth == null)
            {
                throw new ArgumentNullException(nameof(auth));
            }

            auth.CopyCount++;
            await _authenticatorRepository.UpdateAsync(auth);
        }

        public async Task ResetCopyCountsAsync()
        {
            var auths = await _authenticatorRepository.GetAllAsync();

            foreach (var auth in auths)
            {
                auth.CopyCount = 0;
                await _authenticatorRepository.UpdateAsync(auth);
            }
        }
    }
}