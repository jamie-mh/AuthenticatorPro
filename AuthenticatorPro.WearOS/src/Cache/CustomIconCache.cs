// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content;
using Android.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AuthenticatorPro.WearOS.Cache
{
    internal class CustomIconCache : IDisposable
    {
        public const char Prefix = '@';
        private const string IconFileExtension = "bmp";

        private readonly Context _context;
        private readonly SemaphoreSlim _decodeLock;
        private readonly Dictionary<string, Bitmap> _bitmaps;
        private bool _isDisposed;

        public CustomIconCache(Context context)
        {
            _context = context;
            _decodeLock = new SemaphoreSlim(1, 1);
            _bitmaps = new Dictionary<string, Bitmap>();
        }

        ~CustomIconCache()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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

        public async Task Add(string id, byte[] data)
        {
            await File.WriteAllBytesAsync(GetIconPath(id), data);
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

        public async Task<Bitmap> GetBitmapAsync(string id)
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