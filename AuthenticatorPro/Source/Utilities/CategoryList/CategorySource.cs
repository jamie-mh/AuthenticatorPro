using System.Collections.Generic;
using System.Threading.Tasks;
using AuthenticatorPro.Data;
using SQLite;

namespace AuthenticatorPro.Utilities.CategoryList
{
    internal class CategorySource
    {
        private readonly SQLiteAsyncConnection _connection;

        public CategorySource(SQLiteAsyncConnection connection)
        {
            Categories = new List<Category>();
            _connection = connection;

            UpdateTask = Update();
        }

        public List<Category> Categories { get; private set; }
        public Task UpdateTask { get; }

        public async Task Update()
        {
            Categories.Clear();
            Categories =
                await _connection.QueryAsync<Category>("SELECT * FROM category ORDER BY ranking ASC");
        }

        public bool IsDuplicate(Category category)
        {
            foreach(var iterator in Categories)
                if(category.Id == iterator.Id)
                    return true;

            return false;
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

        public int Count()
        {
            return Categories.Count;
        }
    }
}