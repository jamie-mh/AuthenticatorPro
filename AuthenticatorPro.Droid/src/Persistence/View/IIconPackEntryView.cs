// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Graphics;
using AuthenticatorPro.Core.Entity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthenticatorPro.Droid.Persistence.View
{
    public interface IIconPackEntryView : IView<KeyValuePair<string, Bitmap>>
    {
        public Task LoadFromPersistenceAsync(IconPack pack);
        public string Search { get; set; }
    }
}