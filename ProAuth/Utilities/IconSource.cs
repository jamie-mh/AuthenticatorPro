using System.Collections.Generic;
using System.Linq;

namespace ProAuth.Utilities
{
    internal class IconSource
    {
        public Dictionary<string, int> List { get; private set; }
        private string _search;

        public IconSource()
        {
            _search = "";
            List = new Dictionary<string, int>(Icons.Service.Count);

            Update();
        }

        public void SetSearch(string query)
        {
            _search = query;
            Update();
        }

        public void Update()
        {
            if(_search.Trim() != "")
            {
                string query = _search.ToLower();

                List<string> keys = Icons.Service.Keys.Where(k => k.Contains(query)).ToList();
                List = new Dictionary<string, int>(keys.Count);
                keys.ForEach(key => List.Add(key, Icons.GetService(key)));

                return;
            }

            List = Icons.Service;
        }
    }
}