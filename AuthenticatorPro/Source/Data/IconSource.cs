using System.Collections.Generic;
using System.Linq;
using AuthenticatorPro.Shared;

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
            List = new Dictionary<string, int>(Icons.Service.Count);

            Update();
        }

        public Dictionary<string, int> List { get; private set; }

        public void SetSearch(string query)
        {
            _search = query;
            Update();
        }

        public void Update()
        {
            if(_search.Trim() == "")
            {
                List = new Dictionary<string, int>(Icons.Service.Count);
                foreach(var item in Icons.Service)
                    List.Add(item.Key, Icons.GetService(item.Key, _isDark));

                return;
            }

            var query = _search.ToLower();

            var keys = Icons.Service.Keys.Where(k => k.Contains(query)).ToList();
            List = new Dictionary<string, int>(keys.Count);
            keys.ForEach(key => List.Add(key, Icons.GetService(key, _isDark)));
        }
    }
}