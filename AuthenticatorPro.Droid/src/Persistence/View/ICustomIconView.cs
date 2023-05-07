// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Graphics;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthenticatorPro.Droid.Persistence.View
{
    public interface ICustomIconView : IView<KeyValuePair<string, Bitmap>>
    {
        public Task LoadFromPersistenceAsync();
        public Bitmap GetOrDefault(string id);
    }
}