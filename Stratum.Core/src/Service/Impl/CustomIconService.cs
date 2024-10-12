// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stratum.Core.Entity;
using Stratum.Core.Persistence;
using Stratum.Core.Persistence.Exception;

namespace Stratum.Core.Service.Impl
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

        public async Task AddIfNotExistsAsync(CustomIcon icon)
        {
            ArgumentNullException.ThrowIfNull(icon);
            var existing = await _customIconRepository.GetAsync(icon.Id);

            if (existing == null)
            {
                await _customIconRepository.CreateAsync(icon);
            }
        }

        public async Task<int> AddManyAsync(IEnumerable<CustomIcon> icons)
        {
            ArgumentNullException.ThrowIfNull(icons);

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

        public Task<List<CustomIcon>> GetAllAsync()
        {
            return _customIconRepository.GetAllAsync();
        }

        public async Task CullUnusedAsync()
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