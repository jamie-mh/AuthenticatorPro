// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthenticatorPro.Droid.Shared.Data;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Data.Generator;
using SQLite;

namespace AuthenticatorPro.Droid.Data.Source
{
    internal class AuthenticatorSource
    {
        public string Search { get; private set; }
        public string CategoryId { get; private set; }
        public SortMode SortMode { get; private set; }
        
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

        public void SetSortMode(SortMode mode)
        {
            SortMode = mode;
            UpdateView();
        }

        public void UpdateView()
        {
            var view = _all.AsEnumerable();

            if(CategoryId != null)
            {
                var bindingForCategory = CategoryBindings.Where(b => b.CategoryId == CategoryId);
                view = view.Where(a => bindingForCategory.Any(c => c.AuthenticatorSecret == a.Secret));
                    
                if(SortMode == SortMode.Custom)
                    view = view.OrderBy(a => bindingForCategory.First(c => c.AuthenticatorSecret == a.Secret).Ranking);
            }

            if(!String.IsNullOrEmpty(Search))
            {
                var searchLower = Search.ToLower();
                view = view.Where(i => i.Issuer.ToLower().Contains(searchLower) || i.Username != null && i.Username.Contains(searchLower));
            }

            view = SortMode switch
            {
                SortMode.AlphabeticalAscending => view.OrderBy(a => a.Issuer).ThenBy(a => a.Username),
                SortMode.AlphabeticalDescending => view.OrderByDescending(a => a.Issuer).ThenByDescending(a => a.Username),
                SortMode.Custom when CategoryId == null => view.OrderBy(a => a.Ranking).ThenBy(a => a.Issuer).ThenBy(a => a.Username),
                _ => view
            };

            _view = view.ToList();
        }

        public async Task Update()
        {
            _all = await _connection.Table<Authenticator>().ToListAsync();
            CategoryBindings = await _connection.Table<AuthenticatorCategory>().ToListAsync();
            UpdateView();
        }

        public async Task<int> Add(Authenticator auth)
        {
            if(Exists(auth))
                throw new ArgumentException("Authenticator already exists");
            
            if(!auth.IsValid())
                throw new ArgumentException("Authenticator is invalid");
            
            await _connection.InsertAsync(auth);
            await Update();
            return GetPosition(auth.Secret);
        }

        public async Task<int> AddMany(IEnumerable<Authenticator> authenticators)
        {
            var valid = GetDistinct(authenticators.Where(a => a.IsValid() && !Exists(a))).ToList();
            var added = await _connection.InsertAllAsync(valid);
            await Update();
            return added;
        }

        public async Task<Tuple<int, int>> AddOrUpdateMany(IEnumerable<Authenticator> authenticators)
        {
            var valid = GetDistinct(authenticators.Where(a => a.IsValid())).ToList();
            
            var toAdd = valid.Where(a => !Exists(a)).ToList();
            var addedCount = await _connection.InsertAllAsync(toAdd);

            var toUpdate = valid
                .Where(a => !toAdd.Contains(a))
                .OrderBy(a => a.Secret).ToList();
            
            var updateTargets = _all
                .Where(a => toUpdate.Any(b => a.Secret == b.Secret))
                .OrderBy(a => a.Secret);
                
            var diff = updateTargets.Except(toUpdate, new AuthenticatorComparer());
            var updatedCount = diff.Count();
            
            await _connection.UpdateAllAsync(toUpdate);
            await Update();
            
            return new Tuple<int, int>(addedCount, updatedCount);
        }

        private static IEnumerable<Authenticator> GetDistinct(IEnumerable<Authenticator> authenticators)
        {
            return authenticators.GroupBy(a => a.Secret).Select(a => a.First());
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
                throw new ArgumentOutOfRangeException(nameof(position), "No authenticator at position");

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
            if(SortMode != SortMode.Custom)
                SortMode = SortMode.Custom;
            
            var atNewPos = Get(newPosition);

            if(atNewPos == null)
                throw new ArgumentOutOfRangeException(nameof(newPosition), "No authenticator at position");
            
            var atOldPos = Get(oldPosition);
            
            if(atOldPos == null)
                throw new ArgumentOutOfRangeException(nameof(oldPosition), "No authenticator at position");
            
            _view[newPosition] = atOldPos;
            _view[oldPosition] = atNewPos;
        }

        public async Task CommitRanking()
        {
            if(SortMode != SortMode.Custom)
                throw new InvalidOperationException("Cannot commit ranking to fixed sort mode");
            
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
                throw new ArgumentOutOfRangeException(nameof(position), "No authenticator at position");

            if(auth.Type.GetGenerationMethod() != GenerationMethod.Counter)
                throw new ArgumentException("Authenticator is not counter based");

            auth.Counter++;
            await _connection.UpdateAsync(auth);
        }

        public bool Exists(Authenticator auth)
        {
            return _all.Any(iterator => auth.Secret == iterator.Secret);
        }

        private bool IsDuplicateCategoryBinding(AuthenticatorCategory binding)
        {
            return CategoryBindings.Any(
                ac => binding.AuthenticatorSecret == ac.AuthenticatorSecret &&
                         binding.CategoryId == ac.CategoryId);
        }

        public string[] GetCategories(int position)
        {
            var auth = Get(position);

            if(auth == null)
                throw new ArgumentOutOfRangeException(nameof(position), "No authenticator at position");

            return CategoryBindings.Where(c => c.AuthenticatorSecret == auth.Secret)
                                   .Select(c => c.CategoryId)
                                   .ToArray();
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
                throw new ArgumentException("Category binding does not exist");

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