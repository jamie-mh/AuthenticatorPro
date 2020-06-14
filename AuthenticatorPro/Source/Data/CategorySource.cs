using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLite;

namespace AuthenticatorPro.Data
{
    internal class CategorySource
    {
        private readonly SQLiteAsyncConnection _connection;

        public List<Category> Categories { get; private set; }


        public CategorySource(SQLiteAsyncConnection connection)
        {
            Categories = new List<Category>();
            _connection = connection;
        }

        public async Task Update()
        {
            Categories.Clear();
            Categories =
                await _connection.QueryAsync<Category>("SELECT * FROM category ORDER BY ranking ASC");
        }

        public bool IsDuplicate(Category category)
        {
            return Categories.Any(iterator => category.Id == iterator.Id);
        }

        public async Task Delete(int position)
        {
            var category = Categories[position];
            await _connection.DeleteAsync(category);
            Categories.RemoveAt(position);

            object[] args = {category.Id};
            await _connection.ExecuteAsync("DELETE FROM authenticatorcategory WHERE categoryId = ?", args);
        }

        public async Task Rename(int position, string name)
        {
            var old = Categories[position];
            var replacement = new Category(name);

            Categories.RemoveAt(position);
            Categories.Add(replacement);

            await _connection.DeleteAsync(old);
            await _connection.InsertAsync(replacement);

            object[] args = {replacement.Id, old.Id};
            await _connection.QueryAsync<AuthenticatorCategory>(
                "UPDATE authenticatorcategory SET categoryId = ? WHERE categoryId = ?", args);
        }

        public async void Move(int oldPosition, int newPosition)
        {
            var old = Categories[newPosition];
            Categories[newPosition] = Categories[oldPosition];
            Categories[oldPosition] = old;

            if(oldPosition > newPosition)
                for(var i = newPosition; i < Categories.Count; ++i)
                {
                    var cat = Categories[i];
                    cat.Ranking++;
                    await _connection.UpdateAsync(cat);
                }
            else
                for(var i = oldPosition; i < newPosition; ++i)
                {
                    var cat = Categories[i];
                    cat.Ranking--;
                    await _connection.UpdateAsync(cat);
                }

            var temp = Categories[newPosition];
            temp.Ranking = newPosition;
            await _connection.UpdateAsync(temp);
        }

        public int GetPosition(string id)
        {
            if(id == null)
                return -1;

            return Categories.FindIndex(c => c.Id == id);
        }
    }
}