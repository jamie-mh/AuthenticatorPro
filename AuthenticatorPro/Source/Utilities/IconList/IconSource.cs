using System.Collections.Generic;
using System.Linq;

namespace AuthenticatorPro.Utilities.IconList
{
    internal class IconSource
    {
        private string _search;

        public IconSource()
        {
            _search = "";
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
            if(_search.Trim() != "")
            {
                var query = _search.ToLower();

                var keys = Icons.Service.Keys.Where(k => k.Contains(query)).ToList();
                List = new Dictionary<string, int>(keys.Count);
                keys.ForEach(key => List.Add(key, Icons.GetService(key)));

                return;
            }

            foreach(var icon in Icons.Service)
                List.Add(icon.Key, Icons.GetService(icon.Key));
        }
    }
}