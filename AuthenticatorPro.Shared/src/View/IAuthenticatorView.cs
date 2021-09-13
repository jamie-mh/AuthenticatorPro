// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Entity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthenticatorPro.Shared.View
{
    public interface IAuthenticatorView : IReorderableView<Authenticator>
    {
        public string Search { get; set; }
        public string CategoryId { get; set; }
        public SortMode SortMode { get; set; }
        public Task LoadFromPersistenceAsync();
        public bool AnyWithoutFilter();
        public int IndexOf(Authenticator auth);
        public IEnumerable<AuthenticatorCategory> GetCurrentBindings();
        public void CommitRanking();
        public void Clear();
    }
}