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

            if(CategoryId != null)
            {
                var bindingForCategory =
                    CategoryBindings.Where(b => b.CategoryId == CategoryId).ToList();

                view = view
                    .Where(a => bindingForCategory.Any(c => c.AuthenticatorSecret == a.Secret))
                    .OrderBy(a => bindingForCategory.First(c => c.AuthenticatorSecret == a.Secret).Ranking)
                    .ToList();
            }

            if(!String.IsNullOrEmpty(Search))
            {
                var searchLower = Search.ToLower();
                
                view = view.Where(i => i.Issuer.ToLower().Contains(searchLower) || i.Username != null && i.Username.Contains(searchLower))
                           .ToList();
            }

            _view = view;
        }

        public async Task Update()
        {
            _all.Clear();
            _all = await _connection.QueryAsync<Authenticator>("SELECT * FROM authenticator ORDER BY ranking, issuer, username");
            
            CategoryBindings.Clear();
            CategoryBindings = await _connection.QueryAsync<AuthenticatorCategory>("SELECT * FROM authenticatorcategory ORDER BY ranking");

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

            await _connection.RunInTransactionAsync(conn =>
            {
                conn.Delete(auth);
                conn.Execute("DELETE FROM authenticatorcategory WHERE authenticatorSecret = ?", auth.Secret);
            });
            
            _view.Remove(auth);
            _all.Remove(auth);
        }

        public void Swap(int oldPosition, int newPosition)
        {
            var atNewPos = Get(newPosition);
            var atOldPos = Get(oldPosition);

            if(atNewPos == null || atOldPos == null)
                throw new ArgumentOutOfRangeException();
            
            _view[newPosition] = atOldPos;
            _view[oldPosition] = atNewPos;
        }

        public async Task CommitRanking()
        {
            if(CategoryId == null)
            {
                for(var i = 0; i < _view.Count; ++i)
                    _view[i].Ranking = i;

                await _connection.UpdateAllAsync(_view);
            }
            else
            {
                await _connection.RunInTransactionAsync(conn =>
                {
                    for(var i = 0; i < _view.Count; ++i)
                    {
                        var binding = GetAuthenticatorCategoryBinding(_view[i].Secret, CategoryId);
                        binding.Ranking = i;
                        
                        conn.Execute(
                            "UPDATE authenticatorcategory SET ranking = ? WHERE categoryId = ? AND authenticatorSecret = ?",
                            i, binding.CategoryId, binding.AuthenticatorSecret);
                    }
                });
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

            return CategoryBindings.Where(c => c.AuthenticatorSecret == auth.Secret)
                                   .Select(c => c.CategoryId)
                                   .ToList();
        }

        private AuthenticatorCategory GetAuthenticatorCategoryBinding(string authSecret, string categoryId)
        {
            return CategoryBindings.FirstOrDefault(b => b.AuthenticatorSecret == authSecret && b.CategoryId == categoryId);
        }

        public async Task AddToCategory(string authSecret, string categoryId)
        {
            var binding = new AuthenticatorCategory(categoryId, authSecret);
            await _connection.InsertAsync(binding);
            CategoryBindings.Add(binding);
        }

        public async Task RemoveFromCategory(string authSecret, string categoryId)
        {
            var binding = GetAuthenticatorCategoryBinding(authSecret, categoryId);

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