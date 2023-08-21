// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Entity;
using System.Threading.Tasks;

namespace AuthenticatorPro.Droid.Persistence.View
{
    public interface IIconPackView : IView<IconPack>
    {
        public Task LoadFromPersistenceAsync();
        public int IndexOf(string name);
    }
}