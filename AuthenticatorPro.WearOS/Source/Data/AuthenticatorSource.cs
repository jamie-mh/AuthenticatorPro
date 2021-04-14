using System;
using System.Collections.Generic;
using System.Linq;
using AuthenticatorPro.Droid.Shared.Query;
using AuthenticatorPro.Shared.Source.Data;
using AuthenticatorPro.WearOS.Cache;

namespace AuthenticatorPro.WearOS.Data
{
    internal class AuthenticatorSource : ISource<WearAuthenticator>
    {
        private readonly ListCache<WearAuthenticator> _cache;
        public string CategoryId { get; private set; }
        
        private List<WearAuthenticator> _view;

        
        public AuthenticatorSource(ListCache<WearAuthenticator> cache)
        {
            _cache = cache;
            _view = cache.GetItems();
        }

        public void SetCategory(string id)
        {
            CategoryId = id;
            UpdateView();
        }

        public void UpdateView()
        {
            var view = _cache.GetItems().AsEnumerable();

            if(CategoryId != null)
            {
                view = view
                    .Where(a => a.Categories != null && a.Categories.Any(c => c.CategoryId == CategoryId))
                    .OrderBy(a => a.Categories.First(c => c.CategoryId == CategoryId).Ranking);
            }
            else
                view = view.OrderBy(a => a.Ranking);

            _view = view.ToList();
        }
        
        public List<WearAuthenticator> GetView()
        {
            return _view;
        }

        public List<WearAuthenticator> GetAll()
        {
            return _cache.GetItems();
        }

        public WearAuthenticator Get(int position)
        {
            return _view.ElementAtOrDefault(position);
        }

        public int FindIndex(Predicate<WearAuthenticator> predicate)
        {
            return _view.FindIndex(predicate);
        }
    }
}