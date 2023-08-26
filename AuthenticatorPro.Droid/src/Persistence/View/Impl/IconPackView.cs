// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Persistence;

namespace AuthenticatorPro.Droid.Persistence.View.Impl
{
    public class IconPackView : IIconPackView
    {
        private readonly IIconPackRepository _iconPackRepository;
        private List<IconPack> _all;

        public IconPackView(IIconPackRepository iconPackRepository)
        {
            _iconPackRepository = iconPackRepository;
            _all = new List<IconPack>();
        }

        public void Update()
        {
            _all = _all.OrderBy(p => p.Name).ToList();
        }

        public async Task LoadFromPersistenceAsync()
        {
            _all = await _iconPackRepository.GetAllAsync();
            Update();
        }

        public int IndexOf(string name)
        {
            return _all.FindIndex(c => c.Name == name);
        }

        public IEnumerator<IconPack> GetEnumerator()
        {
            return _all.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => _all.Count;

        public IconPack this[int index] => _all[index];
    }
}