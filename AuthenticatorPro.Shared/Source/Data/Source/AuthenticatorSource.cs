using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthenticatorPro.Shared.Source.Data.Generator;
using SQLite;

namespace AuthenticatorPro.Shared.Source.Data.Source
{
    public class AuthenticatorSource : ISource<Authenticator>
    {
        public string Search { get; private set; }
        public string CategoryId { get; private set; }
        public GenerationMethod? GenerationMethod { get; private set; }
        
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

        public void SetGenerationMethod(GenerationMethod? generationMethod)
        {
            GenerationMethod = generationMethod;
            UpdateView();
        }

        public void UpdateView()
        {
            var view = _all;

            if(GenerationMethod != null)
                view = view.Where(i => i.Type.GetGenerationMethod() == GenerationMethod).ToList();

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
            _all = await _connection.QueryAsync<Authenticator>("SELECT * FROM authenticator ORDER BY ranking, issuer, username");
            CategoryBindings = await _connection.QueryAsync<AuthenticatorCategory>("SELECT * FROM authenticatorcategory ORDER BY ranking");
            UpdateView();
        }

        public async Task<int> Add(Authenticator auth)
        {
            if(IsDuplicate(auth) || !auth.IsValid())
                throw new ArgumentException();
            
            await _connection.InsertAsync(auth);
            await Update();
            return GetPosition(auth.Secret);
        }

        public async Task<int> AddMany(IEnumerable<Authenticator> authenticators)
        {
            var valid = authenticators.Where(a => a.IsValid() && !IsDuplicate(a)).ToList();
            var added = await _connection.InsertAllAsync(valid);
            await Update();
            return added;
        }

        public async Task<Tuple<int, int>> AddOrUpdateMany(IEnumerable<Authenticator> authenticators)
        {
            var valid = authenticators.Where(a => a.IsValid()).ToList();
            
            var toAdd = valid.Where(a => !IsDuplicate(a)).ToList();
            var addedCount = await _connection.InsertAllAsync(toAdd);

            var toUpdate = valid
                .Where(a => !toAdd.Contains(a))
                .OrderBy(a => a.Secret.GetHashCode()).ToList();
            
            var updateTargets = _all
                .Where(a => toUpdate.Any(b => a.Secret == b.Secret))
                .OrderBy(a => a.Secret.GetHashCode());
                
            var diff = updateTargets.Except(toUpdate, new AuthenticatorComparer());
            var updatedCount = diff.Count();
            
            await _connection.UpdateAllAsync(toUpdate);
            await Update();
            
            return new Tuple<int, int>(addedCount, updatedCount);
        }
        
        public async Task AddManyCategoryBindings(IEnumerable<AuthenticatorCategory> bindings)
        {
            var valid = bindings.Where(b => !IsDuplicateCategoryBinding(b)).ToList();
            await _connection.InsertAllAsync(valid);
            await Update();
        }

        public async Task AddOrUpdateManyCategoryBindings(IEnumerable<AuthenticatorCategory> bindings)
        {
            var bindingsList = bindings.ToList();
            
            var toAdd = bindingsList.Where(b => !IsDuplicateCategoryBinding(b)).ToList();
            await _connection.InsertAllAsync(toAdd);

            var toUpdate = bindingsList.Where(b => !toAdd.Contains(b));
            await _connection.RunInTransactionAsync(conn =>
            {
                foreach(var binding in toUpdate)                
                    conn.Execute("UPDATE authenticatorcategory SET ranking = ? WHERE categoryId = ? AND authenticatorSecret = ?",
                        binding.Ranking, binding.CategoryId, binding.AuthenticatorSecret);
            });

            await Update();
        }

        public Authenticator Get(int position)
        {
            return _view.ElementAtOrDefault(position);
        }

        public int GetPosition(string secret)
        {
            return _view.FindIndex(a => a.Secret == secret);
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

        public async Task UpdateSingle(Authenticator auth)
        {
            await _connection.UpdateAsync(auth);
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
            var binding = new AuthenticatorCategory(authSecret, categoryId);
            await _connection.InsertAsync(binding);
            CategoryBindings.Add(binding);
            
            if(CategoryId == categoryId)
                UpdateView();
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