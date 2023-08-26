// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Threading.Tasks;
using AuthenticatorPro.Core.Entity;

namespace AuthenticatorPro.Droid.Persistence.View
{
    public interface ICategoryView : IReorderableView<Category>
    {
        public Task LoadFromPersistenceAsync();
        public int IndexOf(string id);
    }
}