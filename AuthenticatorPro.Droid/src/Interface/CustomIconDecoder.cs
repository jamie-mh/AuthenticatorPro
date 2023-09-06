// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Threading.Tasks;
using Android.Graphics;
using AuthenticatorPro.Core;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Util;

namespace AuthenticatorPro.Droid.Interface
{
    public class CustomIconDecoder : ICustomIconDecoder
    {
        public async Task<CustomIcon> DecodeAsync(byte[] rawData, bool shouldPreProcess)
        {
            var bitmap = await BitmapFactory.DecodeByteArrayAsync(rawData, 0, rawData.Length);

            if (bitmap == null)
            {
                throw new ArgumentException("Image could not be loaded.");
            }

            if (shouldPreProcess)
            {
                await Task.Run(delegate
                {
                    if (bitmap.HasAlpha)
                    {
                        bitmap = Trim(bitmap);
                    }

                    bitmap = ToSquare(bitmap);
                });
            }

            var size = Math.Min(CustomIcon.MaxSize, bitmap.Width); // width or height, doesn't matter as it's square
            var stream = new MemoryStream();

            try
            {
                bitmap = Bitmap.CreateScaledBitmap(bitmap, size, size, true);
                await bitmap.CompressAsync(Bitmap.CompressFormat.Png, 100, stream);
            }
            finally
            {
                stream.Close();
                bitmap?.Recycle();
            }

            var data = stream.ToArray();
            var hash = HashUtil.Sha1(Convert.ToBase64String(data));
            var id = hash.Truncate(8);

            return new CustomIcon { Id = id, Data = data };
        }

        private static Bitmap ToSquare(Bitmap bitmap)
        {
            if (bitmap.Height == bitmap.Width)
            {
                return bitmap;
            }

            var largestSide = Math.Max(bitmap.Width, bitmap.Height);

            var squareBitmap = Bitmap.CreateBitmap(largestSide, largestSide, Bitmap.Config.Argb8888);
            var canvas = new Canvas(squareBitmap);
            canvas.DrawColor(Color.Transparent);

            if (bitmap.Height > bitmap.Width)
            {
                canvas.DrawBitmap(bitmap, (largestSide - bitmap.Width) / 2f, 0, null);
            }
            else
            {
                canvas.DrawBitmap(bitmap, 0, (largestSide - bitmap.Height) / 2f, null);
            }

            return squareBitmap;
        }

        private static Bitmap Trim(Bitmap bitmap)
        {
            var width = bitmap.Width;
            var height = bitmap.Height;

            var pixels = new int[width * height];
            bitmap.GetPixels(pixels, 0, width, 0, 0, width, height);

            bool IsTransparent(int x, int y)
            {
                return (pixels[y * width + x] & 0xF000) == 0;
            }

            var left = width;

            for (var y = 0; y < height; ++y)
            {
                for (var x = 0; x < width; ++x)
                {
                    if (IsTransparent(x, y) || x > left)
                    {
                        continue;
                    }

                    left = x;
                }
            }

            var top = height;

            for (var x = 0; x < width; ++x)
            {
                for (var y = 0; y < height; ++y)
                {
                    if (IsTransparent(x, y) || y > top)
                    {
                        continue;
                    }

                    top = y;
                }
            }

            var right = 0;

            for (var y = 0; y < height; ++y)
            {
                for (var x = width - 1; x >= 0; --x)
                {
                    if (IsTransparent(x, y) || x < right)
                    {
                        continue;
                    }

                    right = x;
                }
            }

            var bottom = 0;

            for (var x = 0; x < width; ++x)
            {
                for (var y = height - 1; y >= 0; --y)
                {
                    if (IsTransparent(x, y) || y < bottom)
                    {
                        continue;
                    }

                    bottom = y;
                }
            }

            return Bitmap.CreateBitmap(bitmap, left, top, right - left, bottom - top);
        }
    }
}