// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

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
        private readonly IEqualityComparer<Authenticator> _equalityComparer;

        public AuthenticatorService(IAuthenticatorRepository authenticatorRepository,
            IAuthenticatorCategoryRepository authenticatorCategoryRepository,
            IIconResolver iconResolver, ICustomIconService customIconService,
            IEqualityComparer<Authenticator> equalityComparer)
        {
            _authenticatorRepository = authenticatorRepository;
            _authenticatorCategoryRepository = authenticatorCategoryRepository;
            _iconResolver = iconResolver;
            _customIconService = customIconService;
            _equalityComparer = equalityComparer;
        }

        public async Task AddAsync(Authenticator auth)
        {
            if (auth == null)
            {
                throw new ArgumentException("Authenticator cannot be null");
            }

            auth.Validate();
            await _authenticatorRepository.CreateAsync(auth);
        }

        public async Task<int> UpdateManyAsync(IEnumerable<Authenticator> auths)
        {
            if (auths == null)
            {
                throw new ArgumentException("Authenticators cannot be null");
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

        public async Task RenameAsync(Authenticator auth, string issuer, string username)
        {
            if (auth == null)
            {
                throw new ArgumentException("Authenticator cannot be null");
            }

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
            if (auth == null)
            {
                throw new ArgumentException("Authenticator cannot be null");
            }

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
            if (auth == null)
            {
                throw new ArgumentException("Authenticator cannot be null");
            }

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
            finally
            {
                await _customIconService.CullUnused();
            }
        }

        public async Task<int> AddManyAsync(IEnumerable<Authenticator> auths)
        {
            if (auths == null)
            {
                throw new ArgumentException("Authenticators cannot be null");
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
                throw new ArgumentException("Authenticators cannot be null");
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
                throw new ArgumentException("Authenticator cannot be null");
            }

            await _authenticatorRepository.DeleteAsync(auth);
            await _authenticatorCategoryRepository.DeleteAllForAuthenticatorAsync(auth);
        }

        public async Task IncrementCounterAsync(Authenticator auth)
        {
            if (auth == null)
            {
                throw new ArgumentException("Authenticator cannot be null");
            }

            if (auth.Type.GetGenerationMethod() != GenerationMethod.Counter)
            {
                throw new ArgumentException("Not a counter based authenticator");
            }

            auth.Counter++;
            await _authenticatorRepository.UpdateAsync(auth);
        }
    }
}