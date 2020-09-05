using Android.Content;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace AuthenticatorPro.WearOS.Cache
{
    internal class ListCache<T>
    {
        private readonly string _name;
        private readonly Context _context;

        private List<T> _items;
        public int Count => _items.Count;

        private Task _flushTask;

        public ListCache(string name, Context context)
        {
            _name = name;
            _context = context;
            _items = new List<T>();
        }

        private string GetFilePath()
        {
            return $"{_context.CacheDir}/{_name}.json";
        }

        public async Task Init()
        {
            var path = GetFilePath();
            
            if(!File.Exists(path))
                return;

            var json = await File.ReadAllTextAsync(path);
            _items = JsonConvert.DeserializeObject<List<T>>(json);
        }

        public async Task Replace(List<T> items)
        {
            _items = items;
            await Flush();
        }

        public bool Dirty(List<T> items, IEqualityComparer<T> comparer = null)
        {
            return comparer != null
                ? !_items.SequenceEqual(items, comparer)
                : !_items.SequenceEqual(items);
        }

        private async Task Flush()
        {
            var json = JsonConvert.SerializeObject(_items);

            if(_flushTask != null)
                await _flushTask;
            
            _flushTask = File.WriteAllTextAsync(GetFilePath(), json);
            await _flushTask;
        }

        public T Get(int position)
        {
            return _items.ElementAtOrDefault(position);
        }

        public List<T> GetItems()
        {
            return _items;
        }
    }
}