// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Threading.Tasks;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Persistence;

namespace AuthenticatorPro.Core.Service.Impl
{
    public class IconPackService : IIconPackService
    {
        private readonly IIconPackRepository _iconPackRepository;
        private readonly IIconPackEntryRepository _iconPackEntryRepository;

        public IconPackService(IIconPackRepository iconPackRepository, IIconPackEntryRepository iconPackEntryRepository)
        {
            _iconPackRepository = iconPackRepository;
            _iconPackEntryRepository = iconPackEntryRepository;
        }

        public async Task ImportPackAsync(IconPack pack)
        {
            if (pack == null)
            {
                throw new ArgumentNullException(nameof(pack));
            }

            if (pack.Icons == null)
            {
                throw new ArgumentException("Pack must contain icons");
            }

            var existingPack = await _iconPackRepository.GetAsync(pack.Name);

            if (existingPack == null)
            {
                await _iconPackRepository.CreateAsync(pack);
            }
            else
            {
                await _iconPackRepository.UpdateAsync(pack);
                await _iconPackEntryRepository.DeleteAllForPackAsync(pack);
            }

            await Parallel.ForEachAsync(pack.Icons, async (entry, _) =>
            {
                entry.IconPackName = pack.Name;
                await _iconPackEntryRepository.CreateAsync(entry);
            });
        }

        public async Task DeletePackAsync(IconPack pack)
        {
            if (pack == null)
            {
                throw new ArgumentNullException(nameof(pack));
            }

            await _iconPackEntryRepository.DeleteAllForPackAsync(pack);
            await _iconPackRepository.DeleteAsync(pack);
        }
    }
}