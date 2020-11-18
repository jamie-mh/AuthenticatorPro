using System;
using System.IO;
using System.Threading.Tasks;
using Android.Graphics;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Util;
using Java.Nio;
using Newtonsoft.Json;
using SQLite;

namespace AuthenticatorPro.Data
{
    [Table("customicon")]
    internal class CustomIcon
    {
        public const char Prefix = '@';
        private const int MaxSize = 128;
        
        [Column("id")]
        public string Id { get; set; }
        
        [Column("data")]
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] Data { get; set; }

        private Bitmap _bitmap;
        
        public async Task<Bitmap> GetBitmap()
        {
            return _bitmap ??= await BitmapFactory.DecodeByteArrayAsync(Data, 0, Data.Length);
        }

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
        
        public static async Task<CustomIcon> FromBytes(byte[] rawData)
        {
            var bitmap = await BitmapFactory.DecodeByteArrayAsync(rawData, 0, rawData.Length);
                
            if(bitmap == null)
                throw new Exception("Image could not be loaded.");

            bitmap = ToSquare(bitmap);
            var size = Math.Min(MaxSize, bitmap.Width); // width or height, doesn't matter as it's square
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
            var hash = Hash.SHA1(Convert.ToBase64String(data));
            var id = hash.Truncate(8);
            
            return new CustomIcon
            {
                Id = id,
                Data = data 
            };
        }
    }
}