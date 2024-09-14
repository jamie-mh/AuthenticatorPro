// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Stratum.Droid.Shared;

namespace Stratum.Droid.Persistence.View.Impl
{
    public class DefaultIconView : IDefaultIconView
    {
        private Dictionary<string, int> _view;

        private string _search;

        public string Search
        {
            get => _search;
            set
            {
                _search = value;
                Update();
            }
        }

        public bool UseDarkTheme { get; set; }

        public IEnumerator<KeyValuePair<string, int>> GetEnumerator()
        {
            return _view.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _view).GetEnumerator();
        }

        public int Count => _view.Count;

        public KeyValuePair<string, int> this[int index] => _view.ElementAt(index);

        public void Update()
        {
            if (string.IsNullOrEmpty(_search))
            {
                _view = new Dictionary<string, int>(IconMap.Service.Count);
                foreach (var (key, _) in IconMap.Service)
                {
                    _view.Add(key, IconResolver.GetService(key, UseDarkTheme));
                }

                return;
            }

            var query = _search.ToLower();

            var keys = IconMap.Service.Keys.Where(k => k.Contains(query)).ToList();
            _view = new Dictionary<string, int>(keys.Count);
            keys.ForEach(key => _view.Add(key, IconResolver.GetService(key, UseDarkTheme)));
        }
    }
}