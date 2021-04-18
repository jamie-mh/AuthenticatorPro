using System;
using System.Collections.Generic;
using System.Linq;
using AuthenticatorPro.Droid.Shared.Data;
using AuthenticatorPro.Droid.Shared.Query;
using AuthenticatorPro.WearOS.Cache;

namespace AuthenticatorPro.WearOS.Data
{
    internal class AuthenticatorSource
    {
        private readonly ListCache<WearAuthenticator> _cache;
        public string CategoryId { get; private set; }
        public SortMode SortMode { get; private set; }
        
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

        public void SetSortMode(SortMode sortMode)
        {
            SortMode = sortMode;
            UpdateView();
        }

        public void UpdateView()
        {
            var view = _cache.GetItems().AsEnumerable();

            if(CategoryId != null)
            {
                view = view.Where(a => a.Categories != null && a.Categories.Any(c => c.CategoryId == CategoryId));
                    
                if(SortMode == SortMode.Custom)
                    view = view.OrderBy(a => a.Categories.First(c => c.CategoryId == CategoryId).Ranking);
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
        
        public List<WearAuthenticator> GetView()
        {
            return _view;
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