using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLite;


namespace AuthenticatorPro.Data
{
    internal class AuthenticatorSource
    {
        private readonly SQLiteAsyncConnection _connection;

        public List<Authenticator> Authenticators { get; private set; }
        private List<Authenticator> _all;

        private string _search;
        public string Search => _search;

        public string CategoryId { get; private set; }
        public List<AuthenticatorCategory> CategoryBindings { get; private set; }


        public AuthenticatorSource(SQLiteAsyncConnection connection)
        {
            _search = null;
            CategoryId = null;
            _connection = connection;

            Authenticators = new List<Authenticator>();
            _all = new List<Authenticator>();
            CategoryBindings = new List<AuthenticatorCategory>();
        }

        public void SetSearch(string query)
        {
            _search = query;
            UpdateView();
        }

        public void SetCategory(string categoryId)
        {
            CategoryId = categoryId;
            UpdateView();
        }

        public void UpdateView()
        {
            List<Authenticator> view = _all;

            if(CategoryId == null)
            {
                view = view.OrderBy(a => a.Ranking)
                    .ToList();
            }
            else
            {
                var authsInCategory =
                    CategoryBindings.Where(b => b.CategoryId == CategoryId).ToList();

                view =
                    view.Where(a => authsInCategory.Count(b => b.AuthenticatorSecret == a.Secret) == 1)
                        .OrderBy(a => authsInCategory.First(c => c.AuthenticatorSecret == a.Secret).Ranking)
                        .ToList();
            }

            if(!String.IsNullOrEmpty(_search))
            {
                view = view.Where(i => i.Issuer.ToLower().Contains(_search.ToLower()))
                           .ToList();
            }

            Authenticators = view;
        }

        public async Task Update()
        {
            _all.Clear();
            CategoryBindings.Clear();

            var sql = "SELECT * FROM authenticator ORDER BY ranking, issuer, username ASC";
            _all = await _connection.QueryAsync<Authenticator>(sql);

            sql = "SELECT * FROM authenticatorcategory ORDER BY ranking ASC";
            CategoryBindings = await _connection.QueryAsync<AuthenticatorCategory>(sql);

            UpdateView();
        }

        public int GetPosition(string secret)
        {
            return Authenticators.FindIndex(a => a.Secret == secret);
        }

        public async Task Rename(int position, string issuer, string username)
        {
            var auth = Authenticators.ElementAtOrDefault(position);

            if(auth == null)
                return;

            auth.Issuer = issuer;
            auth.Username = username;

            await _connection.UpdateAsync(auth);
        }

        public async Task Delete(int position)
        {
            var auth = Authenticators.ElementAtOrDefault(position);

            if(auth == null)
                return;

            await _connection.DeleteAsync<Authenticator>(auth.Secret);
            Authenticators.Remove(auth);
            _all.Remove(auth);

            const string sql = "DELETE FROM authenticatorcategory WHERE authenticatorSecret = ?";
            await _connection.ExecuteAsync(sql, auth.Secret);
        }

        public async Task Move(int oldPosition, int newPosition)
        {
            var old = Authenticators[newPosition];
            Authenticators[newPosition] = Authenticators[oldPosition];
            Authenticators[oldPosition] = old;

            for(var i = 0; i < Authenticators.Count; ++i)
            {
                if(CategoryId == null)
                {
                    var auth = Authenticators[i];
                    auth.Ranking = i;
                    await _connection.UpdateAsync(auth);
                }
                else
                {
                    var binding = GetAuthenticatorCategoryBinding(Authenticators[i]);
                    binding.Ranking = i;

                    await _connection.ExecuteAsync(
                        "UPDATE authenticatorcategory SET ranking = ? WHERE categoryId = ? AND authenticatorSecret = ?",
                        i, binding.CategoryId, binding.AuthenticatorSecret);
                }
            }
        }

        public async Task IncrementCounter(int position)
        {
            var auth = Authenticators.ElementAtOrDefault(position);

            if(auth == null)
                return;

            auth.Counter++;
            await _connection.UpdateAsync(auth);
        }

        public bool IsDuplicate(Authenticator auth)
        {
            return _all.Any(iterator => auth.Secret == iterator.Secret);
        }

        public bool IsDuplicateCategoryBinding(AuthenticatorCategory binding)
        {
            return CategoryBindings.Any(
                iterator => binding.AuthenticatorSecret == iterator.AuthenticatorSecret &&
                         binding.CategoryId == iterator.CategoryId);
        }

        public List<string> GetCategories(int position)
        {
            var secret = Authenticators[position].Secret;

            var authCategories =
                CategoryBindings.Where(b => b.AuthenticatorSecret == secret).ToList();

            return authCategories.Select(binding => binding.CategoryId).ToList();
        }

        public AuthenticatorCategory GetAuthenticatorCategoryBinding(Authenticator auth)
        {
            return CategoryBindings.First(b => b.AuthenticatorSecret == auth.Secret && b.CategoryId == CategoryId);
        }

        public async Task AddToCategory(string authSecret, string categoryId)
        {
            const string sql = "INSERT INTO authenticatorcategory (categoryId, authenticatorSecret) VALUES (?, ?)";
            await _connection.ExecuteAsync(sql, categoryId, authSecret);
            CategoryBindings.Add(new AuthenticatorCategory(categoryId, authSecret));
        }

        public async Task RemoveFromCategory(string authSecret, string categoryId)
        {
            const string sql = "DELETE FROM authenticatorcategory WHERE categoryId = ? AND authenticatorSecret = ?";
            await _connection.ExecuteAsync(sql, categoryId, authSecret);

            var binding = CategoryBindings.Find(b => b.CategoryId == categoryId && b.AuthenticatorSecret == authSecret);
            CategoryBindings.Remove(binding);
        }
    }
}