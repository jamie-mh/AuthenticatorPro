using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLite;

namespace AuthenticatorPro.Data
{
    internal class CategorySource : ISource<Category>
    {
        private readonly SQLiteAsyncConnection _connection;
        private List<Category> _all;


        public CategorySource(SQLiteAsyncConnection connection)
        {
            _all = new List<Category>();
            _connection = connection;
        }

        public async Task Update()
        {
            _all.Clear();
            _all = await _connection.QueryAsync<Category>("SELECT * FROM category ORDER BY ranking ASC");
        }

        public bool IsDuplicate(Category category)
        {
            return _all.Any(iterator => category.Id == iterator.Id);
        }

        public async Task Delete(int position)
        {
            var category = _all[position];
            await _connection.DeleteAsync(category);
            _all.RemoveAt(position);

            object[] args = {category.Id};
            await _connection.ExecuteAsync("DELETE FROM authenticatorcategory WHERE categoryId = ?", args);
        }

        public async Task Rename(int position, string name)
        {
            var old = _all[position];
            var replacement = new Category(name);

            _all[position] = replacement;

            await _connection.DeleteAsync(old);
            await _connection.InsertAsync(replacement);

            object[] args = {replacement.Id, old.Id};
            await _connection.QueryAsync<AuthenticatorCategory>(
                "UPDATE authenticatorcategory SET categoryId = ? WHERE categoryId = ?", args);
        }

        public async void Move(int oldPosition, int newPosition)
        {
            var old = _all[newPosition];
            _all[newPosition] = _all[oldPosition];
            _all[oldPosition] = old;

            if(oldPosition > newPosition)
                for(var i = newPosition; i < _all.Count; ++i)
                {
                    var cat = _all[i];
                    cat.Ranking++;
                    await _connection.UpdateAsync(cat);
                }
            else
                for(var i = oldPosition; i < newPosition; ++i)
                {
                    var cat = _all[i];
                    cat.Ranking--;
                    await _connection.UpdateAsync(cat);
                }

            var temp = _all[newPosition];
            temp.Ranking = newPosition;
            await _connection.UpdateAsync(temp);
        }

        public Category Get(int position)
        {
            return _all.ElementAtOrDefault(position);
        }

        public int GetPosition(string id)
        {
            if(id == null)
                return -1;

            return _all.FindIndex(c => c.Id == id);
        }
        
        public List<Category> GetView()
        {
            return _all;
        }

        public List<Category> GetAll()
        {
            return _all;
        }
    }
}