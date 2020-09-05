using System.Collections.Generic;
using System.Linq;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Query;
using AuthenticatorPro.WearOS.Cache;

namespace AuthenticatorPro.WearOS.Data
{
    internal class AuthenticatorSource : ISource<WearAuthenticatorResponse>
    {
        private readonly ListCache<WearAuthenticatorResponse> _cache;
        public string CategoryId { get; private set; }
        
        private List<WearAuthenticatorResponse> _view;

        
        public AuthenticatorSource(ListCache<WearAuthenticatorResponse> cache)
        {
            _cache = cache;
            _view = cache.GetItems();
        }

        public void SetCategory(string id)
        {
            CategoryId = id;
            UpdateView();
        }

        private void UpdateView()
        {
            _view = _cache.GetItems();

            if(CategoryId != null)
                _view = _view.Where(a => a.CategoryIds.Contains(CategoryId)).ToList();
        }
        
        public List<WearAuthenticatorResponse> GetView()
        {
            return _view;
        }

        public List<WearAuthenticatorResponse> GetAll()
        {
            return _cache.GetItems();
        }

        public WearAuthenticatorResponse Get(int position)
        {
            return _view.ElementAtOrDefault(position);
        }
    }
}