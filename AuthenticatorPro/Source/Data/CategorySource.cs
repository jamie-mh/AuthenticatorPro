using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLite;

namespace AuthenticatorPro.Data
{
    internal class CategorySource
    {
        private readonly SQLiteAsyncConnection _connection;

        public List<Category> View { get; private set; }


        public CategorySource(SQLiteAsyncConnection connection)
        {
            View = new List<Category>();
            _connection = connection;
        }

        public async Task Update()
        {
            View.Clear();
            View =
                await _connection.QueryAsync<Category>("SELECT * FROM category ORDER BY ranking ASC");
        }

        public bool IsDuplicate(Category category)
        {
            return View.Any(iterator => category.Id == iterator.Id);
        }

        public async Task Delete(int position)
        {
            var category = View[position];
            await _connection.DeleteAsync(category);
            View.RemoveAt(position);

            object[] args = {category.Id};
            await _connection.ExecuteAsync("DELETE FROM authenticatorcategory WHERE categoryId = ?", args);
        }

        public async Task Rename(int position, string name)
        {
            var old = View[position];
            var replacement = new Category(name);

            View[position] = replacement;

            await _connection.DeleteAsync(old);
            await _connection.InsertAsync(replacement);

            object[] args = {replacement.Id, old.Id};
            await _connection.QueryAsync<AuthenticatorCategory>(
                "UPDATE authenticatorcategory SET categoryId = ? WHERE categoryId = ?", args);
        }

        public async void Move(int oldPosition, int newPosition)
        {
            var old = View[newPosition];
            View[newPosition] = View[oldPosition];
            View[oldPosition] = old;

            if(oldPosition > newPosition)
                for(var i = newPosition; i < View.Count; ++i)
                {
                    var cat = View[i];
                    cat.Ranking++;
                    await _connection.UpdateAsync(cat);
                }
            else
                for(var i = oldPosition; i < newPosition; ++i)
                {
                    var cat = View[i];
                    cat.Ranking--;
                    await _connection.UpdateAsync(cat);
                }

            var temp = View[newPosition];
            temp.Ranking = newPosition;
            await _connection.UpdateAsync(temp);
        }

        public Category Get(int position)
        {
            return View.ElementAtOrDefault(position);
        }

        public int GetPosition(string id)
        {
            if(id == null)
                return -1;

            return View.FindIndex(c => c.Id == id);
        }
    }
}