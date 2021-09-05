// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Entity;
using AuthenticatorPro.Shared.Persistence;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthenticatorPro.Shared.Service.Impl
{
    public class CustomIconService : ICustomIconService
    {
        private readonly ICustomIconRepository _customIconRepository;

        public CustomIconService(ICustomIconRepository customIconRepository)
        {
            _customIconRepository = customIconRepository;
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

        public Task CullUnused()
        {
            throw new NotImplementedException();
        }
    }
}