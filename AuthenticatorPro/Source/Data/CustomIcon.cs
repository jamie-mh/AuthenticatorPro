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
        
        public Bitmap GetBitmap()
        {
            return _bitmap ??= BitmapFactory.DecodeByteArray(Data, 0, Data.Length);
        }
        
        public static async Task<CustomIcon> FromBytes(byte[] image)
        {
            Bitmap bitmap = null;
            MemoryStream stream = null;

            try
            {
                bitmap = await BitmapFactory.DecodeByteArrayAsync(image, 0, image.Length);
                
                if(bitmap == null)
                    throw new Exception("Image could not be loaded.");

                var size = Math.Min(MaxSize, bitmap.Width);
                bitmap = Bitmap.CreateScaledBitmap(bitmap, size, size, false);
                var buffer = ByteBuffer.Allocate(bitmap.ByteCount);
                await bitmap.CopyPixelsToBufferAsync(buffer);

                stream = new MemoryStream();
                await bitmap.CompressAsync(Bitmap.CompressFormat.Png, 100, stream);
            }
            finally
            {
                stream?.Close();
                bitmap?.Recycle();
            }
            
            var imageData = stream.ToArray();
            var hash = Hash.SHA1(Convert.ToBase64String(imageData));
            var id = hash.Truncate(8);
            
            return new CustomIcon
            {
                Id = id,
                Data = imageData
            };
        }
    }
}