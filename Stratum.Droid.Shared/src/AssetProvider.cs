// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.IO;
using System.Threading.Tasks;
using Android.Content;
using Stratum.Core;

namespace Stratum.Droid.Shared
{
    public class AssetProvider : IAssetProvider
    {
        private readonly Context _context;

        public AssetProvider(Context context)
        {
            _context = context;
        }

        public async Task<byte[]> ReadBytesAsync(string path)
        {
            Stream stream = null;
            MemoryStream memoryStream = null;

            try
            {
                stream = _context.Assets.Open(path);
                memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
            finally
            {
                stream?.Close();
                memoryStream?.Close();
            }
        }

        public async Task<string> ReadStringAsync(string path)
        {
            Stream stream = null;
            StreamReader reader = null;

            try
            {
                stream = _context.Assets.Open(path);
                reader = new StreamReader(stream);

                return await reader.ReadToEndAsync();
            }
            finally
            {
                stream?.Close();
                reader?.Close();
            }
        }
    }
}