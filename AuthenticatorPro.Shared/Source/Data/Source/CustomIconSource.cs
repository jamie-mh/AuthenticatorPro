using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLite;

namespace AuthenticatorPro.Shared.Source.Data.Source
{
    public class CustomIconSource : ISource<CustomIcon>
    {
        private readonly SQLiteAsyncConnection _connection;
        private List<CustomIcon> _all;

        public CustomIconSource(SQLiteAsyncConnection connection)
        {
            _connection = connection;
            _all = new List<CustomIcon>();
        }

        public async Task Update()
        {
            _all = await _connection.QueryAsync<CustomIcon>("SELECT * FROM customicon");
        }

        public async Task Add(CustomIcon icon)
        {
            if(IsDuplicate(icon.Id))
                throw new ArgumentException();

            await _connection.InsertAsync(icon);
            await Update();
        }

        public async Task<int> AddMany(IEnumerable<CustomIcon> icons)
        {
            var valid = icons.Where(c => !IsDuplicate(c.Id)).ToList();
            var added = await _connection.InsertAllAsync(valid);
            await Update();
            return added;
        }

        public CustomIcon Get(string id)
        {
            return _all.FirstOrDefault(i => i.Id == id);
        }

        public async Task Delete(string id)
        {
            await _connection.DeleteAsync<CustomIcon>(id);
            _all.Remove(_all.First(i => i.Id == id));
        }

        public async Task CullUnused()
        {
            // Cannot query as string directly for some reason
            var distinctIconUses = await _connection.QueryAsync<Authenticator>("SELECT DISTINCT icon FROM authenticator");
            var custom = distinctIconUses.Where(i => i.Icon != null && i.Icon.StartsWith(CustomIcon.Prefix.ToString())).Select(i => i.Icon.Substring(1));
            var unused = _all.Select(i => i.Id).Except(custom).ToList();

            if(!unused.Any())
                return;

            await _connection.RunInTransactionAsync(conn =>
            {
                foreach(var icon in unused)
                    conn.Delete<CustomIcon>(icon);
            });

            await Update();
        }

        public bool IsDuplicate(string id)
        {
            return Get(id) != null;
        }

        public List<CustomIcon> GetView()
        {
            return _all;
        }

        public List<CustomIcon> GetAll()
        {
            return _all;
        }

        public CustomIcon Get(int position)
        {
            return _all.ElementAtOrDefault(position);
        }
    }
}