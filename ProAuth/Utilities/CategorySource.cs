using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Albireo.Base32;
using Javax.Xml.Xpath;
using OtpSharp;
using ProAuth.Data;
using SQLite;

namespace ProAuth.Utilities
{
    internal class CategorySource
    {
        public List<Category> Categories { get; private set; }
        public Task LoadTask { get; }

        private readonly SQLiteAsyncConnection _connection;

        public CategorySource(SQLiteAsyncConnection connection)
        {
            Categories = new List<Category>();
            _connection = connection;

            LoadTask = Update();
        }

        public async Task Update()
        {
            Categories.Clear();
            Categories = await _connection.QueryAsync<Category>("SELECT * FROM category ORDER BY name ASC");
        }

        public bool IsDuplicate(Category category)
        {
            foreach(Category iterator in Categories)
            {
                if(category.Id == iterator.Id)
                {
                    return true;
                }
            }

            return false;
        }

        public async Task Delete(int position)
        {
            Category category = Categories[position];
            await _connection.DeleteAsync(category);
            Categories.RemoveAt(position);

            object[] args = {category.Id};
            _connection.QueryAsync<AuthenticatorCategory>("DELETE * FROM authenticatorcategory WHERE categoryId = ?",
                args);
        }

        public void Rename(int position, string name)
        {
            Category old = Categories[position];
            Category replacement = new Category(name);

            Categories.RemoveAt(position);
            Categories.Add(replacement);

            _connection.DeleteAsync(old);
            _connection.InsertAsync(replacement);

            object[] args = {replacement.Id, old.Id};
            _connection.QueryAsync<AuthenticatorCategory>(
                "UPDATE authenticatorcategory SET categoryId = ? WHERE categoryId = ?", args);
        }

        public int Count()
        {
            return Categories.Count;
        }
    }
}