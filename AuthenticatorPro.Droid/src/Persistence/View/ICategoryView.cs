// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Entity;
using System.Threading.Tasks;

namespace AuthenticatorPro.Droid.Persistence.View
{
    public interface ICategoryView : IReorderableView<Category>
    {
        public Task LoadFromPersistence();
        public int IndexOf(string id);
    }
}