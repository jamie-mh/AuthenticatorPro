// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content.Res;
using System.IO;
using System.Threading.Tasks;

namespace AuthenticatorPro.Droid.Util
{
    internal static class AssetUtil
    {
        public static async Task<string> ReadAllTextAsync(AssetManager assetManager, string path)
        {
            var asset = assetManager.Open(path);
            using var reader = new StreamReader(asset);
            
            try
            {
                return await reader.ReadToEndAsync();
            }
            finally
            {
                asset.Close();
            }
        }
        
        public static async Task<byte[]> ReadAllBytes(AssetManager assetManager, string path)
        {
            var asset = assetManager.Open(path);
            using var stream = new MemoryStream();
            
            try
            {
                await asset.CopyToAsync(stream);
            }
            finally
            {
                asset.Close();
            }

            return stream.ToArray();
        }
    }
}