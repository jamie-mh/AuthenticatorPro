using System;
using System.IO;
using System.Threading.Tasks;
using Android.Graphics;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Util;

namespace AuthenticatorPro.Droid.Data
{
    internal class CustomIconDecoder : ICustomIconDecoder
    {
        private static Bitmap ToSquare(Bitmap bitmap)
        {
            if(bitmap.Height == bitmap.Width)
                return bitmap;
            
            var largestSide = Math.Max(bitmap.Width, bitmap.Height);

            var squareBitmap = Bitmap.CreateBitmap(largestSide, largestSide, Bitmap.Config.Argb8888);
            var canvas = new Canvas(squareBitmap);
            canvas.DrawColor(Color.Transparent);

            if(bitmap.Height > bitmap.Width)
                canvas.DrawBitmap(bitmap, (largestSide - bitmap.Width) / 2f, 0, null);
            else
                canvas.DrawBitmap(bitmap, 0, (largestSide - bitmap.Height) / 2f, null);

            return squareBitmap;
        }

        private static Bitmap Trim(Bitmap bitmap)
        {
            var width = bitmap.Width;
            var height = bitmap.Height;

            var left = width;
            
            for(var y = 0; y < height; ++y)
            {
                for(var x = 0; x < width; ++x)
                {
                    if(bitmap.GetPixel(x, y) == Color.Transparent || x > left)
                        continue;

                    left = x;
                }
            }
            
            var top = height;
            
            for(var x = 0; x < width; ++x)
            {
                for(var y = 0; y < height; ++y)
                {
                    if(bitmap.GetPixel(x, y) == Color.Transparent || y > top)
                        continue;

                    top = y;
                }
            }

            var right = 0;
            
            for(var y = 0; y < height; ++y)
            {
                for(var x = width - 1; x >= 0; --x)
                {
                    if(bitmap.GetPixel(x, y) == Color.Transparent || x < right)
                        continue;

                    right = x;
                }
            }

            var bottom = 0;
            
            for(var x = 0; x < width; ++x)
            {
                for(var y = height - 1; y >= 0; --y)
                {
                    if(bitmap.GetPixel(x, y) == Color.Transparent || y < bottom)
                        continue;

                    bottom = y;
                }
            }
            
            return Bitmap.CreateBitmap(bitmap, left, top, right - left, bottom - top);
        }
        
        public async Task<CustomIcon> Decode(byte[] rawData)
        {
            var bitmap = await BitmapFactory.DecodeByteArrayAsync(rawData, 0, rawData.Length);
                
            if(bitmap == null)
                throw new Exception("Image could not be loaded.");

            await Task.Run(delegate
            {
                if(bitmap.HasAlpha)
                    bitmap = Trim(bitmap);
                
                bitmap = ToSquare(bitmap);
            });
            
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
            var hash = Hash.Sha1(Convert.ToBase64String(data));
            var id = hash.Truncate(8);
            
            return new CustomIcon
            {
                Id = id,
                Data = data 
            };
        }
    }
}