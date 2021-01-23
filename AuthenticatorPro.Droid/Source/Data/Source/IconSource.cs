using System;
using System.Collections.Generic;
using System.Linq;
using AuthenticatorPro.Droid.Shared.Data;

namespace AuthenticatorPro.Droid.Data.Source
{
    internal class IconSource : ISource<KeyValuePair<string, int>>
    {
        private readonly bool _isDark;
        private string _search;
        private Dictionary<string, int> _view;

        public IconSource(bool isDark)
        {
            _search = "";
            _isDark = isDark;
            _view = new Dictionary<string, int>(Icon.Service.Count);

            Update();
        }

        public void SetSearch(string query)
        {
            _search = query;
            Update();
        }

        public List<KeyValuePair<string, int>> GetView()
        {
            return _view.ToList();
        }

        public List<KeyValuePair<string, int>> GetAll()
        {
            return Icon.Service.ToList();
        }

        public KeyValuePair<string, int> Get(int position)
        {
            return _view.ElementAtOrDefault(position);
        }

        private void Update()
        {
            if(String.IsNullOrEmpty(_search))
            {
                _view = new Dictionary<string, int>(Icon.Service.Count);
                foreach(var (key, _) in Icon.Service)
                    _view.Add(key, Icon.GetService(key, _isDark));

                return;
            }

            var query = _search.ToLower();

            var keys = Icon.Service.Keys.Where(k => k.Contains(query)).ToList();
            _view = new Dictionary<string, int>(keys.Count);
            keys.ForEach(key => _view.Add(key, Icon.GetService(key, _isDark)));
        }
    }
}