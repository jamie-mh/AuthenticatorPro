// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Threading.Tasks;
using Stratum.Core.Entity;
using Stratum.Core.Persistence;

namespace Stratum.Core.Service.Impl
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
            ArgumentNullException.ThrowIfNull(pack);
            
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

            foreach (var entry in pack.Icons)
            {
                entry.IconPackName = pack.Name;
            }

            await _iconPackEntryRepository.CreateManyAsync(pack.Icons);
        }

        public async Task DeletePackAsync(IconPack pack)
        {
            ArgumentNullException.ThrowIfNull(pack);
            await _iconPackEntryRepository.DeleteAllForPackAsync(pack);
            await _iconPackRepository.DeleteAsync(pack);
        }
    }
}