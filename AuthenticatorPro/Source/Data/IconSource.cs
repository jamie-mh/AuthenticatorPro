using System.Collections.Generic;
using System.Linq;
using AuthenticatorPro.Shared.Data;

namespace AuthenticatorPro.Data
{
    internal class IconSource
    {
        private readonly bool _isDark;
        private string _search;

        public IconSource(bool isDark)
        {
            _search = "";
            _isDark = isDark;
            View = new Dictionary<string, int>(Icon.Service.Count);

            Update();
        }

        public Dictionary<string, int> View { get; private set; }

        public void SetSearch(string query)
        {
            _search = query;
            Update();
        }

        public KeyValuePair<string, int> Get(int position)
        {
            return View.ElementAtOrDefault(position);
        }

        private void Update()
        {
            if(_search.Trim() == "")
            {
                View = new Dictionary<string, int>(Icon.Service.Count);
                foreach(var item in Icon.Service)
                    View.Add(item.Key, Icon.GetService(item.Key, _isDark));

                return;
            }

            var query = _search.ToLower();

            var keys = Icon.Service.Keys.Where(k => k.Contains(query)).ToList();
            View = new Dictionary<string, int>(keys.Count);
            keys.ForEach(key => View.Add(key, Icon.GetService(key, _isDark)));
        }
    }
}