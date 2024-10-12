// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Graphics;
using Stratum.Core.Persistence;

namespace Stratum.Droid.Persistence.View.Impl
{
    public class CustomIconView : ICustomIconView
    {
        private readonly ICustomIconRepository _customIconRepository;

        private Dictionary<string, Bitmap> _all;

        public CustomIconView(ICustomIconRepository customIconRepository)
        {
            _customIconRepository = customIconRepository;
            _all = new Dictionary<string, Bitmap>();
        }

        public void Update()
        {
            // Do nothing
        }

        public async Task LoadFromPersistenceAsync()
        {
            var decoded = new ConcurrentDictionary<string, Bitmap>();
            var icons = await _customIconRepository.GetAllAsync();

            await Parallel.ForEachAsync(icons, async (icon, token) =>
            {
                var bitmap = await BitmapFactory.DecodeByteArrayAsync(icon.Data, 0, icon.Data.Length);

                if (!token.IsCancellationRequested)
                {
                    decoded.TryAdd(icon.Id, bitmap);
                }
            });

            _all = new Dictionary<string, Bitmap>(decoded);
            Update();
        }

        public Bitmap GetOrDefault(string id)
        {
            return _all.GetValueOrDefault(id);
        }

        public IEnumerator<KeyValuePair<string, Bitmap>> GetEnumerator()
        {
            return _all.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => _all.Count;

        public KeyValuePair<string, Bitmap> this[int index] => _all.ElementAt(index);
    }
}