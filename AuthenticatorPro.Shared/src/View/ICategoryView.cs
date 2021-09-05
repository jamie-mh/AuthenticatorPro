// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Entity;
using System.Threading.Tasks;

namespace AuthenticatorPro.Shared.View
{
    public interface ICategoryView : IReorderableView<Category>
    {
        public Task LoadFromPersistence();
        public int IndexOf(string id);
    }
}