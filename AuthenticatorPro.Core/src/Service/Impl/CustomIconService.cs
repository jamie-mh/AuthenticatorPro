// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Persistence;
using AuthenticatorPro.Core.Persistence.Exception;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthenticatorPro.Core.Service.Impl
{
    public class CustomIconService : ICustomIconService
    {
        private readonly ICustomIconRepository _customIconRepository;
        private readonly IAuthenticatorRepository _authenticatorRepository;

        public CustomIconService(ICustomIconRepository customIconRepository,
            IAuthenticatorRepository authenticatorRepository)
        {
            _customIconRepository = customIconRepository;
            _authenticatorRepository = authenticatorRepository;
        }

        public async Task AddIfNotExists(CustomIcon icon)
        {
            if (icon == null)
            {
                throw new ArgumentNullException(nameof(icon));
            }

            var existing = await _customIconRepository.GetAsync(icon.Id);

            if (existing == null)
            {
                await _customIconRepository.CreateAsync(icon);
            }
        }

        public async Task<int> AddManyAsync(IEnumerable<CustomIcon> icons)
        {
            if (icons == null)
            {
                throw new ArgumentNullException(nameof(icons));
            }

            var added = 0;

            foreach (var icon in icons)
            {
                try
                {
                    await _customIconRepository.CreateAsync(icon);
                }
                catch (EntityDuplicateException)
                {
                    continue;
                }

                added++;
            }

            return added;
        }

        public async Task CullUnused()
        {
            var authenticators = await _authenticatorRepository.GetAllAsync();
            var icons = await _customIconRepository.GetAllAsync();

            var iconsInUse = authenticators
                .Where(a => a.Icon != null && a.Icon.StartsWith(CustomIcon.Prefix))
                .Select(a => a.Icon[1..])
                .Distinct();

            var unusedIcons = icons.Where(i => !iconsInUse.Contains(i.Id));

            foreach (var icon in unusedIcons)
            {
                await _customIconRepository.DeleteAsync(icon);
            }
        }
    }
}