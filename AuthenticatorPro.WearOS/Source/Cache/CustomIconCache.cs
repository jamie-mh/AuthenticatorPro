using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using Bitmap = Android.Graphics.Bitmap;

namespace AuthenticatorPro.WearOS.Cache
{
    internal class CustomIconCache
    {
        public const char Prefix = '@';
        private const string IconFileExtension = "bmp";
        
        private readonly Context _context;
        private readonly Dictionary<string, Task<Bitmap>> _bitmaps;

        
        public CustomIconCache(Context context)
        {
            _context = context;
            _bitmaps = new Dictionary<string, Task<Bitmap>>();
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
                .Select(f => f.Name.Substring(0, f.Name.LastIndexOf('.'))).ToList();
            
            return ids;
        }
        
        public async Task<Bitmap> GetBitmap(string id)
        {
            if(_bitmaps.ContainsKey(id))
                return await _bitmaps[id];

            var bitmapTask = BitmapFactory.DecodeFileAsync(GetIconPath(id));
            _bitmaps.Add(id, bitmapTask);

            return await bitmapTask;
        }

        private string GetIconPath(string id)
        {
            return $"{_context.CacheDir.Path}/{id}.{IconFileExtension}";
        }
    }
}