using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthenticatorPro.Shared.Data;
using SQLite;


namespace AuthenticatorPro.Data
{
    internal class AuthenticatorSource : ISource<Authenticator>
    {
        public string Search { get; private set; }
        public string CategoryId { get; private set; }
        public AuthenticatorType? Type { get; private set; }
        
        public List<AuthenticatorCategory> CategoryBindings { get; private set; }

        
        private readonly SQLiteAsyncConnection _connection;
        private List<Authenticator> _view;
        private List<Authenticator> _all;


        public AuthenticatorSource(SQLiteAsyncConnection connection)
        {
            Search = null;
            CategoryId = null;
            _connection = connection;

            _view = new List<Authenticator>();
            _all = new List<Authenticator>();
            CategoryBindings = new List<AuthenticatorCategory>();
        }

        public void SetSearch(string query)
        {
            Search = query;
            UpdateView();
        }

        public void SetCategory(string categoryId)
        {
            CategoryId = categoryId;
            UpdateView();
        }

        public void SetType(AuthenticatorType? type)
        {
            Type = type;
            UpdateView();
        }

        public void UpdateView()
        {
            var view = _all;

            if(Type != null)
                view = view.Where(i => i.Type == Type.Value).ToList();

            if(CategoryId == null)
                view = view.OrderBy(a => a.Ranking).ToList();
            else
            {
                var authsInCategory =
                    CategoryBindings.Where(b => b.CategoryId == CategoryId).ToList();

                view = view
                    .Where(a => authsInCategory.Count(b => b.AuthenticatorSecret == a.Secret) == 1)
                    .OrderBy(a => authsInCategory.First(c => c.AuthenticatorSecret == a.Secret).Ranking)
                    .ToList();
            }

            if(!String.IsNullOrEmpty(Search))
            {
                var searchLower = Search.ToLower();
                
                view = view.Where(i => i.Issuer.ToLower().Contains(searchLower) || (i.Username != null && i.Username.Contains(searchLower)))
                           .ToList();
            }

            _view = view;
        }

        public async Task Update()
        {
            _all.Clear();
            CategoryBindings.Clear();
            
            var sql = "SELECT * FROM authenticator ORDER BY ranking, issuer, username";
            _all = await _connection.QueryAsync<Authenticator>(sql);

            sql = "SELECT * FROM authenticatorcategory ORDER BY ranking";
            CategoryBindings = await _connection.QueryAsync<AuthenticatorCategory>(sql);

            UpdateView();
        }

        public Authenticator Get(int position)
        {
            return _view.ElementAtOrDefault(position);
        }

        public int GetPosition(string secret)
        {
            return _view.FindIndex(a => a.Secret == secret);
        }

        public async Task Rename(int position, string issuer, string username)
        {
            var auth = Get(position);

            if(auth == null)
                throw new ArgumentOutOfRangeException();

            auth.Issuer = issuer;
            auth.Username = username;

            await _connection.UpdateAsync(auth);
        }

        public async Task Delete(int position)
        {
            var auth = Get(position);

            if(auth == null)
                throw new ArgumentOutOfRangeException();

            await _connection.DeleteAsync<Authenticator>(auth.Secret);
            _view.Remove(auth);
            _all.Remove(auth);

            const string sql = "DELETE FROM authenticatorcategory WHERE authenticatorSecret = ?";
            await _connection.ExecuteAsync(sql, auth.Secret);
        }

        public async Task Move(int oldPosition, int newPosition)
        {
            var atNewPos = Get(newPosition);
            var atOldPos = Get(oldPosition);

            if(atNewPos == null || atOldPos == null)
                throw new ArgumentOutOfRangeException();
            
            _view[newPosition] = atOldPos;
            _view[oldPosition] = atNewPos;

            for(var i = 0; i < _view.Count; ++i)
            {
                if(CategoryId == null)
                {
                    var auth = _view[i];
                    auth.Ranking = i;
                    await _connection.UpdateAsync(auth);
                }
                else
                {
                    var binding = GetAuthenticatorCategoryBinding(_view[i]);
                    binding.Ranking = i;
                    
                    await _connection.ExecuteAsync(
                        "UPDATE authenticatorcategory SET ranking = ? WHERE categoryId = ? AND authenticatorSecret = ?",
                        i, binding.CategoryId, binding.AuthenticatorSecret);
                }
            }
        }

        public async Task IncrementCounter(int position)
        {
            var auth = Get(position);

            if(auth == null)
                throw new ArgumentOutOfRangeException();

            if(auth.Type != AuthenticatorType.Hotp)
                throw new ArgumentException();

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
                ac => binding.AuthenticatorSecret == ac.AuthenticatorSecret &&
                         binding.CategoryId == ac.CategoryId);
        }

        public List<string> GetCategories(int position)
        {
            var auth = Get(position);

            if(auth == null)
                throw new ArgumentOutOfRangeException();
            
            var authCategories =
                CategoryBindings.Where(b => b.AuthenticatorSecret == auth.Secret).ToList();

            return authCategories.Select(binding => binding.CategoryId).ToList();
        }

        public AuthenticatorCategory GetAuthenticatorCategoryBinding(Authenticator auth)
        {
            return CategoryBindings.FirstOrDefault(b => b.AuthenticatorSecret == auth.Secret && b.CategoryId == CategoryId);
        }

        public async Task AddToCategory(string categoryId, string authSecret)
        {
            var binding = new AuthenticatorCategory(categoryId, authSecret);
            await _connection.InsertAsync(binding);
            CategoryBindings.Add(binding);
        }

        public async Task RemoveFromCategory(string categoryId, string authSecret)
        {
            var binding = CategoryBindings.Find(b => b.CategoryId == categoryId && b.AuthenticatorSecret == authSecret);

            if(binding == null)
                throw new ArgumentException();

            await _connection.ExecuteAsync(
                "DELETE FROM authenticatorcategory WHERE authenticatorSecret = ? AND categoryId = ?", 
                authSecret, categoryId);
            
            CategoryBindings.Remove(binding);
        }

        public int CountUsesOfCustomIcon(string id)
        {
            return _all.Count(a => a.Icon == CustomIcon.Prefix + id);
        }

        public List<Authenticator> GetView()
        {
            return _view;
        }

        public List<Authenticator> GetAll()
        {
            return _all;
        }
    }
}