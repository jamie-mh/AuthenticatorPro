// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;

namespace Stratum.WearOS.Cache
{
    public class CustomIconCache : IDisposable
    {
        public const char Prefix = '@';
        private const string IconFileExtension = "bmp";

        private readonly Context _context;
        private readonly SemaphoreSlim _decodeLock;
        private Dictionary<string, Bitmap> _bitmaps;
        private bool _isDisposed;

        public CustomIconCache(Context context)
        {
            _context = context;
            _decodeLock = new SemaphoreSlim(1, 1);
            _bitmaps = new Dictionary<string, Bitmap>();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~CustomIconCache()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                _decodeLock.Dispose();
            }

            _isDisposed = true;
        }

        public async Task InitAsync()
        {
            var decoded = new ConcurrentDictionary<string, Bitmap>();

            await Parallel.ForEachAsync(GetIcons(), async (id, _) =>
            {
                var bitmap = await BitmapFactory.DecodeFileAsync(GetIconPath(id));
                decoded.TryAdd(id, bitmap);
            });

            _bitmaps = new Dictionary<string, Bitmap>(decoded);
        }

        public async Task AddAsync(string id, byte[] data)
        {
            await File.WriteAllBytesAsync(GetIconPath(id), data);
            _ = await GetFreshBitmapAsync(id);
        }

        public void Remove(string id)
        {
            File.Delete(GetIconPath(id));
        }

        public List<string> GetIcons()
        {
            var info = new DirectoryInfo(_context.CacheDir.Path);

            var ids = info.GetFiles()
                .Where(f => f.Extension == $".{IconFileExtension}")
                .Select(f => f.Name[..f.Name.LastIndexOf('.')]).ToList();

            return ids;
        }

        public Bitmap GetCachedBitmap(string id)
        {
            return _bitmaps.GetValueOrDefault(id);
        }

        public async Task<Bitmap> GetFreshBitmapAsync(string id)
        {
            if (_bitmaps.TryGetValue(id, out var bitmap))
            {
                return bitmap;
            }

            await _decodeLock.WaitAsync();

            try
            {
                if (_bitmaps.TryGetValue(id, out bitmap))
                {
                    return bitmap;
                }

                bitmap = await BitmapFactory.DecodeFileAsync(GetIconPath(id));

                if (bitmap == null)
                {
                    return null;
                }

                _bitmaps.Add(id, bitmap);
                return bitmap;
            }
            finally
            {
                _decodeLock.Release();
            }
        }

        private string GetIconPath(string id)
        {
            return $"{_context.CacheDir.Path}/{id}.{IconFileExtension}";
        }
    }
}