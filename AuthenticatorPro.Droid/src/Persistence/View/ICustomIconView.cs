// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Graphics;

namespace AuthenticatorPro.Droid.Persistence.View
{
    public interface ICustomIconView : IView<KeyValuePair<string, Bitmap>>
    {
        public Task LoadFromPersistenceAsync();
        public Bitmap GetOrDefault(string id);
    }
}