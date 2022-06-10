// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Entity;
using AuthenticatorPro.Shared.Persistence;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthenticatorPro.Shared.Service.Impl
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
                return 0;
            }

            var added = 0;

            foreach (var icon in icons)
            {
                var existing = await _customIconRepository.GetAsync(icon.Id);

                if (existing != null)
                {
                    continue;
                }

                await _customIconRepository.CreateAsync(icon);
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