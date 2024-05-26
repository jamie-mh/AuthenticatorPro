// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using AuthenticatorPro.Droid.Util;
using AuthenticatorPro.ZXing;
using Java.Nio;
using ImageFormat = AuthenticatorPro.ZXing.ImageFormat;
using Uri = Android.Net.Uri;

namespace AuthenticatorPro.Droid.QrCode
{
    public static class QrCodeImageReader
    {
        public static async Task<string> ScanImageFromFileAsync(Context context, Uri uri)
        {
            Bitmap bitmap;
        
            try
            {
                var data = await FileUtil.ReadFileAsync(context, uri);
                bitmap = await BitmapFactory.DecodeByteArrayAsync(data, 0, data.Length);
            }
            catch (Exception e)
            {
                throw new IOException("Failed to read file", e);
            }
        
            if (bitmap == null)
            {
                throw new IOException("Failed to decode bitmap");
            }
            
            var reader = new QrCodeReader(new ReaderOptions
            {
                Binarizer = Binarizer.LocalAverage,
                TryRotate = true,
                TryHarder = true,
                TryInvert = true
            });
            
            using var buffer = ByteBuffer.Allocate(bitmap.ByteCount);
            await bitmap.CopyPixelsToBufferAsync(buffer);
            buffer.Rewind();
    
            var bytes = new byte[buffer.Remaining()];
            buffer.Get(bytes);
    
            using var imageView = new ImageView(bytes, bitmap.Width, bitmap.Height, ImageFormat.RGBA);
            return await Task.Run(() => reader.Read(imageView));
        }
    }
}
