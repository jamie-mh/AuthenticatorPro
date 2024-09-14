// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Graphics;
using Stratum.Core.Entity;

namespace Stratum.Droid.Persistence.View
{
    public interface IIconPackEntryView : IView<KeyValuePair<string, Bitmap>>
    {
        public string Search { get; set; }
        public Task LoadFromPersistenceAsync(IconPack pack);
    }
}