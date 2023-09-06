// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using System.Threading.Tasks;
using AuthenticatorPro.Core;
using AuthenticatorPro.Core.Entity;

namespace AuthenticatorPro.Droid.Persistence.View
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
    }
}