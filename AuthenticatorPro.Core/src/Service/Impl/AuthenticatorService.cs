// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Comparer;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Generator;
using AuthenticatorPro.Core.Persistence;
using AuthenticatorPro.Core.Persistence.Exception;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthenticatorPro.Core.Service.Impl
{
    public class AuthenticatorService : IAuthenticatorService
    {
        private readonly IAuthenticatorRepository _authenticatorRepository;
        private readonly IAuthenticatorCategoryRepository _authenticatorCategoryRepository;
        private readonly IIconResolver _iconResolver;
        private readonly ICustomIconService _customIconService;
        private readonly ICustomIconRepository _customIconRepository;

        public AuthenticatorService(IAuthenticatorRepository authenticatorRepository,
            IAuthenticatorCategoryRepository authenticatorCategoryRepository,
            IIconResolver iconResolver, ICustomIconService customIconService,
            ICustomIconRepository customIconRepository)
        {
            _authenticatorRepository = authenticatorRepository;
            _authenticatorCategoryRepository = authenticatorCategoryRepository;
            _iconResolver = iconResolver;
            _customIconService = customIconService;
            _customIconRepository = customIconRepository;
        }

        public async Task AddAsync(Authenticator auth)
        {
            auth.Validate();
            await _authenticatorRepository.CreateAsync(auth);
        }

        public async Task<int> UpdateManyAsync(IEnumerable<Authenticator> auths)
        {
            if (auths == null)
            {
                return 0;
            }

            var updated = 0;
            var comparer = new AuthenticatorComparer();

            foreach (var auth in auths)
            {
                auth.Validate();
                var original = await _authenticatorRepository.GetAsync(auth.Secret);

                if (original == null || comparer.Equals(original, auth))
                {
                    continue;
                }

                await _authenticatorRepository.UpdateAsync(auth);
                updated++;
            }

            return updated;
        }

        public async Task RenameAsync(Authenticator auth, string issuer, string username)
        {
            if (String.IsNullOrEmpty(issuer))
            {
                throw new ArgumentException("Issuer cannot be null or empty");
            }

            auth.Issuer = issuer;
            auth.Username = username;
            auth.Icon ??= _iconResolver.FindServiceKeyByName(issuer);

            await _authenticatorRepository.UpdateAsync(auth);
        }

        public async Task SetIconAsync(Authenticator auth, string icon)
        {
            if (String.IsNullOrEmpty(icon))
            {
                throw new ArgumentException("Invalid icon");
            }

            auth.Icon = icon;
            await _authenticatorRepository.UpdateAsync(auth);
            await _customIconService.CullUnused();
        }

        public async Task SetCustomIconAsync(Authenticator auth, CustomIcon icon)
        {
            if (icon == null)
            {
                throw new ArgumentException("Icon cannot be null");
            }

            var iconId = CustomIcon.Prefix + icon.Id;

            if (auth.Icon == iconId)
            {
                return;
            }

            await _customIconService.AddIfNotExists(icon);
            auth.Icon = iconId;

            try
            {
                await _authenticatorRepository.UpdateAsync(auth);
            }
            catch
            {
                await _customIconRepository.DeleteAsync(icon);
                throw;
            }

            await _customIconService.CullUnused();
        }

        public async Task<int> AddManyAsync(IEnumerable<Authenticator> auths)
        {
            if (auths == null)
            {
                return 0;
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
            var list = auths.ToList();
            var added = await AddManyAsync(list);
            var updated = await UpdateManyAsync(list);

            return new ValueTuple<int, int>(added, updated);
        }

        public async Task DeleteWithCategoryBindingsAsync(Authenticator auth)
        {
            await _authenticatorRepository.DeleteAsync(auth);
            await _authenticatorCategoryRepository.DeleteAllForAuthenticatorAsync(auth);
        }

        public async Task IncrementCounterAsync(Authenticator auth)
        {
            if (auth.Type.GetGenerationMethod() != GenerationMethod.Counter)
            {
                throw new ArgumentException("Not a counter based authenticator");
            }

            auth.Counter++;
            await _authenticatorRepository.UpdateAsync(auth);
        }
    }
}