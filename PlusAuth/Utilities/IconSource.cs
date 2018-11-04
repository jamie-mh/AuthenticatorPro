using System.Collections.Generic;
using System.Linq;

namespace PlusAuth.Utilities
{
    internal class IconSource
    {
        public Dictionary<string, int> Icons { get; private set; }
        private string _search;

        public IconSource()
        {
            _search = "";
            Icons = new Dictionary<string, int>(Icon.List.Count);

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

                List<string> keys = Icon.List.Keys.Where(k => k.Contains(query)).ToList();
                Icons = new Dictionary<string, int>(keys.Count);
                keys.ForEach(key => Icons.Add(key, Icon.Get(key)));

                return;
            }

            Icons = Icon.List;
        }
    }
}