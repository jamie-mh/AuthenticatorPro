using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLite;

namespace AuthenticatorPro.Data
{
    internal class CustomIconSource
    {
        private readonly SQLiteAsyncConnection _connection;
        
        public List<CustomIcon> View { get; private set; }


        public CustomIconSource(SQLiteAsyncConnection connection)
        {
            _connection = connection;
            View = new List<CustomIcon>();
        }

        public async Task Update()
        {
            View.Clear();
            View = await _connection.QueryAsync<CustomIcon>("SELECT * FROM customicon");
        }

        public CustomIcon Get(string id)
        {
            return View.FirstOrDefault(i => i.Id == id);
        }

        public async Task Delete(string id)
        {
            await _connection.ExecuteAsync("DELETE FROM customicon WHERE id = ?", id);
            View.Remove(View.First(i => i.Id == id));
        }

        public bool IsDuplicate(string id)
        {
            return Get(id) != null;
        }
    }
}