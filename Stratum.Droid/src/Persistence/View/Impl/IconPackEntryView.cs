// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Graphics;
using Stratum.Core.Entity;
using Stratum.Core.Persistence;

namespace Stratum.Droid.Persistence.View.Impl
{
    public class IconPackEntryView : IIconPackEntryView
    {
        private readonly IIconPackEntryRepository _iconPackEntryRepository;

        private Dictionary<string, Bitmap> _all;
        private Dictionary<string, Bitmap> _view;

        private string _search;

        public IconPackEntryView(IIconPackEntryRepository iconPackEntryRepository)
        {
            _iconPackEntryRepository = iconPackEntryRepository;
            _all = new Dictionary<string, Bitmap>();
            _view = new Dictionary<string, Bitmap>();
        }

        public string Search
        {
            get => _search;
            set
            {
                _search = value;
                Update();
            }
        }

        public void Update()
        {
            if (!string.IsNullOrEmpty(_search))
            {
                var query = _search.ToLower();

                _view = _all
                    .Where(e => e.Key.Contains(query))
                    .OrderBy(e => e.Key)
                    .ToDictionary(k => k.Key, v => v.Value);
            }
            else
            {
                _view = new Dictionary<string, Bitmap>(_all.OrderBy(e => e.Key));
            }
        }

        public async Task LoadFromPersistenceAsync(IconPack pack)
        {
            var decoded = new ConcurrentDictionary<string, Bitmap>();
            var entries = await _iconPackEntryRepository.GetAllForPackAsync(pack);

            await Parallel.ForEachAsync(entries, async (entry, token) =>
            {
                var bitmap = await BitmapFactory.DecodeByteArrayAsync(entry.Data, 0, entry.Data.Length);

                if (!token.IsCancellationRequested)
                {
                    decoded.TryAdd(entry.Name, bitmap);
                }
            });

            _all = new Dictionary<string, Bitmap>(decoded);
            Update();
        }

        public IEnumerator<KeyValuePair<string, Bitmap>> GetEnumerator()
        {
            return _view.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => _view.Count;

        public KeyValuePair<string, Bitmap> this[int index] => _view.ElementAt(index);
    }
}