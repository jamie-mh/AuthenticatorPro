using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLite;

namespace AuthenticatorPro.Shared.Source.Data.Source
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
            _all = await _connection.QueryAsync<Category>("SELECT * FROM category ORDER BY ranking ASC");
        }

        public async Task<int> Add(Category category)
        {
            if(IsDuplicate(category))
                throw new ArgumentException();
            
            await _connection.InsertAsync(category);
            await Update();
            return GetPosition(category.Id);
        }

        public async Task<int> AddMany(IEnumerable<Category> categories)
        {
            var valid = categories.Where(c => !IsDuplicate(c)).ToList();
            var added = await _connection.InsertAllAsync(valid);
            await Update();
            return added;
        }

        public bool IsDuplicate(Category category)
        {
            return _all.Any(iterator => category.Id == iterator.Id);
        }

        public async Task Delete(int position)
        {
            var category = Get(position);

            if(category == null)
                throw new ArgumentOutOfRangeException();

            await _connection.RunInTransactionAsync(conn =>
            {
                conn.Delete(category);
                conn.Execute("DELETE FROM authenticatorcategory WHERE categoryId = ?", category.Id);
            });
            
            _all.RemoveAt(position);
        }

        public async Task Rename(int position, string name)
        {
            if(String.IsNullOrEmpty(name))
                throw new ArgumentException();
            
            var old = Get(position);

            if(old == null)
                throw new ArgumentOutOfRangeException();
            
            var replacement = new Category(name);
            _all[position] = replacement;

            await _connection.RunInTransactionAsync(conn =>
            {
                conn.Delete(old);
                conn.Insert(replacement);

                conn.Query<AuthenticatorCategory>(
                    "UPDATE authenticatorcategory SET categoryId = ? WHERE categoryId = ?", replacement.Id, old.Id);
            });
        }

        public void Swap(int oldPosition, int newPosition)
        {
            var atNewPos = Get(newPosition);
            var atOldPos = Get(oldPosition);

            if(atNewPos == null || atOldPos == null)
                throw new ArgumentOutOfRangeException();
            
            _all[newPosition] = atOldPos;
            _all[oldPosition] = atNewPos;
        }

        public async Task CommitRanking()
        {
            for(var i = 0; i < _all.Count; ++i)
                Get(i).Ranking = i;

            await _connection.UpdateAllAsync(_all);
        }

        public Category Get(int position)
        {
            return _all.ElementAtOrDefault(position);
        }

        public int GetPosition(string id)
        {
            if(String.IsNullOrEmpty(id))
                throw new ArgumentException();

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