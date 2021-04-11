using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using AuthenticatorPro.Shared.Source.Data;
using Microsoft.UI.Xaml.Controls;
using SQLite;

namespace AuthenticatorPro.UWP.Data.Source
{
    internal class AuthenticatorSource : IList<Authenticator>, IKeyIndexMapping, INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private string _categoryId;
        public string CategoryId
        {
            get => _categoryId;
            set
            { 
                _categoryId = value;
                UpdateView();
            }
        }

        private List<Authenticator> _view;
        private List<Authenticator> _all;
        private List<AuthenticatorCategory> _categoryBindings;

        private readonly SQLiteAsyncConnection _connection;

        public AuthenticatorSource(SQLiteAsyncConnection connection)
        {
            _connection = connection;
            _categoryId = null;

            _view = new List<Authenticator>();
            _all = new List<Authenticator>();
            _categoryBindings = new List<AuthenticatorCategory>();
        }

        public async Task Update()
        {
            _all = await _connection.Table<Authenticator>().OrderBy(a => a.Ranking).OrderBy(a => a.Issuer).OrderBy(a => a.Username).ToListAsync();
            _categoryBindings = await _connection.Table<AuthenticatorCategory>().OrderBy(a => a.Ranking).ToListAsync();
            UpdateView();
        }

        public void UpdateView()
        {
            var view = _all.AsEnumerable();

            if(CategoryId != null)
            {
                var bindingForCategory = _categoryBindings.Where(b => b.CategoryId == CategoryId).ToList();

                view = view.Where(a => bindingForCategory.Any(c => c.AuthenticatorSecret == a.Secret))
                           .OrderBy(a => bindingForCategory.First(c => c.AuthenticatorSecret == a.Secret).Ranking);
            }

            _view = view.ToList();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public Authenticator this[int index] { get => _view[index]; set => throw new NotImplementedException(); }

        public int Count => _view.Count;

        public bool IsReadOnly => false;

        public void Add(Authenticator item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(Authenticator item)
        {
            return _view.Contains(item);
        }

        public void CopyTo(Authenticator[] array, int arrayIndex)
        {
            _view.CopyTo(array, arrayIndex);
        }

        public IEnumerator<Authenticator> GetEnumerator()
        {
            return _view.GetEnumerator();
        }

        public int IndexOf(Authenticator item)
        {
            return _view.IndexOf(item);
        }

        public void Insert(int index, Authenticator item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(Authenticator item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public string KeyFromIndex(int index)
        {
            return _view[index].Secret;
        }

        public int IndexFromKey(string key)
        {
            return _view.FindIndex(a => a.Secret == key);
        }
    }
}
